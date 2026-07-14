'use client';

import React, { useState, useEffect, useRef, useMemo } from 'react';
import { cn } from '@/lib/utils';
import {
  useCandidateSkills,
  useAllMasterSkills,
  useAddSkill,
  useRemoveSkill,
} from '@/hooks/useCandidateProfile';
import { candidateService } from '@/services/candidate.service';
import type { SkillSearchResponse } from '@/types/candidate.types';
import { PageLoader } from '@/components/shared/PageLoader';
import { SkillChip } from '@/components/shared/SkillChip';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

import { Award, Plus, Search, Loader2 } from 'lucide-react';

export function SkillsTab() {
  const { data: skills, isLoading: isLoadingSkills, isError: isErrorSkills } = useCandidateSkills();
  const { data: allMasterSkills, isLoading: isLoadingMasterSkills } = useAllMasterSkills();
  const { mutate: addSkill, isPending: isAddingSkill } = useAddSkill();
  const { mutate: removeSkill, isPending: isRemovingSkill } = useRemoveSkill();

  // Search autocomplete states
  const [keyword, setKeyword] = useState('');
  const [showDropdown, setShowDropdown] = useState(false);
  const [focusedIndex, setFocusedIndex] = useState(-1);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Derive search results using useMemo (zero-latency, no extra re-renders)
  const searchResults = useMemo(() => {
    if (!keyword.trim() || !allMasterSkills) {
      return [];
    }

    const lowerKeyword = keyword.toLowerCase();
    return allMasterSkills
      .filter((s) => s.name.toLowerCase().includes(lowerKeyword))
      // Exclude skills the user already has
      .filter((s) => !skills?.some((owned) => owned.skillId === s.id))
      .slice(0, 20); // Top 20 results
  }, [keyword, allMasterSkills, skills]);

  // Reset focus when search changes
  useEffect(() => {
    setFocusedIndex(-1);
  }, [keyword]);

  // Click outside listener for dropdown
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setShowDropdown(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  if (isLoadingSkills || isLoadingMasterSkills) {
    return <PageLoader message="Loading skills..." />;
  }

  if (isErrorSkills || !skills) {
    return (
      <div className="p-8 border rounded-xl bg-card text-center text-muted-foreground">
        Failed to load skills. Please try again.
      </div>
    );
  }

  const handleAddSkillId = (skillId: number) => {
    if (isAddingSkill) return;
    addSkill(
      { skillId },
      {
        onSuccess: () => {
          setKeyword('');
          setShowDropdown(false);
          setFocusedIndex(-1);
        },
      }
    );
  };

  const handleRemoveSkillId = (skillId: number) => {
    removeSkill(skillId);
  };

  return (
    <div className="space-y-6 max-w-3xl mx-auto w-full">
      {/* Manage Skills Card */}
      <Card>
        <CardHeader className="border-b pb-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-md bg-muted text-muted-foreground">
              <Award className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Skills</CardTitle>
              <CardDescription className="text-xs">Add your professional skills and expertise</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6 space-y-6">
          {/* Autocomplete Input Container */}
          <div className="relative max-w-md" ref={dropdownRef}>
            <div className="relative">
              <Search className="absolute left-3 top-3 w-4 h-4 text-muted-foreground" />
              <Input
                placeholder="Type a skill (e.g. React, Docker...)"
                value={keyword}
                onChange={(e) => {
                  setKeyword(e.target.value);
                  setShowDropdown(true);
                }}
                onFocus={() => setShowDropdown(true)}
                onKeyDown={(e) => {
                  if (!showDropdown || searchResults.length === 0) return;
                  if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    setFocusedIndex((prev) => (prev < searchResults.length - 1 ? prev + 1 : prev));
                  } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    setFocusedIndex((prev) => (prev > 0 ? prev - 1 : -1));
                  } else if (e.key === 'Enter') {
                    e.preventDefault();
                    if (focusedIndex >= 0 && searchResults[focusedIndex]) {
                      handleAddSkillId(searchResults[focusedIndex].id);
                    }
                  } else if (e.key === 'Escape') {
                    setShowDropdown(false);
                  }
                }}
                className="pl-9"
              />
            </div>

            {/* Dropdown Menu */}
            {showDropdown && (keyword.trim() !== '' || searchResults.length > 0) && (
              <div className="absolute left-0 right-0 mt-1.5 z-20 border bg-popover text-popover-foreground shadow-md max-h-60 overflow-y-auto rounded-md p-1">
                {searchResults.length === 0 ? (
                  <div className="py-3 px-4 text-sm text-muted-foreground text-center">
                    No matching skills found
                  </div>
                ) : (
                  <ul className="space-y-0.5">
                    {searchResults.map((s, index) => (
                      <li key={s.id}>
                        <button
                          type="button"
                          onClick={() => handleAddSkillId(s.id)}
                          disabled={isAddingSkill}
                          className={cn(
                            "w-full text-left px-3 py-2 text-sm rounded-sm transition-colors flex items-center justify-between",
                            focusedIndex === index ? "bg-accent text-accent-foreground" : "hover:bg-accent/50"
                          )}
                        >
                          <span className="font-medium">{s.name}</span>
                          <Plus className={cn("w-3.5 h-3.5", focusedIndex === index ? "text-foreground" : "text-muted-foreground")} />
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}
          </div>

          {/* User Skills Chips List */}
          <div className="space-y-3">
            <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Added Skills</h3>
            {skills.length === 0 ? (
              <div className="text-sm text-muted-foreground py-8 text-center bg-muted/30 rounded-md">
                No skills added yet. Use the search box above to add your skills.
              </div>
            ) : (
              <div className="flex flex-wrap gap-2">
                {skills.map((s) => (
                  <SkillChip
                    key={s.skillId}
                    skillId={s.skillId}
                    name={s.name}
                    proficiencyLevel={s.proficiencyLevel}
                    onDelete={handleRemoveSkillId}
                  />
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
