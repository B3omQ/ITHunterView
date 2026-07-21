import React from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { MapPin, DollarSign, Heart, Sparkles, MessageSquare, Eye } from 'lucide-react';
import type { SavedJobDto } from '@/types/job.types';
import { CompanyLogo } from '@/components/shared/CompanyLogo';

interface SavedJobCardProps {
  job: SavedJobDto;
  onUnsave: (id: string) => void;
  isUnsaving?: boolean;
}

export function SavedJobCard({ job, onUnsave, isUnsaving }: SavedJobCardProps) {
  const handleUnsave = (e: React.MouseEvent) => {
    e.preventDefault();
    onUnsave(job.jobId);
  };

  return (
    <Card className="hover:border-primary/50 transition-colors group">
      <CardContent className="p-4 flex flex-col gap-3">
        {/* Top Row: Info and Toggle */}
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-3 flex-1 min-w-0">
            <Link href={`/jobs/${job.jobId}`} className="shrink-0">
              <div className="w-11 h-11 rounded-lg overflow-hidden bg-muted flex items-center justify-center border border-border">
                <CompanyLogo src={job.logoUrl} alt={job.companyName} fallbackType="briefcase" fallbackIconClassName="text-slate-400 w-5 h-5" />
              </div>
            </Link>
            <div className="flex-1 min-w-0">
              <Link href={`/jobs/${job.jobId}`} className="font-semibold text-primary hover:underline line-clamp-1 text-base">
                {job.title}
              </Link>
              <p className="text-muted-foreground text-sm truncate">{job.companyName}</p>
              <div className="flex items-center gap-3 flex-wrap mt-0.5 text-xs text-muted-foreground">
                <span className="flex items-center gap-1">
                  <MapPin className="h-3 w-3 shrink-0" /> {job.location}
                </span>
                <span className="flex items-center gap-1">
                  <DollarSign className="h-3 w-3 shrink-0" /> {job.salaryText}
                </span>
                <span className="flex items-center gap-1 text-slate-400">
                  Saved on {new Date(job.savedAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          </div>
          <Button 
            variant="ghost" 
            size="icon" 
            onClick={handleUnsave}
            disabled={isUnsaving}
            title="Unsave Job"
            className="h-8 w-8 text-primary hover:text-primary/80 hover:bg-primary/10 transition-colors shrink-0"
          >
            <Heart className="w-4 h-4 fill-current" />
          </Button>
        </div>

        {/* Action Row */}
        <div className="flex flex-wrap items-center gap-2 pt-2.5 border-t border-border/50">
          <Link href={`/jobs/${job.jobId}`} className="flex-1 sm:flex-none">
            <Button variant="outline" size="sm" className="w-full gap-1.5">
              <Eye className="w-3.5 h-3.5" /> View Details
            </Button>
          </Link>
          <Link href={`/candidate/cv-matching/new?prefillJobId=${job.jobId}`} className="flex-1 sm:flex-none">
            <Button size="sm" className="w-full gap-1.5 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border border-indigo-200">
              <Sparkles className="w-3.5 h-3.5" /> Match CV
            </Button>
          </Link>
          <Link href={`/candidate/interview?prefillJobId=${job.jobId}&openModal=true`} className="flex-1 sm:flex-none">
            <Button size="sm" className="w-full gap-1.5 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border border-emerald-200">
              <MessageSquare className="w-3.5 h-3.5" /> Mock Interview
            </Button>
          </Link>
        </div>
      </CardContent>
    </Card>
  );
}
