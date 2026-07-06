import React from 'react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { MapPin, DollarSign, Trash2 } from 'lucide-react';
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
    <Link href={`/candidate/jobs/${job.jobId}`}>
      <Card className="hover:border-primary/50 transition-colors group">
        <CardContent className="p-4 flex items-center justify-between gap-4">
          <div className="flex items-center gap-4 flex-1">
              <div className="w-12 h-12 rounded overflow-hidden bg-slate-100 flex items-center justify-center shrink-0 border">
                <CompanyLogo src={job.logoUrl} alt={job.companyName} fallbackType="briefcase" fallbackIconClassName="text-slate-400 w-5 h-5" />
              </div>
              <div>
                <h3 className="font-semibold text-primary group-hover:underline line-clamp-1">
                  {job.title}
                </h3>
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
            variant="destructive" 
            size="icon" 
            onClick={handleUnsave}
            disabled={isUnsaving}
            title="Remove from Saved Jobs"
          >
            <Trash2 className="w-4 h-4" />
          </Button>
        </CardContent>
      </Card>
    </Link>
  );
}
