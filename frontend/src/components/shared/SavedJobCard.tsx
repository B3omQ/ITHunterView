import React, { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { MapPin, DollarSign, Heart, Sparkles, MessageSquare, Eye, MoreHorizontal } from 'lucide-react';
import type { SavedJobDto } from '@/types/job.types';
import { CompanyLogo } from '@/components/shared/CompanyLogo';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';

interface SavedJobCardProps {
  job: SavedJobDto;
  onUnsave: (id: string) => void;
  isUnsaving?: boolean;
}

export function SavedJobCard({ job, onUnsave, isUnsaving }: SavedJobCardProps) {
  const [showDialog, setShowDialog] = useState(false);
  const [popoverOpen, setPopoverOpen] = useState(false);

  const handleConfirmUnsave = (e: React.MouseEvent) => {
    e.preventDefault();
    onUnsave(job.jobId);
    setShowDialog(false);
  };

  return (
    <Card className="hover:border-primary/50 transition-colors group">
      <CardContent className="flex flex-col gap-3">
        {/* Single Row Layout */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-3 flex-1 min-w-0">
            <Link href={`/jobs/${job.jobId}`} className="shrink-0">
              <div className="w-11 h-11 rounded-lg overflow-hidden bg-muted flex items-center justify-center border border-border">
                <CompanyLogo src={job.logoUrl} alt={job.companyName} fallbackType="briefcase" fallbackIconClassName="text-slate-400 w-5 h-5" />
              </div>
            </Link>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <Link href={`/jobs/${job.jobId}`} className="font-medium text-base text-foreground group-hover:text-primary transition-colors line-clamp-1 leading-snug">
                  {job.title}
                </Link>
              </div>
              <p className="text-slate-600 text-sm font-medium line-clamp-1 mt-0.5">{job.companyName}</p>
              <div className="flex items-center gap-4 flex-wrap mt-1 text-sm text-slate-600">
                <span className="flex items-center gap-1.5">
                  <MapPin className="h-4 w-4 shrink-0 text-slate-400" /> {job.location}
                </span>
                <span className="flex items-center gap-1.5">
                  <DollarSign className="h-4 w-4 shrink-0 text-slate-400" /> {job.salaryText}
                </span>
                <span className="flex items-center gap-1.5 text-slate-500">
                  Saved on {new Date(job.savedAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          </div>

          {/* Action Zone (Right side) */}
          <div className="flex items-center gap-2 shrink-0">
            <Link href={`/jobs/${job.jobId}`}>
              <Button size="sm" variant="outline" className="gap-1.5 h-9">
                <Eye className="w-4 h-4" /> View Job
              </Button>
            </Link>

            <Popover open={popoverOpen} onOpenChange={setPopoverOpen}>
              <PopoverTrigger className="inline-flex items-center justify-center h-9 w-9 text-slate-500 hover:text-foreground shrink-0 border border-transparent hover:border-border hover:bg-muted/50 rounded-lg transition-colors focus-visible:outline-hidden focus-visible:ring-1 focus-visible:ring-ring">
                <MoreHorizontal className="h-4 w-4" />
              </PopoverTrigger>
              <PopoverContent align="end" className="w-48 p-1">
                <div className="flex flex-col">
                  <Link href={`/candidate/cv-matching/new?prefillJobId=${job.jobId}`} className="w-full">
                    <Button variant="ghost" className="w-full justify-start gap-2 h-9 text-indigo-600 hover:text-indigo-700 hover:bg-indigo-50">
                      <Sparkles className="h-4 w-4" />
                      <span>Match CV</span>
                    </Button>
                  </Link>
                  <Link href={`/candidate/interview?prefillJobId=${job.jobId}&openModal=true`} className="w-full">
                    <Button variant="ghost" className="w-full justify-start gap-2 h-9 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50">
                      <MessageSquare className="h-4 w-4" />
                      <span>Mock Interview</span>
                    </Button>
                  </Link>
                  <div className="h-px bg-border my-1" />
                  <Button 
                    variant="ghost" 
                    className="w-full justify-start gap-2 h-9 text-rose-600 hover:text-rose-700 hover:bg-rose-50"
                    onClick={() => {
                      setPopoverOpen(false);
                      setShowDialog(true);
                    }}
                  >
                    <Heart className="h-4 w-4" />
                    <span>Unsave Job</span>
                  </Button>
                </div>
              </PopoverContent>
            </Popover>
          </div>
        </div>
      </CardContent>

      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Unsave Job?</DialogTitle>
            <DialogDescription>
              Are you sure you want to remove "{job.title}" from your saved jobs? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)} disabled={isUnsaving}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleConfirmUnsave} disabled={isUnsaving}>
              {isUnsaving ? 'Unsaving...' : 'Unsave'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
