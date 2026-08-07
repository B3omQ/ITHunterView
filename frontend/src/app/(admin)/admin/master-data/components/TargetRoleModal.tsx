import React, { useState, useEffect, useMemo } from "react";
import { X, Plus, Trash2, Search, Check, AlertTriangle } from "lucide-react";
import { 
  TargetRoleTemplateDto, 
  CreateTargetRoleTemplateDto, 
  UpdateTargetRoleTemplateDto,
  CreateTargetRoleSkillDto
} from "@/types/master-data.types";
import { useCreateTargetRole, useUpdateTargetRole, useAllSfiaSkills } from "@/hooks/useTargetRole";
import { useTranslations } from 'next-intl';

interface TargetRoleModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: "create" | "edit";
  initialData?: TargetRoleTemplateDto | null;
  onSuccess: (message: string) => void;
}

export function TargetRoleModal({
  isOpen,
  onClose,
  mode,
  initialData,
  onSuccess,
}: TargetRoleModalProps) {
  const t = useTranslations('AdminMasterData');
  const [roleName, setRoleName] = useState("");
  const [description, setDescription] = useState("");
  const [skills, setSkills] = useState<CreateTargetRoleSkillDto[]>([]);
  
  const [skillSearch, setSkillSearch] = useState("");
  const [isSkillDropdownOpen, setIsSkillDropdownOpen] = useState(false);
  
  const [error, setError] = useState("");

  const createMutation = useCreateTargetRole();
  const updateMutation = useUpdateTargetRole();
  
  const { data: sfiaSkillsResponse, isLoading: isSkillsLoading } = useAllSfiaSkills();
  const allSfiaSkills = useMemo(() => sfiaSkillsResponse?.data || [], [sfiaSkillsResponse]);

  useEffect(() => {
    if (isOpen) {
      if (mode === "edit" && initialData) {
        setRoleName(initialData.roleName);
        setDescription(initialData.description || "");
        
        // Map from TargetRoleSkillDto to CreateTargetRoleSkillDto
        // We need the sfiaSkillId, but initialData only has skillCode and skillName.
        // So we look it up from allSfiaSkills based on skillCode.
        if (allSfiaSkills.length > 0) {
           const mappedSkills = initialData.requiredSkills.map(rs => {
             const foundSfia = allSfiaSkills.find(s => s.skillCode === rs.skillCode);
             return {
               sfiaSkillId: foundSfia ? foundSfia.id : "",
               targetLevel: rs.targetLevel
             };
           }).filter(s => s.sfiaSkillId !== ""); // filter out any that couldn't be mapped
           setSkills(mappedSkills);
        } else {
           setSkills([]);
        }
      } else {
        setRoleName("");
        setDescription("");
        setSkills([]);
      }
      setError("");
      setSkillSearch("");
      setIsSkillDropdownOpen(false);
    }
  }, [isOpen, mode, initialData, allSfiaSkills]);

  // Handle outside click for dropdown
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as HTMLElement;
      if (!target.closest('.skill-search-container')) {
        setIsSkillDropdownOpen(false);
      }
    };
    if (isSkillDropdownOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isSkillDropdownOpen]);

  if (!isOpen) return null;

  const isPending = createMutation.isPending || updateMutation.isPending;

  const filteredSfiaSkills = allSfiaSkills.filter(s => 
    !skills.some(selected => selected.sfiaSkillId === s.id) &&
    (s.skillName.toLowerCase().includes(skillSearch.toLowerCase()) || 
     s.skillCode.toLowerCase().includes(skillSearch.toLowerCase()))
  ).slice(0, 50); // limit to 50 for performance

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!roleName.trim()) {
      setError("Role name is required.");
      return;
    }

    if (skills.length === 0) {
      setError("At least one required skill must be added.");
      return;
    }

    const payload: CreateTargetRoleTemplateDto = {
      roleName: roleName.trim(),
      description: description.trim(),
      requiredSkills: skills,
    };

    if (mode === "create") {
      createMutation.mutate(payload, {
        onSuccess: () => {
          onSuccess("Target Role created successfully!");
          onClose();
        },
        onError: (err: any) => {
          setError(err.response?.data?.message || "Failed to create target role.");
        },
      });
    } else if (mode === "edit" && initialData) {
      updateMutation.mutate(
        { id: initialData.id, dto: payload },
        {
          onSuccess: () => {
            onSuccess("Target Role updated successfully!");
            onClose();
          },
          onError: (err: any) => {
            setError(err.response?.data?.message || "Failed to update target role.");
          },
        }
      );
    }
  };

  const addSkill = (sfiaSkillId: string) => {
    setSkills([...skills, { sfiaSkillId, targetLevel: 3 }]); // Default level 3
    setSkillSearch("");
    setIsSkillDropdownOpen(false);
  };

  const removeSkill = (index: number) => {
    const newSkills = [...skills];
    newSkills.splice(index, 1);
    setSkills(newSkills);
  };

  const updateSkillLevel = (index: number, level: number) => {
    const newSkills = [...skills];
    newSkills[index].targetLevel = level;
    setSkills(newSkills);
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm transition-opacity"
        onClick={!isPending ? onClose : undefined}
      />
      
      <div className="relative bg-card rounded-2xl shadow-xl w-full max-w-3xl flex flex-col max-h-[90vh] animate-in zoom-in-95 duration-200">
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <div>
            <h2 className="text-lg font-bold text-foreground">
              {mode === "create" ? t('targetRoleFormTitleCreate') : t('targetRoleFormTitleEdit')}
            </h2>
            <p className="text-xs text-muted-foreground mt-1">
              Configure the role and its required SFIA skills.
            </p>
          </div>
          <button
            onClick={onClose}
            disabled={isPending}
            className="p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-full transition-colors disabled:opacity-50"
          >
            <X size={20} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-6">
          <form id="target-role-form" onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="flex items-center gap-2 p-3 text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-xl">
                <AlertTriangle size={16} />
                <span>{error}</span>
              </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-4">
                <div className="space-y-2">
                  <label className="text-sm font-semibold text-foreground">
                    {t('targetRoleFormNameEn')} <span className="text-destructive">*</span>
                  </label>
                  <input
                    type="text"
                    value={roleName}
                    onChange={(e) => setRoleName(e.target.value)}
                    placeholder="e.g. Senior Software Engineer"
                    className="w-full px-4 py-2.5 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                    disabled={isPending}
                  />
                </div>

                <div className="space-y-2">
                  <label className="text-sm font-semibold text-foreground">
                    {t('targetRoleFormDescEn')}
                  </label>
                  <textarea
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="Brief description of the role's responsibilities..."
                    rows={4}
                    className="w-full px-4 py-2.5 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all resize-none"
                    disabled={isPending}
                  />
                </div>
              </div>

              {/* Skills Section */}
              <div className="space-y-4 border-l border-border pl-0 md:pl-6">
                <label className="text-sm font-semibold text-foreground flex items-center justify-between">
                  <span>{t('targetRoleFormRequiredSkills')} <span className="text-destructive">*</span></span>
                  <span className="text-xs font-normal text-muted-foreground bg-muted px-2 py-0.5 rounded-full">{skills.length} added</span>
                </label>

                {/* Skill Search/Select */}
                <div className="relative skill-search-container z-50">
                  <div className="relative">
                    <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                    <input
                      type="text"
                      placeholder="Search and add SFIA skills..."
                      value={skillSearch}
                      onChange={(e) => {
                        setSkillSearch(e.target.value);
                        setIsSkillDropdownOpen(true);
                      }}
                      onFocus={() => setIsSkillDropdownOpen(true)}
                      className="w-full pl-9 pr-4 py-2.5 rounded-xl border border-input bg-muted/30 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
                      disabled={isPending || isSkillsLoading}
                    />
                  </div>
                  
                  {isSkillDropdownOpen && (
                    <div className="absolute top-full left-0 right-0 mt-2 bg-popover border border-border rounded-xl shadow-lg overflow-hidden max-h-60 overflow-y-auto">
                      {isSkillsLoading ? (
                        <div className="p-4 text-center text-xs text-muted-foreground">Loading skills...</div>
                      ) : filteredSfiaSkills.length === 0 ? (
                        <div className="p-4 text-center text-xs text-muted-foreground">No matching skills found.</div>
                      ) : (
                        <div className="p-1">
                          {filteredSfiaSkills.map(skill => (
                            <button
                              key={skill.id}
                              type="button"
                              onClick={() => addSkill(skill.id)}
                              className="w-full flex items-center justify-between px-3 py-2 text-left text-sm hover:bg-muted rounded-lg transition-colors group"
                            >
                              <div className="flex flex-col">
                                <span className="font-medium text-foreground">{skill.skillName}</span>
                                <span className="text-[10px] text-muted-foreground">{skill.skillCode} • {skill.category}</span>
                              </div>
                              <Plus size={16} className="text-primary opacity-0 group-hover:opacity-100 transition-opacity" />
                            </button>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>

                {/* Selected Skills List */}
                <div className="space-y-3 max-h-[350px] overflow-y-auto pr-1">
                  {skills.length === 0 ? (
                    <div className="text-center py-8 border-2 border-dashed border-border rounded-xl bg-muted/10">
                      <p className="text-xs text-muted-foreground">{t('targetRoleFormNoSkills')}</p>
                    </div>
                  ) : (
                    skills.map((skill, index) => {
                      const sfiaSkill = allSfiaSkills.find(s => s.id === skill.sfiaSkillId);
                      return (
                        <div key={skill.sfiaSkillId} className="p-3 bg-muted/20 border border-border rounded-xl flex flex-col gap-3 group">
                          <div className="flex items-start justify-between">
                            <div className="flex flex-col">
                              <span className="text-sm font-semibold text-foreground">
                                {sfiaSkill?.skillName || "Unknown Skill"}
                              </span>
                              <span className="text-[10px] text-muted-foreground uppercase tracking-wider mt-0.5">
                                {sfiaSkill?.skillCode || "CODE"}
                              </span>
                            </div>
                            <button
                              type="button"
                              onClick={() => removeSkill(index)}
                              className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors"
                            >
                              <Trash2 size={14} />
                            </button>
                          </div>
                          
                          <div className="flex items-center gap-3">
                            <span className="text-xs font-medium text-foreground whitespace-nowrap w-24">Target Level: <span className="text-primary">{skill.targetLevel}</span></span>
                            <input
                              type="range"
                              min="1"
                              max="7"
                              step="1"
                              value={skill.targetLevel}
                              onChange={(e) => updateSkillLevel(index, parseInt(e.target.value))}
                              className="flex-1 h-1.5 bg-border rounded-lg appearance-none cursor-pointer accent-primary"
                            />
                            <div className="flex justify-between w-full absolute pointer-events-none px-2 invisible">
                               {/* Just for spacing logic if needed */}
                            </div>
                          </div>
                        </div>
                      );
                    })
                  )}
                </div>
              </div>
            </div>
          </form>
        </div>

        <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-border bg-muted/10">
          <button
            type="button"
            onClick={onClose}
            disabled={isPending}
            className="px-5 py-2.5 text-sm font-medium text-muted-foreground hover:text-foreground bg-transparent hover:bg-muted rounded-xl transition-colors disabled:opacity-50"
          >
            {t('cancelBtn')}
          </button>
          <button
            type="submit"
            form="target-role-form"
            disabled={isPending}
            className="inline-flex items-center gap-2 px-6 py-2.5 text-sm font-semibold text-primary-foreground bg-primary hover:bg-primary/95 rounded-xl shadow-xs transition-colors disabled:opacity-50"
          >
            {isPending ? (
              <div className="h-4 w-4 rounded-full border-2 border-primary-foreground/30 border-t-primary-foreground animate-spin" />
            ) : (
              <Check size={16} />
            )}
            <span>{mode === "create" ? t('saveBtn') : t('saveBtn')}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
