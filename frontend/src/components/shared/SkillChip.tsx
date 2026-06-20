'use client';

import React from 'react';
import { X } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

interface SkillChipProps {
  skillId: number;
  name: string;
  proficiencyLevel?: number | null;
  onDelete?: (skillId: number) => void;
  className?: string;
}

export function SkillChip({
  skillId,
  name,
  proficiencyLevel,
  onDelete,
  className,
}: SkillChipProps) {
  return (
    <Badge
      variant="secondary"
      className={cn(
        "pl-3 pr-2 py-1.5 rounded-full border border-border/40 bg-secondary/40 text-secondary-foreground flex items-center gap-1.5 text-xs font-semibold backdrop-blur-sm transition-all hover:bg-secondary/60 hover:border-border/80 group",
        className
      )}
    >
      <span>{name}</span>
      {proficiencyLevel !== undefined && proficiencyLevel !== null && (
        <span className="text-[10px] bg-primary/10 text-primary px-1.5 py-0.5 rounded-full font-mono font-bold">
          Lvl {proficiencyLevel}
        </span>
      )}
      {onDelete && (
        <button
          type="button"
          onClick={() => onDelete(skillId)}
          className="rounded-full p-0.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors duration-200 focus:outline-none"
        >
          <X className="w-3.5 h-3.5" />
        </button>
      )}
    </Badge>
  );
}
