'use client';

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Brain, FileCode, Users, RefreshCcw, AlertCircle, FileText, Download, Eye, FileSpreadsheet } from 'lucide-react';
import { recruiterService } from '@/services/recruiter.service';
import type { MatchHistoryDto } from '@/types/cv.types';
import { cn } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import Link from 'next/link';
import { toast } from 'sonner';
import { exportMatchingResultsToExcel } from '@/utils/excel-export.util';

interface MatchCvsSectionProps {
  jobId: string;
  jobStatus?: string;
  jobParseStatus?: string;
}

export function MatchCvsSection({ jobId, jobStatus, jobParseStatus }: MatchCvsSectionProps) {
  const [useAI, setUseAI] = useState(false);
  const [isScanning, setIsScanning] = useState(false);
  const [matches, setMatches] = useState<MatchHistoryDto[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchMatches = async () => {
    if (!jobId) return;
    try {
      setIsLoadingHistory(true);
      const res = await recruiterService.getJobMatches(jobId, 1, 20);
      if (res.success && res.data && res.data.data) {
        setMatches(res.data.data.items || []);
      }
    } catch (err: any) {
      console.error(err);
    } finally {
      setIsLoadingHistory(false);
    }
  };

  useEffect(() => {
    fetchMatches();
  }, [jobId]);

  const handleScan = async () => {
    try {
      setIsScanning(true);
      setError(null);
      if (useAI) {
        await recruiterService.matchJobWithCvs(jobId);
      } else {
        await recruiterService.matchJobWithCvsHardcode(jobId);
      }
      
      
      await fetchMatches();
    } catch (err: any) {
      setError(err.message || 'Failed to scan for matches.');
    } finally {
      setIsScanning(false);
    }
  };

  const isParsePending = jobParseStatus && jobParseStatus !== 'SUCCESS' && jobParseStatus !== 'FAILED';
  const isParseFailed = jobParseStatus === 'FAILED';
  const isScanDisabled = isScanning || jobStatus?.toUpperCase() !== 'PUBLISHED' || jobParseStatus !== 'SUCCESS';
  
  const getButtonTitle = () => {
    if (jobStatus?.toUpperCase() !== 'PUBLISHED') return "Job must be published to scan CVs";
    if (isParsePending) return "Hệ thống đang phân tích yêu cầu công việc để tìm kiếm ứng viên chuẩn xác nhất. Vui lòng thử lại sau vài giây...";
    if (isParseFailed) return "Lỗi phân tích dữ liệu, không thể matching.";
    return "Scan CVs";
  const handleExportExcel = () => {
    try {
      if (matches.length === 0) {
        toast.error('Không có dữ liệu ứng viên để xuất Excel. Hãy nhấn "Scan DB" trước.');
        return;
      }
      exportMatchingResultsToExcel(matches[0]?.jdTitle || 'Job', matches);
      toast.success('Đã xuất file Excel thành công!');
    } catch (err: any) {
      toast.error(err.message || 'Lỗi khi xuất file Excel.');
    }
  };

  return (
    <Card className="border-zinc-200/80 dark:border-zinc-800/80 shadow-xs mt-6 border-t-4 border-t-purple-600">
      <CardHeader className="pb-4 border-b border-zinc-100">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <CardTitle className="text-xl font-bold flex items-center gap-2">
            <Users className="h-5 w-5 text-purple-600" />
            Suggested Candidates
          </CardTitle>
          
          <div className="flex items-center gap-3 bg-slate-50 px-4 py-2 rounded-lg border border-slate-200">
            <div className="flex items-center gap-2">
              <Label className="text-xs font-semibold text-slate-700">Method:</Label>
              <span className={cn("text-xs font-medium", !useAI ? "text-slate-900" : "text-slate-400")}>Hardcode</span>
              <Switch disabled checked={useAI} onCheckedChange={setUseAI} className={useAI ? "data-[state=checked]:bg-purple-600" : ""} />
              <span className={cn("text-xs font-medium", useAI ? "text-purple-700" : "text-slate-400")}>AI Vector</span>
            </div>
            
            <div className="h-6 w-px bg-slate-300 mx-1"></div>

            <Button 
              onClick={handleScan} 
              disabled={isScanDisabled}
              size="sm"
              className={cn(
                "gap-2",
                useAI ? "bg-purple-600 hover:bg-purple-700" : "bg-blue-600 hover:bg-blue-700"
              )}
              title={getButtonTitle()}
            >
              <RefreshCcw className={cn("h-4 w-4", isScanning && "animate-spin")} />
              {isScanning ? 'Scanning...' : (isParsePending ? 'Preparing Data...' : 'Scan DB')}
            </Button>

            <Button
              onClick={handleExportExcel}
              disabled={matches.length === 0 || isLoadingHistory}
              variant="outline"
              size="sm"
              className="gap-2 border-emerald-300 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 hover:text-emerald-800 font-semibold"
              title="Xuất danh sách ứng viên phù hợp ra file Excel để gửi Manager"
            >
              <FileSpreadsheet className="h-4 w-4 text-emerald-600" />
              Xuất Excel
            </Button>
          </div>
        </div>
      </CardHeader>
      
      <CardContent className="p-0">
        {error && (
          <div className="flex items-center gap-2 p-4 m-4 text-sm text-red-600 bg-red-50 rounded-lg border border-red-100">
            <AlertCircle className="h-4 w-4" />
            {error}
          </div>
        )}

        <div className="flex flex-col min-h-[300px]">
          {isLoadingHistory ? (
            <div className="flex items-center justify-center flex-1 py-12 text-sm text-slate-500">
              <RefreshCcw className="h-6 w-6 animate-spin text-slate-300 mr-2" />
              Loading suggested candidates...
            </div>
          ) : matches.length === 0 ? (
            <div className="flex flex-col items-center justify-center flex-1 py-16 text-center px-4">
              <div className="bg-slate-100 p-4 rounded-full mb-4">
                <Users className="h-8 w-8 text-slate-400" />
              </div>
              <p className="text-base font-semibold text-slate-900">No candidates matched yet</p>
              <p className="text-sm text-slate-500 mt-1 max-w-sm mx-auto">
                Click "Scan DB" to run our matching engine and find the best CVs for this position.
              </p>
            </div>
          ) : (
            <div className="divide-y divide-slate-100">
              {matches.map((match, index) => (
                <div key={`${match.cvId || index}-${match.matchType || 'unknown'}`} className="flex flex-col sm:flex-row sm:items-center justify-between p-6 hover:bg-slate-50 transition-colors">
                  <div className="flex items-start gap-4">
                    <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-blue-100 text-blue-600">
                      <FileText className="h-6 w-6" />
                    </div>
                    <div className="flex flex-col">
                      <h4 className="text-base font-bold text-slate-900">{match.cvFileName || "Anonymous CV"}</h4>
                      <div className="flex items-center gap-3 mt-1.5 text-xs text-slate-500">
                        <span className={cn(
                          "inline-flex items-center rounded-md px-2 py-0.5 font-semibold text-[10px] uppercase tracking-wider",
                          match.matchType === 'Hardcode' ? "bg-blue-100 text-blue-700" : "bg-purple-100 text-purple-700"
                        )}>
                          {match.matchType === 'Hardcode' ? (
                            <><FileCode className="w-3 h-3 mr-1" /> Hardcode</>
                          ) : (
                            <><Brain className="w-3 h-3 mr-1" /> AI Vector</>
                          )}
                        </span>
                        <span>Matched on {new Date(match.updatedAt).toLocaleDateString()}</span>
                      </div>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-6 mt-4 sm:mt-0 pl-16 sm:pl-0">
                    <div className="flex flex-col items-end">
                      <span className={cn(
                        "text-2xl font-black",
                        (match.matchScore || 0) >= 0.7 ? "text-emerald-600" : 
                        (match.matchScore || 0) >= 0.5 ? "text-amber-600" : "text-slate-600"
                      )}>
                        {Math.round((match.matchScore || 0) * 100)}%
                      </span>
                      <span className="text-[10px] text-slate-400 font-bold uppercase tracking-widest">Match Score</span>
                    </div>
                    
                    <div className="flex gap-2">
                      {match.candidateId && (
                        <a href={`/recruiter/candidates/${match.candidateId}`} target="_blank" rel="noreferrer">
                          <Button variant="default" size="sm" className="gap-2 bg-slate-900 hover:bg-slate-800 text-white">
                            <Eye className="h-4 w-4" />
                            View Profile
                          </Button>
                        </a>
                      )}
                      <a href={match.fileUrl || '#'} target="_blank" rel="noreferrer">
                        <Button variant="outline" size="sm" className="gap-2">
                          <Download className="h-4 w-4" />
                          View CV
                        </Button>
                      </a>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
