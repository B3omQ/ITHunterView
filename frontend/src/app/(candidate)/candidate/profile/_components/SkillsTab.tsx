'use client';

import React, { useState, useEffect, useRef } from 'react';
import {
  useCandidateSkills,
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
  const { mutate: addSkill, isPending: isAddingSkill } = useAddSkill();
  const { mutate: removeSkill, isPending: isRemovingSkill } = useRemoveSkill();

  // Search autocomplete states
  const [keyword, setKeyword] = useState('');
  const [searchResults, setSearchResults] = useState<SkillSearchResponse[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Debounce search effect
  useEffect(() => {
    if (!keyword.trim()) {
      setSearchResults([]);
      setIsSearching(false);
      return;
    }

    setIsSearching(true);
    const timer = setTimeout(() => {
      candidateService
        .searchSkills(keyword, true)
        .then((res) => {
          if (res.success && res.data) {
            setSearchResults(res.data);
          }
        })
        .catch((err) => {
          console.error(err);
        })
        .finally(() => {
          setIsSearching(false);
        });
    }, 300);

    return () => clearTimeout(timer);
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

  if (isLoadingSkills) {
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
    addSkill(
      { skillId },
      {
        onSuccess: () => {
          setKeyword('');
          setShowDropdown(false);
        },
      }
    );
  };

  const handleRemoveSkillId = (skillId: number) => {
    removeSkill(skillId);
  };

  return (
    <div className="space-y-6">
      {/* Manage Skills Card */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
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
                className="pl-9 bg-background/50 border-border/60 focus-visible:ring-primary/30"
              />
              {isSearching && (
                <div className="absolute right-3 top-3">
                  <Loader2 className="w-4 h-4 animate-spin text-muted-foreground" />
                </div>
              )}
            </div>

            {/* Dropdown Menu */}
            {showDropdown && (keyword.trim() !== '' || searchResults.length > 0) && (
              <Card className="absolute left-0 right-0 mt-1.5 z-20 border border-border/40 bg-popover/95 backdrop-blur-md shadow-lg max-h-60 overflow-y-auto rounded-xl">
                <CardContent className="p-2">
                  {isSearching ? (
                    <div className="py-3 px-4 text-xs text-muted-foreground flex items-center gap-2">
                      <Loader2 className="w-3.5 h-3.5 animate-spin" /> Searching...
                    </div>
                  ) : searchResults.length === 0 ? (
                    <div className="py-3 px-4 text-xs text-muted-foreground">
                      No matching skills found
                    </div>
                  ) : (
                    <ul className="space-y-0.5">
                      {searchResults.map((s) => (
                        <li key={s.id}>
                          <button
                            type="button"
                            onClick={() => handleAddSkillId(s.id)}
                            disabled={isAddingSkill}
                            className="w-full text-left px-3 py-2 text-sm rounded-lg hover:bg-accent/80 transition-colors flex items-center justify-between"
                          >
                            <span className="font-medium text-foreground">{s.name}</span>
                            <Plus className="w-3.5 h-3.5 text-muted-foreground" />
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                </CardContent>
              </Card>
            )}
          </div>

          {/* User Skills Chips List */}
          <div className="space-y-2">
            <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Added Skills</h3>
            {skills.length === 0 ? (
              <p className="text-sm text-muted-foreground italic bg-muted/20 p-4 rounded-xl border border-dashed border-border/30">
                No skills added yet. Use the search box above to add your skills.
              </p>
            ) : (
              <div className="flex flex-wrap gap-2.5">
                {skills.map((s) => (
                  <SkillChip
                    key={s.skillId}
                    skillId={s.skillId}
                    name={s.name}
                    proficiencyLevel={s.proficiencyLevel}
                    onDelete={handleRemoveSkillId}
                    className="hover:scale-[1.02] active:scale-[0.98] transition-transform duration-100"
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
