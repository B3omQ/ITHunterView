import React from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DollarSign, Briefcase, Heart, Monitor, CheckSquare, MapPin, Flame } from 'lucide-react';
import type { JobCardDto } from '@/types/job.types';
import { CompanyLogo } from '@/components/shared/CompanyLogo';

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
  const jobLink = isCandidateMode ? `/jobs/${job.id}` : `/jobs/${job.id}`;
  const isTop = job.isPushedTop || (job.pushedTopUntil && new Date(job.pushedTopUntil) >= new Date());

  return (
    <Link href={jobLink} onClick={onClick} className="block h-full">
      <Card className={`relative overflow-hidden transition-all group h-full flex flex-col bg-white border ${isActive ? 'border-primary shadow-md' : isTop ? 'border-amber-400/80 shadow-md shadow-amber-500/15 hover:border-amber-500 hover:shadow-lg hover:shadow-amber-500/25 bg-gradient-to-br from-amber-50/40 via-white to-orange-50/20 dark:from-amber-950/20 dark:via-zinc-900 dark:to-orange-950/10' : 'border-slate-200 hover:border-primary/50 hover:shadow-md'}`}>
        {/* Active state styling */}
        {isActive && (
          <>
            <div className="absolute left-0 top-0 bottom-0 w-1.5 bg-primary" />
            <svg className="absolute right-0 top-1/2 -translate-y-1/2 w-2 h-4 text-primary" viewBox="0 0 8 16" fill="currentColor">
              <polygon points="8,0 0,8 8,16" />
            </svg>
          </>
        )}
        {isTop && !isActive && (
          <div className="absolute top-0 right-0 w-24 h-24 overflow-hidden pointer-events-none">
            <div className="absolute top-2 -right-7 rotate-45 bg-gradient-to-r from-amber-500 to-orange-600 text-white font-bold text-[9px] uppercase px-8 py-0.5 shadow-md flex items-center justify-center tracking-wider">
              TOP 24H
            </div>
          </div>
        )}
        
        <CardContent className={`px-4 py-3 flex-1 flex flex-col relative ${isActive ? 'pl-5' : ''}`}>

          <div className="flex flex-col gap-2">
            {/* Posted time & Status */}
            <div className="flex items-center gap-2 flex-wrap">
              {job.publishedAt && (
                <span className="text-sm text-slate-400 font-medium">
                  {getDaysAgo(job.publishedAt)}
                </span>
              )}
              {isTop && (
                <Badge variant="secondary" className="bg-gradient-to-r from-amber-500/15 via-orange-500/15 to-red-500/15 text-orange-600 dark:text-orange-400 border border-orange-500/30 px-2 py-0 text-[11px] font-bold gap-1 animate-pulse">
                  <Flame className="h-3 w-3 fill-orange-500 text-orange-500" />
                  Nổi bật Top
                </Badge>
              )}
              {job.isApplied && (
                <Badge variant="secondary" className="bg-emerald-100 text-emerald-700 hover:bg-emerald-100 border-none px-2 py-0.5 text-xs font-semibold ml-auto">
                  Applied
                </Badge>
              )}
              {job.status === 'EXPIRED' && (
                <Badge variant="secondary" className="bg-slate-200 text-slate-600 hover:bg-slate-200 border-none px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider ml-auto">
                  Hết hạn
                </Badge>
              )}
            </div>

            {/* Title */}
            <h3 className="font-bold text-lg text-slate-900 group-hover:text-primary transition-colors line-clamp-2 pr-8 leading-tight">
              {job.title}
            </h3>

            {/* Company Info */}
            <div className="flex items-center gap-2 mt-1">
              <div className="w-8 h-8 rounded overflow-hidden bg-white flex items-center justify-center shrink-0 border border-slate-200 p-1">
                <CompanyLogo src={job.logoUrl} alt={job.companyName} fallbackType="briefcase" fallbackIconClassName="text-slate-400 w-4 h-4" />
              </div>
              <p className="text-slate-600 text-sm font-medium uppercase tracking-wide line-clamp-1">{job.companyName}</p>
            </div>

            {/* Salary */}
            <div className="flex items-center gap-2 mt-1">
              <DollarSign className="w-4 h-4 text-slate-700" />
              <span className="font-semibold text-sm underline cursor-pointer decoration-slate-400 underline-offset-2 text-slate-700">
                {!job.minSalary && !job.maxSalary 
                  ? "Negotiable" 
                  : (job.minSalary && !job.maxSalary)
                    ? `From ${job.minSalary.toLocaleString()} ${job.currency}`
                    : (!job.minSalary && job.maxSalary)
                      ? `Up to ${job.maxSalary.toLocaleString()} ${job.currency}`
                      : `${job.minSalary?.toLocaleString()} - ${job.maxSalary?.toLocaleString()} ${job.currency}`
                }
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
