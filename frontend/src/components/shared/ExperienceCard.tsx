'use client';

import React from 'react';
import { Calendar, MapPin, Pencil, Trash, Building2 } from 'lucide-react';
import type { CandidateExperience } from '@/types/candidate.types';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';

interface ExperienceCardProps {
  experience: CandidateExperience;
  onEdit?: (experience: CandidateExperience) => void;
  onDelete?: (id: string) => void;
}

export function ExperienceCard({ experience, onEdit, onDelete }: ExperienceCardProps) {
  const formatPeriod = (start: string | null, end: string | null, isCurrent: boolean) => {
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
    const endFormatted = isCurrent ? 'Present' : formatDate(end) || 'N/A';

    return `${startFormatted} — ${endFormatted}`;
  };

  const getEmploymentTypeLabel = (type: string | null) => {
    if (!type) return null;
    return type.replace('_', ' ').toUpperCase();
  };

  return (
    <Card className="group border border-border/30 bg-card/40 hover:bg-card/75 backdrop-blur-sm transition-all duration-300 rounded-xl overflow-hidden shadow-sm hover:shadow-md">
      <CardContent className="p-5 sm:p-6 flex gap-4 items-start justify-between">
        <div className="flex gap-4 items-start flex-1">
          {/* Company Logo Placeholder */}
          <div className="w-12 h-12 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-center text-primary flex-shrink-0">
            <Building2 className="w-6 h-6" />
          </div>

          {/* Job Details */}
          <div className="space-y-1.5 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-base sm:text-lg font-bold text-foreground tracking-tight leading-snug">
                {experience.title}
              </h3>
              {experience.employmentType && (
                <Badge variant="outline" className="text-[10px] py-0 px-2 font-bold uppercase border-primary/20 bg-primary/5 text-primary">
                  {getEmploymentTypeLabel(experience.employmentType)}
                </Badge>
              )}
            </div>

            <p className="text-sm font-semibold text-muted-foreground/90 flex items-center gap-1.5">
              {experience.companyName}
            </p>

            <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-muted-foreground/80 font-medium">
              <span className="flex items-center gap-1">
                <Calendar className="w-3.5 h-3.5 text-primary/70" />
                {formatPeriod(experience.startDate, experience.endDate, experience.isCurrent)}
              </span>
              {experience.location && (
                <span className="flex items-center gap-1">
                  <MapPin className="w-3.5 h-3.5 text-primary/70" />
                  {experience.location}
                </span>
              )}
            </div>

            {experience.description && (
              <p className="text-sm text-muted-foreground/90 leading-relaxed pt-2.5 whitespace-pre-wrap max-w-2xl border-t border-border/10 mt-2">
                {experience.description}
              </p>
            )}
          </div>
        </div>

        {/* Action Buttons */}
        {(onEdit || onDelete) && (
          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity duration-200 focus-within:opacity-100 ml-2">
            {onEdit && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => onEdit(experience)}
                className="w-8 h-8 rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground"
              >
                <Pencil className="w-4 h-4" />
              </Button>
            )}
            {onDelete && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => onDelete(experience.id)}
                className="w-8 h-8 rounded-lg hover:bg-destructive/10 text-muted-foreground hover:text-destructive"
              >
                <Trash className="w-4 h-4" />
              </Button>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
