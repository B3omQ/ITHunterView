'use client';

import React from 'react';
import { Calendar, ExternalLink, Pencil, Trash, Award } from 'lucide-react';
import type { CandidateCertification } from '@/types/candidate.types';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

interface CertificationCardProps {
  certification: CandidateCertification;
  onEdit?: (certification: CandidateCertification) => void;
  onDelete?: (id: string) => void;
}

export function CertificationCard({ certification, onEdit, onDelete }: CertificationCardProps) {
  const formatPeriod = (issue: string | null, expire: string | null) => {
    const formatDate = (dateStr: string | null) => {
      if (!dateStr) return '';
      try {
        const date = new Date(dateStr);
        return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
      } catch {
        return dateStr;
      }
    };

    const issueFormatted = formatDate(issue) || 'N/A';
    const expireFormatted = expire ? formatDate(expire) : 'No expiry';

    return `Issued ${issueFormatted} • ${expireFormatted === 'No expiry' ? 'No Expiry Date' : `Expires ${expireFormatted}`}`;
  };

  return (
    <div className="flex gap-4 py-6 border-b border-border/40 last:border-0 items-start justify-between group">
        <div className="flex gap-4 items-start flex-1">
          {/* Logo Cert Placeholder */}
          <div className="w-12 h-12 rounded-xl bg-primary/10 border border-primary/20 flex items-center justify-center text-primary flex-shrink-0">
            <Award className="w-6 h-6" />
          </div>

          {/* Certification Details */}
          <div className="space-y-1.5 flex-1">
            <h3 className="text-base sm:text-lg font-bold text-foreground tracking-tight leading-none capitalize">
              {certification.name}
            </h3>

            <p className="text-sm font-semibold text-muted-foreground/90 capitalize mt-1.5">
              {certification.issuingOrganization}
            </p>

            <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-muted-foreground/80 font-medium mt-1.5">
              <span className="flex items-center gap-1">
                <Calendar className="w-3.5 h-3.5 text-primary/70" />
                {formatPeriod(certification.issueDate, certification.expirationDate)}
              </span>
            </div>

            {certification.credentialUrl && (
              <div className="pt-2">
                <a
                  href={certification.credentialUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 text-xs font-bold text-primary hover:text-primary/80 transition-colors"
                >
                  View Credential
                  <ExternalLink className="w-3 h-3" />
                </a>
              </div>
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
                onClick={() => onEdit(certification)}
                className="w-8 h-8 rounded-lg hover:bg-muted text-muted-foreground hover:text-foreground"
              >
                <Pencil className="w-4 h-4" />
              </Button>
            )}
            {onDelete && (
              <Button
                variant="ghost"
                size="icon"
                onClick={() => onDelete(certification.id)}
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
