'use client';

import { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Brain, FileCode, CheckCircle2, AlertCircle, RefreshCcw, Briefcase, ChevronRight, FileText } from 'lucide-react';
import { cvService } from '@/services/cv.service';
import { useGetMyCvs } from '@/hooks/useCv';
import type { Cv, MatchHistoryDto } from '@/types/cv.types';
import { cn } from '@/lib/utils';
import Link from 'next/link';

interface MatchJobsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function MatchJobsModal({ isOpen, onClose }: MatchJobsModalProps) {
  const { data: cvsResponse, isLoading: isLoadingCvs } = useGetMyCvs();
  const cvs = cvsResponse?.data || [];

  const [selectedCvId, setSelectedCvId] = useState<string>('');
  const [useAI, setUseAI] = useState(false);
  const [isScanning, setIsScanning] = useState(false);
  const [matches, setMatches] = useState<MatchHistoryDto[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen && cvs.length > 0 && !selectedCvId) {
      const primary = cvs.find((c) => c.isPrimary) || cvs[0];
      setSelectedCvId(primary.id);
    }
  }, [isOpen, cvs, selectedCvId]);

  const selectedCv = cvs.find((c) => c.id === selectedCvId);

  const fetchMatches = async () => {
    if (!selectedCvId) return;
    try {
      setIsLoadingHistory(true);
      const res = await cvService.getMatchHistory(1, 20, selectedCvId);
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
    if (isOpen && selectedCvId) {
      fetchMatches();
    }
  }, [isOpen, selectedCvId]);

  const handleScan = async () => {
    if (!selectedCvId) return;
    try {
      setIsScanning(true);
      setError(null);
      if (useAI) {
        await cvService.matchJobs(selectedCvId);
      } else {
        await cvService.matchJobsHardcode(selectedCvId);
      }

      await fetchMatches();
    } catch (err: any) {
      setError(err.message || 'Failed to scan for matches.');
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
          <div className={cn("flex flex-col gap-3 min-h-[300px] max-h-[400px] overflow-y-auto pr-1 transition-opacity duration-200", isLoadingHistory && matches.length > 0 && "opacity-50 pointer-events-none")}>
            {isLoadingHistory && matches.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full min-h-[300px] text-sm text-slate-500">
                <RefreshCcw className="h-6 w-6 animate-spin mb-3 text-slate-400" />
                <p>Loading history...</p>
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
                    <Link href={`/jobs/${match.sourceJobId || match.jobId}`} target="_blank">
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
