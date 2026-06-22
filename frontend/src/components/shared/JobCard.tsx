import React from 'react';
import Link from 'next/link';
import { Card, CardContent, CardFooter } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { MapPin, DollarSign, Bookmark, BookmarkCheck, Briefcase } from 'lucide-react';
import type { JobCardDto } from '@/types/job.types';

interface JobCardProps {
  job: JobCardDto;
  isCandidateMode?: boolean;
  onSave?: (id: string) => void;
  onUnsave?: (id: string) => void;
  isLoadingAction?: boolean;
}

export function JobCard({ job, isCandidateMode = false, onSave, onUnsave, isLoadingAction }: JobCardProps) {
  const handleSaveToggle = (e: React.MouseEvent) => {
    e.preventDefault();
    if (!isCandidateMode) {
      // Typically redirect to login or show toast
      window.location.href = '/login';
      return;
    }
    
    if (job.isSaved) {
      onUnsave?.(job.id);
    } else {
      onSave?.(job.id);
    }
  };

  const jobLink = isCandidateMode ? `/candidate/jobs/${job.id}` : `/jobs/${job.id}`;

  return (
    <Link href={jobLink}>
      <Card className="hover:border-primary/50 transition-colors group h-full flex flex-col">
        <CardContent className="p-6 flex-1">
          <div className="flex justify-between items-start gap-4">
            <div className="flex gap-4">
              <div className="w-16 h-16 rounded overflow-hidden bg-slate-100 flex items-center justify-center shrink-0 border">
                {job.logoUrl ? (
                  <img src={job.logoUrl} alt={job.companyName} className="object-contain w-full h-full" />
                ) : (
                  <Briefcase className="text-slate-400" />
                )}
              </div>
              <div>
                <h3 className="font-semibold text-lg text-primary group-hover:underline line-clamp-1">
                  {job.title}
                </h3>
                <p className="text-muted-foreground text-sm font-medium">{job.companyName}</p>
                <div className="flex items-center gap-2 mt-2 text-sm text-slate-500">
                  <span className="flex items-center gap-1">
                    <MapPin className="w-4 h-4" /> {job.location}
                  </span>
                  {(job.minSalary || job.maxSalary) && (
                    <span className="flex items-center gap-1">
                      <DollarSign className="w-4 h-4" /> 
                      {job.minSalary ? `${job.minSalary}` : 'Up to'} - {job.maxSalary ? `${job.maxSalary}` : 'Negotiable'} {job.currency}
                    </span>
                  )}
                </div>
              </div>
            </div>
            
            <Button 
              variant="ghost" 
              size="icon" 
              className="shrink-0"
              onClick={handleSaveToggle}
              disabled={isLoadingAction}
              title={job.isSaved ? "Unsave Job" : "Save Job"}
            >
              {job.isSaved ? (
                <BookmarkCheck className="w-5 h-5 text-primary fill-primary" />
              ) : (
                <Bookmark className="w-5 h-5 text-muted-foreground group-hover:text-primary" />
              )}
            </Button>
          </div>
          
          <div className="flex flex-wrap gap-2 mt-4">
            <Badge variant="secondary">{job.jobType.replace('_', ' ').toUpperCase()}</Badge>
            {job.publishedAt && (
              <Badge variant="outline" className="text-muted-foreground">
                Posted {new Date(job.publishedAt).toLocaleDateString()}
              </Badge>
            )}
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
