'use client';

import { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Brain, FileCode, CheckCircle2, AlertCircle, RefreshCcw, Briefcase, ChevronRight } from 'lucide-react';
import { cvService } from '@/services/cv.service';
import type { Cv, MatchHistoryDto } from '@/types/cv.types';
import { cn } from '@/lib/utils';
import Link from 'next/link';

interface MatchJobsModalProps {
  cv: Cv | null;
  isOpen: boolean;
  onClose: () => void;
}

export function MatchJobsModal({ cv, isOpen, onClose }: MatchJobsModalProps) {
  const [useAI, setUseAI] = useState(false);
  const [isScanning, setIsScanning] = useState(false);
  const [matches, setMatches] = useState<MatchHistoryDto[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchMatches = async () => {
    if (!cv) return;
    try {
      setIsLoadingHistory(true);
      const res = await cvService.getMatchHistory(1, 20, cv.id);
      if (res.success && res.data) {
        setMatches(res.data.items);
      }
    } catch (err: any) {
      console.error(err);
    } finally {
      setIsLoadingHistory(false);
    }
  };

  useEffect(() => {
    if (isOpen && cv) {
      fetchMatches();
    }
  }, [isOpen, cv]);

  const handleScan = async () => {
    if (!cv) return;
    try {
      setIsScanning(true);
      setError(null);
      if (useAI) {
        await cvService.matchJobs(cv.id);
      } else {
        await cvService.matchJobsHardcode(cv.id);
      }
      
      await fetchMatches();
    } catch (err: any) {
      setError(err.message || 'Failed to scan for matches.');
    } finally {
      setIsScanning(false);
    }
  };

  if (!cv) return null;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-[600px] bg-white border-slate-200">
        <DialogHeader>
          <DialogTitle className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <Briefcase className="h-5 w-5 text-blue-600" />
            Find Matching Jobs
          </DialogTitle>
          <p className="text-sm text-slate-500 mt-1">
            We will scan all available jobs to find the best fit for your CV: <strong className="text-slate-900">{cv.fileName}</strong>
          </p>
        </DialogHeader>

        <div className="flex flex-col gap-6 py-4">
          {/* Controls */}
          <div className="flex items-center justify-between p-4 bg-slate-50 rounded-xl border border-slate-100">
            <div className="flex flex-col gap-1">
              <Label className="text-sm font-semibold text-slate-900">Matching Engine</Label>
              <div className="flex items-center gap-1.5 text-xs text-slate-500">
                {useAI ? <Brain className="h-3.5 w-3.5 text-purple-500" /> : <FileCode className="h-3.5 w-3.5 text-blue-500" />}
                {useAI ? 'Semantic Vector AI' : 'Rule-based Keyword Extraction'}
              </div>
            </div>
            
            <div className="flex items-center gap-3">
              <span className={cn("text-xs font-medium", !useAI ? "text-slate-900" : "text-slate-400")}>Hardcode</span>
              <Switch disabled checked={useAI} onCheckedChange={setUseAI} className={useAI ? "data-[state=checked]:bg-purple-600" : ""} />
              <span className={cn("text-xs font-medium", useAI ? "text-purple-700" : "text-slate-400")}>AI Vector</span>
            </div>
          </div>

          <div className="flex items-center justify-between">
            <h4 className="text-sm font-semibold text-slate-900">Matches Found ({matches.length})</h4>
            <Button 
              onClick={handleScan} 
              disabled={isScanning}
              size="sm"
              className={cn(
                "gap-2",
                useAI ? "bg-purple-600 hover:bg-purple-700" : "bg-blue-600 hover:bg-blue-700"
              )}
            >
              <RefreshCcw className={cn("h-4 w-4", isScanning && "animate-spin")} />
              {isScanning ? 'Scanning...' : 'Scan Now'}
            </Button>
          </div>

          {error && (
            <div className="flex items-center gap-2 p-3 text-sm text-red-600 bg-red-50 rounded-lg border border-red-100">
              <AlertCircle className="h-4 w-4" />
              {error}
            </div>
          )}

          {/* Results */}
          <div className="flex flex-col gap-3 min-h-[250px] max-h-[400px] overflow-y-auto pr-1">
            {isLoadingHistory ? (
              <div className="flex items-center justify-center h-full text-sm text-slate-500">
                Loading history...
              </div>
            ) : matches.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-center p-6 border-2 border-dashed border-slate-200 rounded-xl bg-slate-50">
                <Briefcase className="h-8 w-8 text-slate-300 mb-2" />
                <p className="text-sm font-medium text-slate-900">No matches found yet.</p>
                <p className="text-xs text-slate-500 mt-1 max-w-[250px]">
                  Click "Scan Now" to compare your CV against open jobs.
                </p>
              </div>
            ) : (
              matches.map((match) => (
                <div key={match.jobId + match.matchType} className="flex items-center justify-between p-4 rounded-xl border border-slate-200 bg-white hover:border-blue-300 transition-colors shadow-sm">
                  <div className="flex flex-col gap-1 min-w-0 flex-1">
                    <h5 className="text-sm font-semibold text-slate-900 truncate" title={match.jdTitle}>
                      {match.jdTitle || "Unknown Job"}
                    </h5>
                    <div className="flex items-center gap-2 text-xs">
                      <span className={cn(
                        "inline-flex items-center rounded-full px-2 py-0.5 font-medium",
                        match.matchType === 'Hardcode' ? "bg-blue-50 text-blue-700" : "bg-purple-50 text-purple-700"
                      )}>
                        {match.matchType === 'Hardcode' ? 'Hardcode' : 'AI'}
                      </span>
                      <span className="text-slate-500">
                        {new Date(match.updatedAt).toLocaleDateString()}
                      </span>
                    </div>
                  </div>

                  <div className="flex items-center gap-4 pl-4 shrink-0">
                    <div className="flex flex-col items-end">
                      <span className={cn(
                        "text-lg font-bold",
                        (match.matchScore || 0) >= 0.7 ? "text-green-600" : 
                        (match.matchScore || 0) >= 0.5 ? "text-amber-600" : "text-slate-600"
                      )}>
                        {Math.round((match.matchScore || 0) * 100)}%
                      </span>
                      <span className="text-[10px] text-slate-500 font-medium uppercase tracking-wider">Match Score</span>
                    </div>
                    <Link href={`/candidate/jobs/${match.sourceJobId || match.jobId}`} target="_blank">
                      <Button variant="ghost" size="icon" className="h-8 w-8 text-slate-400 hover:text-blue-600">
                        <ChevronRight className="h-5 w-5" />
                      </Button>
                    </Link>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
