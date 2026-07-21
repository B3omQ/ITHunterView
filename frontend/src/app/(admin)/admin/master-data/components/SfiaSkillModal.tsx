"use client";

import React, { useState, useEffect } from "react";
import { X, Save, AlertCircle } from "lucide-react";
import { SfiaSkillDto } from "@/types/master-data.types";
import { useCreateSfiaSkill, useUpdateSfiaSkill } from "@/hooks/useSfiaSkill";

interface SfiaSkillModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode: "create" | "edit";
  initialData?: SfiaSkillDto | null;
  onSuccess: (msg: string) => void;
}

export function SfiaSkillModal({
  isOpen,
  onClose,
  mode,
  initialData,
  onSuccess,
}: SfiaSkillModalProps) {
  const [skillCode, setSkillCode] = useState("");
  const [skillName, setSkillName] = useState("");
  const [category, setCategory] = useState("");
  const [subcategory, setSubcategory] = useState("");
  const [description, setDescription] = useState("");
  const [selectedLevels, setSelectedLevels] = useState<number[]>([]);
  const [levelDescriptions, setLevelDescriptions] = useState<Record<number, string>>({});
  const [error, setError] = useState("");

  const createMutation = useCreateSfiaSkill();
  const updateMutation = useUpdateSfiaSkill();

  useEffect(() => {
    if (isOpen) {
      if (mode === "edit" && initialData) {
        setSkillCode(initialData.skillCode || "");
        setSkillName(initialData.skillName || "");
        setCategory(initialData.category || "");
        setSubcategory(initialData.subcategory || "");
        setDescription(initialData.description || "");
        setSelectedLevels(
          initialData.availableLevels
            ? initialData.availableLevels.split(",").map(Number).filter((n) => !isNaN(n))
            : []
        );
        const descs: Record<number, string> = {};
        initialData.levels?.forEach(l => {
          descs[l.level] = l.description;
        });
        setLevelDescriptions(descs);
      } else {
        setSkillCode("");
        setSkillName("");
        setCategory("");
        setSubcategory("");
        setDescription("");
        setSelectedLevels([]);
        setLevelDescriptions({});
      }
      setError("");
    }
  }, [isOpen, mode, initialData]);

  if (!isOpen) return null;

  const isPending = createMutation.isPending || updateMutation.isPending;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!skillCode || !skillName || !category) {
      setError("Code, Name, and Category are required.");
      return;
    }

    const payload = {
      skillCode,
      skillName,
      category,
      subcategory,
      description,
      availableLevels: selectedLevels.sort((a, b) => a - b).join(","),
      levels: selectedLevels.map(level => ({
        level,
        description: levelDescriptions[level] || ""
      })),
    };

    if (mode === "create") {
      createMutation.mutate(payload, {
        onSuccess: (res) => {
          if (res.success) {
            onSuccess("SFIA Skill created successfully");
            onClose();
          } else {
            setError(res.message || "Failed to create skill");
          }
        },
        onError: (err: any) => {
          setError(err.response?.data?.message || "An error occurred");
        },
      });
    } else if (initialData?.id) {
      updateMutation.mutate(
        { id: initialData.id, dto: payload },
        {
          onSuccess: (res) => {
            if (res.success) {
              onSuccess("SFIA Skill updated successfully");
              onClose();
            } else {
              setError(res.message || "Failed to update skill");
            }
          },
          onError: (err: any) => {
            setError(err.response?.data?.message || "An error occurred");
          },
        }
      );
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-card w-full max-w-lg rounded-2xl shadow-xl overflow-hidden border border-border flex flex-col max-h-[90vh]">
        <div className="px-6 py-4 border-b border-border flex justify-between items-center bg-muted/10 shrink-0">
          <h2 className="text-lg font-semibold text-foreground">
            {mode === "create" ? "Add SFIA Skill" : "Edit SFIA Skill"}
          </h2>
          <button
            onClick={onClose}
            className="p-1.5 text-muted-foreground hover:bg-muted rounded-lg transition-colors"
          >
            <X size={20} />
          </button>
        </div>

        <div className="p-6 overflow-y-auto">
          {error && (
            <div className="mb-4 p-3 bg-destructive/10 border border-destructive/20 rounded-xl flex items-start gap-2 text-destructive text-sm">
              <AlertCircle size={16} className="mt-0.5 shrink-0" />
              <p>{error}</p>
            </div>
          )}

          <form id="sfia-skill-form" onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-foreground mb-1.5">
                Skill Code <span className="text-destructive">*</span>
              </label>
              <input
                type="text"
                value={skillCode}
                onChange={(e) => setSkillCode(e.target.value.toUpperCase())}
                placeholder="e.g. PROG"
                className="w-full px-3 py-2 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                disabled={isPending}
                required
              />
            </div>
            
            <div>
              <label className="block text-sm font-medium text-foreground mb-1.5">
                Skill Name <span className="text-destructive">*</span>
              </label>
              <input
                type="text"
                value={skillName}
                onChange={(e) => setSkillName(e.target.value)}
                placeholder="e.g. Programming/Software Development"
                className="w-full px-3 py-2 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                disabled={isPending}
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-1.5">
                Category <span className="text-destructive">*</span>
              </label>
              <input
                type="text"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                placeholder="e.g. Development and Implementation"
                className="w-full px-3 py-2 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                disabled={isPending}
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-1.5">
                Subcategory
              </label>
              <input
                type="text"
                value={subcategory}
                onChange={(e) => setSubcategory(e.target.value)}
                placeholder="e.g. Systems development"
                className="w-full px-3 py-2 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                disabled={isPending}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-1.5">
                Description
              </label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Description of the skill..."
                rows={3}
                className="w-full px-3 py-2 rounded-xl border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all resize-none"
                disabled={isPending}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Available Levels
              </label>
              <div className="flex flex-wrap gap-2">
                {[1, 2, 3, 4, 5, 6, 7].map((level) => (
                  <label
                    key={level}
                    className={`cursor-pointer px-3 py-1.5 rounded-full border text-sm font-medium transition-colors ${
                      selectedLevels.includes(level)
                        ? "bg-primary text-primary-foreground border-primary"
                        : "bg-background text-muted-foreground border-input hover:border-primary/50"
                    }`}
                  >
                    <input
                      type="checkbox"
                      className="sr-only"
                      checked={selectedLevels.includes(level)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedLevels([...selectedLevels, level]);
                        } else {
                          setSelectedLevels(selectedLevels.filter((l) => l !== level));
                        }
                      }}
                      disabled={isPending}
                    />
                    Level {level}
                  </label>
                ))}
              </div>

              {selectedLevels.length > 0 && (
                <div className="space-y-3 mt-4 p-4 bg-muted/20 rounded-xl border border-border">
                  <h3 className="text-sm font-medium text-foreground">Level Descriptions</h3>
                  {selectedLevels.sort((a, b) => a - b).map((level) => (
                    <div key={level}>
                      <label className="block text-xs font-medium text-muted-foreground mb-1">
                        Level {level}
                      </label>
                      <textarea
                        value={levelDescriptions[level] || ""}
                        onChange={(e) => setLevelDescriptions({ ...levelDescriptions, [level]: e.target.value })}
                        placeholder={`Description for Level ${level}...`}
                        rows={2}
                        className="w-full px-3 py-2 rounded-lg border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all resize-none"
                        disabled={isPending}
                      />
                    </div>
                  ))}
                </div>
              )}
            </div>
          </form>
        </div>

        <div className="px-6 py-4 border-t border-border bg-muted/10 flex justify-end gap-3 shrink-0">
          <button
            type="button"
            onClick={onClose}
            disabled={isPending}
            className="px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground hover:bg-muted rounded-xl transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            form="sfia-skill-form"
            disabled={isPending}
            className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 rounded-xl shadow-xs transition-colors disabled:opacity-50"
          >
            {isPending ? (
              <div className="w-4 h-4 border-2 border-primary-foreground/30 border-t-primary-foreground rounded-full animate-spin" />
            ) : (
              <Save size={16} />
            )}
            <span>{mode === "create" ? "Save Skill" : "Update Skill"}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
