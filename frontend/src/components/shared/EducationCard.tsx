'use client';

import React from 'react';
import { Calendar, Award, Pencil, Trash, GraduationCap } from 'lucide-react';
import type { CandidateEducation } from '@/types/candidate.types';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

interface EducationCardProps {
  education: CandidateEducation;
  onEdit?: (education: CandidateEducation) => void;
  onDelete?: (id: string) => void;
}

export function EducationCard({ education, onEdit, onDelete }: EducationCardProps) {
  const formatPeriod = (start: string | null, end: string | null) => {
    const formatDate = (dateStr: string | null) => {
      if (!dateStr) return '';
      try {
        const date = new Date(dateStr);
        return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
      } catch {
        return dateStr;
      }
    };

    const startFormatted = formatDate(start) || 'N/A';
    const endFormatted = formatDate(end) || 'Present';

    return `${startFormatted} — ${endFormatted}`;
  };

  return (
    <div className="flex gap-4 py-6 border-b border-border/40 last:border-0 items-start justify-between group">
        <div className="flex gap-4 items-start flex-1">
          {/* Logo Academy Placeholder */}
          <div className="w-12 h-12 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-center text-primary flex-shrink-0">
            <GraduationCap className="w-6 h-6" />
          </div>

          {/* Education Details */}
          <div className="space-y-1.5 flex-1">
            <div className="flex flex-wrap items-center gap-3">
              <h3 className="text-base sm:text-lg font-bold text-foreground tracking-tight leading-none capitalize">
                {education.degree}
              </h3>
              {education.gpa !== null && education.gpa !== undefined && (
                <Badge variant="outline" className="text-[10px] py-0 px-2 font-mono font-bold uppercase border-primary/20 bg-primary/5 text-primary shrink-0">
                  GPA: {education.gpa}/{education.maxGpa || 4.0}
                </Badge>
              )}
            </div>

            <p className="text-sm font-semibold text-muted-foreground/90 capitalize mt-1.5">
              {education.institutionName}
              {education.majorName && (
                <span className="text-muted-foreground/60 font-medium normal-case"> • {education.majorName}</span>
              )}
            </p>

            <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-muted-foreground/80 font-medium mt-1.5">
              <span className="flex items-center gap-1">
                <Calendar className="w-3.5 h-3.5 text-primary/70" />
                {formatPeriod(education.startDate, education.endDate)}
              </span>
            </div>

            {education.description && (
              <p className="text-sm text-muted-foreground/90 leading-relaxed pt-2.5 whitespace-pre-wrap max-w-2xl border-t border-border/10 mt-2">
                {education.description}
              </p>
            )}
          </div>
        </div>

        {/* Action Buttons */}
        {(onEdit || onDelete) && (
          <div className="flex items-center gap-1 ml-2">
            {onEdit && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => onEdit(education)}
                className="w-8 h-8 rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground"
              >
                <Pencil className="w-4 h-4" />
              </Button>
            )}
            {onDelete && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => onDelete(education.id)}
                className="w-8 h-8 rounded-lg hover:bg-destructive/10 text-muted-foreground hover:text-destructive"
              >
                <Trash className="w-4 h-4" />
              </Button>
            )}
          </div>
        )}
    </div>
  );
}
