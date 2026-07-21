import React from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { MapPin, DollarSign, Heart, Sparkles, MessageSquare } from 'lucide-react';
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
      <CardContent className="p-4 flex flex-col gap-4">
        {/* Top Row: Info and Toggle */}
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-start gap-4 flex-1">
            <Link href={`/jobs/${job.jobId}`} className="shrink-0">
              <div className="w-12 h-12 rounded overflow-hidden bg-slate-100 flex items-center justify-center border">
                <CompanyLogo src={job.logoUrl} alt={job.companyName} fallbackType="briefcase" fallbackIconClassName="text-slate-400 w-5 h-5" />
              </div>
            </Link>
            <div>
              <Link href={`/jobs/${job.jobId}`} className="font-semibold text-primary hover:underline line-clamp-1 text-base">
                {job.title}
              </Link>
              <p className="text-muted-foreground text-sm">{job.companyName}</p>
              <div className="flex items-center gap-4 mt-1 text-xs text-slate-500">
                <span className="flex items-center gap-1">
                  <MapPin className="w-3 h-3" /> {job.location}
                </span>
                <span className="flex items-center gap-1">
                  <DollarSign className="w-3 h-3" /> {job.salaryText}
                </span>
                <span className="text-slate-400">
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
            className="text-primary hover:text-primary/80 hover:bg-primary/10 transition-colors shrink-0"
          >
            <Heart className="w-5 h-5 fill-current" />
          </Button>
        </div>

        {/* Action Row */}
        <div className="flex flex-wrap items-center gap-2 pt-3 border-t border-border/50">
          <Link href={`/jobs/${job.jobId}`} className="flex-1 sm:flex-none">
            <Button variant="outline" size="sm" className="w-full">
              View Details
            </Button>
          </Link>
          <Link href={`/candidate/cv-matching/new?prefillJobId=${job.jobId}`} className="flex-1 sm:flex-none">
            <Button variant="secondary" size="sm" className="w-full gap-2 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border-indigo-200 border">
              <Sparkles className="w-4 h-4" /> Match CV
            </Button>
          </Link>
          <Link href={`/candidate/interview?prefillJobId=${job.jobId}&openModal=true`} className="flex-1 sm:flex-none">
            <Button variant="secondary" size="sm" className="w-full gap-2 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 border-emerald-200 border">
              <MessageSquare className="w-4 h-4" /> Mock Interview
            </Button>
          </Link>
        </div>
      </CardContent>
    </Card>
  );
}
