'use client';

import { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { AlertCircle, RefreshCcw, Briefcase, ChevronRight, FileText } from 'lucide-react';
import { cvService } from '@/services/cv.service';
import { useGetMyCvs } from '@/hooks/useCv';
import { useCandidateJobScan } from '@/hooks/useCandidateJobScan';
import type { CandidateJobScanResultDto } from '@/types/cv.types';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { useSignalR } from '@/hooks/useSignalR';
import { toast } from 'sonner';

interface MatchJobsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function MatchJobsModal({ isOpen, onClose }: MatchJobsModalProps) {
  const { data: cvsResponse, isLoading: isLoadingCvs } = useGetMyCvs();
  const cvs = cvsResponse?.data || [];

  const [selectedCvId, setSelectedCvId] = useState<string>('');
  const [isScanning, setIsScanning] = useState(false);
  const [isBackgroundScanning, setIsBackgroundScanning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: scanResponse, isLoading: isLoadingHistory, refetch } = useCandidateJobScan(
    isOpen && selectedCvId ? selectedCvId : undefined
  );
  const matches: CandidateJobScanResultDto[] = scanResponse?.data?.items || [];

  const connection = useSignalR('/hubs/notification');

  useEffect(() => {
    if (connection) {
      connection.on('ReceiveNotification', (notification: any) => {
        if (notification.type === 'CandidateJobScanComplete' && notification.cvId === selectedCvId) {
          setIsBackgroundScanning(false);
          toast.success(notification.message || 'Matching complete!');
          refetch();
        } else if (notification.type === 'CandidateJobScanError' && notification.cvId === selectedCvId) {
          setIsBackgroundScanning(false);
          toast.error(notification.message || 'An error occurred during matching.');
          setError(notification.message || 'An error occurred during matching.');
        }
      });
    }
    return () => {
      if (connection) {
        connection.off('ReceiveNotification');
      }
    };
  }, [connection, selectedCvId, refetch]);

  useEffect(() => {
    if (isOpen && cvs.length > 0 && !selectedCvId) {
      const primary = cvs.find((c) => c.isPrimary) || cvs[0];
      setSelectedCvId(primary.id);
    }
  }, [isOpen, cvs, selectedCvId]);

  const selectedCv = cvs.find((c) => c.id === selectedCvId);

  const handleScan = async () => {
    if (!selectedCvId) {
      setError('Please select a resume before scanning.');
      return;
    }
    try {
      setIsScanning(true);
      setError(null);
      const res = await cvService.matchJobsHardcode(selectedCvId);

      // If backend accepted the request for background processing (202 Accepted)
      if (res?.message?.includes('queued') || res?.message?.includes('background') || res?.message?.includes('accepted') || res?.data) {
        setIsBackgroundScanning(true);
        toast.success("Matching started in background. You will be notified when it's done.");
      } else {
        await refetch();
      }
    } catch (err: any) {
      const serverMsg = err?.response?.data?.message || err?.response?.data?.title || err?.message || 'Failed to scan for matches.';
      setError(serverMsg);
    } finally {
      setIsScanning(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-[600px] bg-white border-slate-200">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <Briefcase className="h-5 w-5 text-blue-600" />
            Smart Match Jobs
          </DialogTitle>
          <p className="text-sm text-slate-500 mt-1">
            Scan all available jobs to find the best fit for your CV.
          </p>
        </DialogHeader>

        <div className="flex flex-col gap-6 py-4">
          {/* CV Selection */}
          <div className="flex flex-col gap-2">
            <Label className="text-sm font-semibold text-slate-900">Select Resume for Matching</Label>
            {isLoadingCvs ? (
              <div className="h-10 bg-slate-100 rounded-md animate-pulse"></div>
            ) : cvs.length === 0 ? (
              <div className="text-sm text-red-500 bg-red-50 p-3 rounded-md border border-red-100">
                You haven't uploaded any resumes yet.
              </div>
            ) : (
              <Select value={selectedCvId} onValueChange={(val) => setSelectedCvId(val || '')}>
                <SelectTrigger className="w-full">
                  <span className="flex-1 text-left truncate">
                    {selectedCv ? selectedCv.fileName : "Select a resume..."}
                  </span>
                </SelectTrigger>
                <SelectContent>
                  {cvs.map(c => (
                    <SelectItem key={c.id} value={c.id}>
                      <div className="flex items-center gap-2">
                        <FileText className="w-4 h-4 text-blue-500" />
                        <span>{c.fileName}</span>
                        {c.isPrimary && <span className="text-[10px] bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded-full ml-2">Primary</span>}
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>

          <div className="flex items-center justify-between mt-4">
            <h4 className="text-sm font-semibold text-slate-900">Matches Found ({matches.length})</h4>
            <Button
              onClick={handleScan}
              disabled={isScanning || isBackgroundScanning}
              size="sm"
              className="gap-2 bg-blue-600 hover:bg-blue-700"
            >
              <RefreshCcw className={cn("h-4 w-4", (isScanning || isBackgroundScanning) && "animate-spin")} />
              {isBackgroundScanning ? 'Scanning in background...' : isScanning ? 'Starting...' : 'Scan Now'}
            </Button>
          </div>

          {error && (
            <div className="flex items-center gap-2 p-3 text-sm text-red-600 bg-red-50 rounded-lg border border-red-100">
              <AlertCircle className="h-4 w-4" />
              {error}
            </div>
          )}

          {/* Results */}
          <div className="flex flex-col gap-3 min-h-[300px] max-h-[400px] overflow-y-auto pr-1">
            {isLoadingHistory && matches.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full min-h-[300px] text-sm text-slate-500">
                <RefreshCcw className="h-6 w-6 animate-spin mb-3 text-slate-400" />
                <p>Loading matches...</p>
              </div>
            ) : matches.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full min-h-[300px] text-center p-6 border-2 border-dashed border-slate-200 rounded-xl bg-slate-50">
                <Briefcase className="h-8 w-8 text-slate-300 mb-2" />
                <p className="text-sm font-medium text-slate-900">No matches found yet.</p>
                <p className="text-xs text-slate-500 mt-1 max-w-[250px]">
                  Click "Scan Now" to compare your CV against open jobs.
                </p>
              </div>
            ) : (
              matches.map((match) => {
                const score = match.matchScore;
                return (
                  <div key={match.id} className="flex items-center justify-between p-4 rounded-xl border border-slate-200 bg-white hover:border-blue-300 transition-colors shadow-sm">
                    <div className="flex flex-col gap-1 min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-slate-100 text-slate-700 text-xs font-semibold">
                          #{match.rank}
                        </span>
                        <h5 className="text-sm font-semibold text-slate-900 truncate" title={match.jobTitle || match.jobTitleSnapshot || "Unknown Job"}>
                          {match.jobTitle || match.jobTitleSnapshot || "Unknown Job"}
                        </h5>
                      </div>
                    </div>

                    <div className="flex items-center gap-4 pl-4 shrink-0">
                      <div className="flex flex-col items-end">
                        <span className={cn(
                          "text-lg font-bold",
                          score !== null && score !== undefined && score >= 70 ? "text-green-600" :
                            score !== null && score !== undefined && score >= 50 ? "text-amber-600" : "text-slate-600"
                        )}>
                          {score === null || score === undefined ? "—" : `${Math.round(score)}%`}
                        </span>
                        <span className="text-[10px] text-slate-500 font-medium uppercase tracking-wider">Match Score</span>
                      </div>
                      <Link href={`/jobs/${match.jobId}`} target="_blank">
                        <Button variant="ghost" size="icon" className="h-8 w-8 text-slate-400 hover:text-blue-600">
                          <ChevronRight className="h-5 w-5" />
                        </Button>
                      </Link>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
