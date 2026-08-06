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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { useTranslations } from 'next-intl';

import { Award, Plus, Search, Loader2, X } from 'lucide-react';

export function SkillsTab() {
  const { data: skills, isLoading: isLoadingSkills, isError: isErrorSkills } = useCandidateSkills();
  const { data: allMasterSkills, isLoading: isLoadingMasterSkills } = useAllMasterSkills();
  const { mutate: addSkill, isPending: isAddingSkill } = useAddSkill();
  const { mutate: removeSkill, isPending: isRemovingSkill } = useRemoveSkill();
  const t = useTranslations("CandidateProfile");

  // Search autocomplete states
  const [keyword, setKeyword] = useState('');
  const [showDropdown, setShowDropdown] = useState(false);
  const [isAdding, setIsAdding] = useState(false);
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
    return <PageLoader message={t('loadingSkills')} />;
  }

  if (isErrorSkills || !skills) {
    return (
      <div className="p-8 border rounded-xl bg-card text-center text-muted-foreground">
        {t('skillsError')}
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
    <div className="space-y-6 w-full">
      {/* Manage Skills Card */}
      <Card>
        <CardHeader className="border-b pb-4 flex flex-row items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <CardTitle className="text-xl font-bold mt-1">{t('skills')}</CardTitle>
            {skills.length === 0 && (
              <CardDescription className="text-sm">{t('addSkillsDesc')}</CardDescription>
            )}
          </div>
          <Button
            onClick={() => setIsAdding(true)}
            variant="outline"
            size="icon"
            className="rounded-full border-primary text-primary hover:bg-primary/10 w-8 h-8 shrink-0 transition-colors mt-0"
          >
            <Plus className="w-5 h-5" />
          </Button>
        </CardHeader>
        <CardContent className={skills.length === 0 ? "p-0" : "px-6 py-4 space-y-4"}>

          {/* User Skills Chips List */}
          {skills.length > 0 && (
            <div className="space-y-3">
              <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">{t('addedSkills')}</h3>
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
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog disablePointerDismissal open={isAdding} onOpenChange={setIsAdding}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle className="text-xl">{t('addSkills')}</DialogTitle>
            <DialogDescription>
              {t('searchSkillsDesc')}
            </DialogDescription>
          </DialogHeader>
          
          <div className="pt-2 relative" ref={dropdownRef}>
            <div className="border border-border/60 rounded-lg px-3 py-2.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm flex items-center">
              <Search className="w-4 h-4 text-muted-foreground mr-2 shrink-0" />
              <input
                placeholder={t('typeSkillPlaceholder')}
                value={keyword}
                autoFocus
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
                className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
              />
            </div>

            {/* Dropdown Menu */}
            {showDropdown && (keyword.trim() !== '' || searchResults.length > 0) && (
              <div className="absolute left-0 right-0 mt-1.5 z-20 border bg-popover text-popover-foreground shadow-md max-h-60 overflow-y-auto rounded-md p-1">
                {searchResults.length === 0 ? (
                  <div className="py-3 px-4 text-sm text-muted-foreground text-center">
                    {t('noMatchingSkills')}
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
          
          {skills.length > 0 && (
            <div className="space-y-3 mt-6">
              <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">{t('addedSkills')}</h3>
              <div className="flex flex-wrap gap-2 max-h-[200px] overflow-y-auto pr-2 pb-2">
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
            </div>
          )}
          
          <div className="flex justify-end pt-4 mt-2 border-t">
            <Button onClick={() => setIsAdding(false)}>{t('done')}</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
