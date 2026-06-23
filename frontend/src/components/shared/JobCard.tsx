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
  isActive?: boolean;
  onClick?: (e: React.MouseEvent) => void;
}

export function JobCard({ job, isCandidateMode = false, onSave, onUnsave, isLoadingAction, isActive, onClick }: JobCardProps) {
  const handleSaveToggle = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!isCandidateMode) {
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
    <Link href={jobLink} onClick={onClick} className="block h-full">
      <Card className={`transition-all group h-full flex flex-col ${isActive ? 'border-primary ring-1 ring-primary bg-primary/5 shadow-md' : 'hover:border-primary/50 hover:shadow-md'}`}>
        <CardContent className="p-5 flex-1 flex flex-col">
          <div className="flex justify-between items-start gap-4">
            <div className="flex gap-4">
              <div className="w-14 h-14 rounded-lg overflow-hidden bg-white flex items-center justify-center shrink-0 border shadow-sm p-1">
                {job.logoUrl ? (
                  <img src={job.logoUrl} alt={job.companyName} className="object-contain w-full h-full" />
                ) : (
                  <Briefcase className="text-slate-400 w-6 h-6" />
                )}
              </div>
              <div>
                <h3 className="font-semibold text-lg text-slate-900 group-hover:text-primary transition-colors line-clamp-1">
                  {job.title}
                </h3>
                <p className="text-muted-foreground text-sm font-medium mt-0.5">{job.companyName}</p>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1 mt-2 text-sm text-slate-600">
                  <span className="flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5 text-slate-400" /> {job.location}
                  </span>
                  {(job.minSalary || job.maxSalary) && (
                    <span className="flex items-center gap-1 font-medium text-emerald-600">
                      <DollarSign className="w-3.5 h-3.5" /> 
                      {job.minSalary ? `${job.minSalary}` : 'Up to'} - {job.maxSalary ? `${job.maxSalary}` : 'Negotiable'} {job.currency}
                    </span>
                  )}
                </div>
              </div>
            </div>
            
            <Button 
              variant="ghost" 
              size="icon" 
              className="shrink-0 -mt-2 -mr-2"
              onClick={handleSaveToggle}
              disabled={isLoadingAction}
              title={job.isSaved ? "Unsave Job" : "Save Job"}
            >
              {job.isSaved ? (
                <BookmarkCheck className="w-5 h-5 text-primary fill-primary" />
              ) : (
                <Bookmark className="w-5 h-5 text-slate-400 group-hover:text-primary transition-colors" />
              )}
            </Button>
          </div>
          
          <div className="mt-4 flex flex-col gap-3 justify-end flex-1">
            {/* Skills section */}
            {job.skills && job.skills.length > 0 && (
              <div className="flex flex-wrap gap-1.5">
                {job.skills.slice(0, 4).map(skill => (
                  <Badge key={skill} variant="secondary" className="bg-slate-100 text-slate-600 font-normal hover:bg-slate-200">
                    {skill}
                  </Badge>
                ))}
                {job.skills.length > 4 && (
                  <Badge variant="secondary" className="bg-slate-100 text-slate-600 font-normal hover:bg-slate-200">
                    +{job.skills.length - 4}
                  </Badge>
                )}
              </div>
            )}
            
            {/* Meta tags */}
            <div className="flex items-center justify-between pt-3 border-t border-slate-100 mt-auto">
              <Badge variant="outline" className="text-slate-500 bg-white border-slate-200">
                {job.jobType.replace('_', ' ').toUpperCase()}
              </Badge>
              {job.publishedAt && (
                <span className="text-xs text-slate-400 font-medium">
                  {new Date(job.publishedAt).toLocaleDateString()}
                </span>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
