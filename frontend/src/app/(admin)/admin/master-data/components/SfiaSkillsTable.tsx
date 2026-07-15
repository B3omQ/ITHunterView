"use client";

import React from "react";
import { Edit2, Trash2, AlertCircle, ChevronDown, ChevronUp } from "lucide-react";
import { SfiaSkillDto } from "@/types/master-data.types";

interface SfiaSkillsTableProps {
  skills: SfiaSkillDto[];
  isLoading: boolean;
  isError: boolean;
  onEdit: (skill: SfiaSkillDto) => void;
  onDelete: (skill: SfiaSkillDto) => void;
  onRetry: () => void;
}

export function SfiaSkillsTable({
  skills,
  isLoading,
  isError,
  onEdit,
  onDelete,
  onRetry,
}: SfiaSkillsTableProps) {
  const [expandedRows, setExpandedRows] = React.useState<Set<string>>(new Set());

  const toggleRow = (id: string) => {
    setExpandedRows((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  // Group skills by category -> subcategory -> skills
  const groupedData = React.useMemo(() => {
    const categories: Record<string, Record<string, SfiaSkillDto[]>> = {};
    
    if (!Array.isArray(skills)) return categories;

    skills.forEach(skill => {
      if (!categories[skill.category]) {
        categories[skill.category] = {};
      }
      const subcat = skill.subcategory || "Other";
      if (!categories[skill.category][subcat]) {
        categories[skill.category][subcat] = [];
      }
      categories[skill.category][subcat].push(skill);
    });
    
    return categories;
  }, [skills]);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-muted-foreground">
        <div className="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin mb-4"></div>
        <p className="text-sm">Loading SFIA skills...</p>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-destructive space-y-4">
        <AlertCircle size={40} className="opacity-50" />
        <div className="text-center">
          <p className="text-base font-semibold">Failed to load skills</p>
          <p className="text-sm opacity-80 mt-1">There was a problem communicating with the server.</p>
        </div>
        <button
          onClick={onRetry}
          className="px-4 py-2 bg-destructive/10 text-destructive hover:bg-destructive/20 rounded-xl text-sm font-medium transition-colors"
        >
          Try Again
        </button>
      </div>
    );
  }

  if (!skills || skills.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-muted-foreground">
        <div className="w-16 h-16 bg-muted/50 rounded-full flex items-center justify-center mb-4">
          <AlertCircle size={24} className="opacity-50" />
        </div>
        <p className="text-base font-medium text-foreground">No SFIA skills found</p>
        <p className="text-sm mt-1">Try adjusting your search criteria or add a new skill.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex-1 overflow-auto">
        <table className="w-full text-sm text-left border-collapse">
          <thead className="text-xs text-muted-foreground bg-muted/30 sticky top-0 z-10 backdrop-blur-sm">
            <tr>
              <th className="px-4 py-3 font-semibold text-left border-b border-border">Category</th>
              <th className="px-4 py-3 font-semibold text-left border-b border-border">Subcategory</th>
              <th className="px-4 py-3 font-semibold text-left border-b border-border">Skill</th>
              <th className="px-2 py-3 font-semibold text-center border-b border-border" colSpan={7}>Levels</th>
              <th className="px-4 py-3 font-semibold text-right border-b border-border">Actions</th>
            </tr>
            <tr className="border-b border-border bg-muted/10">
              <th colSpan={3} className="border-b border-border"></th>
              {[1, 2, 3, 4, 5, 6, 7].map(level => (
                <th key={level} className="px-2 py-1.5 font-medium text-center w-8 border-b border-border">{level}</th>
              ))}
              <th className="border-b border-border"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/50">
            {Object.entries(groupedData).map(([category, subcategories]) => {
              const totalCategoryRows = Object.values(subcategories).reduce((sum, skills) => {
                const expandedCount = skills.filter(s => expandedRows.has(s.id)).length;
                return sum + skills.length + expandedCount;
              }, 0);
              let isFirstInCategory = true;

              return (
                <React.Fragment key={category}>
                  {Object.entries(subcategories).map(([subcategory, subcatSkills]) => {
                    const totalSubcategoryRows = subcatSkills.length + subcatSkills.filter(s => expandedRows.has(s.id)).length;
                    let isFirstInSubcategory = true;

                    return subcatSkills.map(skill => {
                      const availableLevels = skill.availableLevels ? skill.availableLevels.split(',').map(Number) : [];
                      const isExpanded = expandedRows.has(skill.id);
                      
                      const tr = (
                        <tr key={skill.id} className="hover:bg-muted/10 transition-colors group border-b border-border/50">
                          {isFirstInCategory && (
                            <td className="px-4 py-3 font-semibold text-primary align-top border-r border-border/50 bg-card/30" rowSpan={totalCategoryRows}>
                              {category}
                            </td>
                          )}
                          {isFirstInSubcategory && (
                            <td className="px-4 py-3 text-muted-foreground align-top border-r border-border/50" rowSpan={totalSubcategoryRows}>
                              {subcategory === "Other" ? <span className="italic opacity-50">None</span> : subcategory}
                            </td>
                          )}
                          <td className="px-4 py-3 text-foreground">
                            <div className="flex items-center gap-2">
                              <span className="font-medium hover:underline cursor-pointer" onClick={() => onEdit(skill)}>{skill.skillName}</span>
                              <span className="text-xs text-muted-foreground font-mono bg-muted px-1.5 py-0.5 rounded shrink-0">{skill.skillCode}</span>
                            </div>
                          </td>
                          {[1, 2, 3, 4, 5, 6, 7].map(level => (
                            <td key={level} className={`px-2 py-3 text-center ${availableLevels.includes(level) ? 'bg-primary/5' : ''}`}>
                              {availableLevels.includes(level) ? (
                                <span className="font-semibold text-primary">{level}</span>
                              ) : (
                                <span className="text-muted-foreground/20">-</span>
                              )}
                            </td>
                          ))}
                          <td className="px-4 py-3 text-right">
                            <div className="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                              <button
                                onClick={() => toggleRow(skill.id)}
                                className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted/50 rounded-lg transition-colors"
                                title={isExpanded ? "Collapse Details" : "Expand Details"}
                              >
                                {isExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                              </button>
                              <div className="w-px h-4 bg-border mx-1"></div>
                              <button
                                onClick={() => onEdit(skill)}
                                className="p-1.5 text-muted-foreground hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                                title="Edit Skill"
                              >
                                <Edit2 size={14} />
                              </button>
                              <button
                                onClick={() => onDelete(skill)}
                                className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-lg transition-colors"
                                title="Delete Skill"
                              >
                                <Trash2 size={14} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );

                      isFirstInCategory = false;
                      isFirstInSubcategory = false;
                      
                      const expandedTr = isExpanded ? (
                        <tr key={`${skill.id}-expanded`} className="bg-muted/5 border-b border-border/50">
                          <td colSpan={9} className="px-6 py-4 shadow-inner">
                            <div className="space-y-4">
                              <div>
                                <h4 className="text-sm font-semibold text-foreground mb-1">Description</h4>
                                <p className="text-sm text-muted-foreground whitespace-pre-wrap">{skill.description || "No description provided."}</p>
                              </div>
                              {skill.levels && skill.levels.length > 0 && (
                                <div>
                                  <h4 className="text-sm font-semibold text-foreground mb-3">Level Requirements</h4>
                                  <div className="space-y-3">
                                    {skill.levels.map(l => (
                                      <div key={l.level} className="flex gap-4 items-start bg-background p-3 rounded-xl border border-border/50">
                                        <span className="inline-flex items-center justify-center min-w-[24px] h-6 rounded-md bg-primary/10 text-primary text-xs font-bold shrink-0">
                                          {l.level}
                                        </span>
                                        <p className="text-sm text-muted-foreground whitespace-pre-wrap pt-0.5">{l.description}</p>
                                      </div>
                                    ))}
                                  </div>
                                </div>
                              )}
                            </div>
                          </td>
                        </tr>
                      ) : null;

                      return (
                        <React.Fragment key={skill.id}>
                          {tr}
                          {expandedTr}
                        </React.Fragment>
                      );
                    });
                  })}
                </React.Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
