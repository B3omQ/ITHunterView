import React from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DollarSign, Briefcase, Heart, Monitor, CheckSquare, MapPin } from 'lucide-react';
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

const getDaysAgo = (dateStr?: string) => {
  if (!dateStr) return null;
  const days = Math.floor((Date.now() - new Date(dateStr).getTime()) / (1000 * 60 * 60 * 24));
  return days > 0 ? `Posted ${days} days ago` : 'Posted today';
};

export function JobCard({ job, isCandidateMode = false, onSave, onUnsave, isLoadingAction, isActive, onClick }: JobCardProps) {
  const jobLink = isCandidateMode ? `/candidate/jobs/${job.id}` : `/jobs/${job.id}`;

  return (
    <Link href={jobLink} onClick={onClick} className="block h-full">
      <Card className={`relative overflow-hidden transition-all group h-full flex flex-col bg-white border ${isActive ? 'border-primary shadow-md' : 'border-slate-200 hover:border-primary/50 hover:shadow-md'}`}>
        {/* Active state styling */}
        {isActive && (
          <>
            <div className="absolute left-0 top-0 bottom-0 w-1.5 bg-primary" />
            <svg className="absolute right-0 top-1/2 -translate-y-1/2 w-2 h-4 text-primary" viewBox="0 0 8 16" fill="currentColor">
              <polygon points="8,0 0,8 8,16" />
            </svg>
          </>
        )}
        
        <CardContent className={`px-4 py-2 flex-1 flex flex-col relative ${isActive ? 'pl-5' : ''}`}>

          <div className="flex flex-col gap-2">
            {/* Posted time */}
            {job.publishedAt && (
              <span className="text-sm text-slate-400 font-medium">
                {getDaysAgo(job.publishedAt)}
              </span>
            )}

            {/* Title */}
            <h3 className="font-bold text-lg text-slate-900 group-hover:text-primary transition-colors line-clamp-2 pr-8 leading-tight">
              {job.title}
            </h3>

            {/* Company Info */}
            <div className="flex items-center gap-2 mt-1">
              <div className="w-8 h-8 rounded overflow-hidden bg-white flex items-center justify-center shrink-0 border border-slate-200 p-1">
                {job.logoUrl ? (
                  <img src={job.logoUrl} alt={job.companyName} className="object-contain w-full h-full" />
                ) : (
                  <Briefcase className="text-slate-400 w-4 h-4" />
                )}
              </div>
              <p className="text-slate-600 text-sm font-medium uppercase tracking-wide line-clamp-1">{job.companyName}</p>
            </div>

            {/* Salary */}
            <div className="flex items-center gap-2 mt-1">
              <DollarSign className="w-4 h-4 text-slate-700" />
              <span className="font-semibold text-sm underline cursor-pointer decoration-slate-400 underline-offset-2 text-slate-700">
                {job.minSalary || job.maxSalary
                  ? `${job.minSalary?.toLocaleString() || "0"} - ${job.maxSalary?.toLocaleString() || "∞"} ${job.currency}`
                  : "Sign in to view salary"}
              </span>
            </div>
          </div>

          <div className="border-b border-dashed border-slate-200 my-3"></div>

          <div className="flex flex-col gap-1.5 mb-3">
            {job.level && (
              <div className="flex items-center gap-2 text-slate-600 text-sm">
                <CheckSquare className="w-4 h-4 text-slate-400 shrink-0" />
                <span className="leading-snug underline decoration-slate-300 underline-offset-4">{job.level === 'Fresher' ? 'Fresher Accepted' : job.level}</span>
              </div>
            )}
            {job.jobExpertise && (
              <div className="flex items-center gap-2 text-slate-600 text-sm">
                <Briefcase className="w-4 h-4 text-slate-400 shrink-0" />
                <span className="leading-snug underline decoration-slate-300 underline-offset-4">{job.jobExpertise}</span>
              </div>
            )}
            <div className="flex items-center gap-2 text-slate-600 text-sm">
              <Monitor className="w-4 h-4 text-slate-400 shrink-0" />
              <div className="flex items-center gap-1.5 flex-wrap leading-snug">
                <span>{job.workingModel || 'At office'}</span>
                <span className="text-slate-300">•</span>
                <MapPin className="w-3.5 h-3.5 text-slate-400" />
                <span>{job.location}</span>
              </div>
            </div>
          </div>

          {/* Skills */}
          <div className="mt-auto">
            <div className="flex flex-wrap gap-1.5">
              {job.skills && job.skills.length > 0 ? (
                <>
                  {job.skills.slice(0, 5).map(skill => (
                    <Badge key={skill} variant="outline" className="font-normal border-slate-200 text-slate-700 bg-white px-3">
                      {skill}
                    </Badge>
                  ))}
                  {job.skills.length > 5 && (
                    <Badge variant="outline" className="font-normal border-slate-200 text-slate-700 bg-slate-50 px-3">
                      +{job.skills.length - 5}
                    </Badge>
                  )}
                </>
              ) : (
                job.jobDomain && job.jobDomain.slice(0, 5).map(domain => (
                  <Badge key={domain} variant="outline" className="font-normal border-slate-200 text-slate-700 bg-white px-3">
                    {domain}
                  </Badge>
                ))
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
