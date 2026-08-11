'use client';

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Brain, FileCode, Users, RefreshCcw, AlertCircle, FileText, Download, Eye, FileSpreadsheet, Lock, Sparkles, Coins } from 'lucide-react';
import { recruiterService } from '@/services/recruiter.service';
import type { MatchHistoryDto } from '@/types/cv.types';
import { cn } from '@/lib/utils';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from 'sonner';
import { exportMatchingResultsToExcel } from '@/utils/excel-export.util';
import { getMatchMethodLabel, getScorePercent } from '@/lib/matching-score';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

interface MatchCvsSectionProps {
  jobId: string;
  jobStatus?: string;
  jobParseStatus?: string;
}

export function MatchCvsSection({ jobId, jobStatus, jobParseStatus }: MatchCvsSectionProps) {
  const [isScanning, setIsScanning] = useState(false);
  const [matches, setMatches] = useState<MatchHistoryDto[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Unlock Modal state
  const [unlockTarget, setUnlockTarget] = useState<MatchHistoryDto | null>(null);
  const [isUnlocking, setIsUnlocking] = useState(false);

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
      await recruiterService.matchJobWithCvsHardcode(jobId);

      await fetchMatches();
    } catch (err: any) {
      const serverMsg = err?.response?.data?.message || err?.response?.data?.title || err?.message || 'Failed to scan for matches.';
      setError(serverMsg);
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
  };

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

  const handleConfirmUnlock = async () => {
    if (!unlockTarget || !unlockTarget.cvId) return;
    try {
      setIsUnlocking(true);
      const res = await recruiterService.unlockCandidateCv(unlockTarget.cvId, jobId);
      if (res.success && res.data && res.data.success) {
        toast.success(res.data.message || 'Mở khóa hồ sơ thành công!');
        setUnlockTarget(null);
        await fetchMatches();
      } else {
        toast.error(res.data?.message || res.message || 'Không thể mở khóa. Vui lòng nạp thêm Coin.');
      }
    } catch (err: any) {
      toast.error(err.message || 'Lỗi hệ thống khi mở khóa.');
    } finally {
      setIsUnlocking(false);
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
            <Button
              onClick={handleScan}
              disabled={isScanDisabled}
              size="sm"
              className="gap-2 bg-blue-600 hover:bg-blue-700"
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
              {matches.map((match, index) => {
                const isUnlocked = match.isUnlocked !== false;
                return (
                  <div key={`${match.cvId || index}-${match.matchType || 'unknown'}`} className="flex flex-col sm:flex-row sm:items-center justify-between p-6 hover:bg-slate-50 transition-colors">
                    <div className="flex items-start gap-4">
                      <div className={cn(
                        "flex h-12 w-12 shrink-0 items-center justify-center rounded-xl",
                        isUnlocked ? "bg-blue-100 text-blue-600" : "bg-amber-100 text-amber-600 border border-amber-200"
                      )}>
                        {isUnlocked ? <FileText className="h-6 w-6" /> : <Lock className="h-6 w-6 text-amber-600" />}
                      </div>
                      <div className="flex flex-col">
                        <div className="flex items-center gap-2">
                          <h4 className={cn("text-base font-bold", isUnlocked ? "text-slate-900" : "text-slate-500 italic")}>
                            {match.cvFileName || "Ứng viên chưa mở khóa"}
                          </h4>
                          {!isUnlocked && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-extrabold bg-amber-100 text-amber-800 border border-amber-200">
                              <Lock className="w-2.5 h-2.5 mr-1" /> Khóa
                            </span>
                          )}
                        </div>
                        <div className="flex items-center gap-3 mt-1.5 text-xs text-slate-500">
                          <span className={cn(
                            "inline-flex items-center rounded-md px-2 py-0.5 font-semibold text-[10px] uppercase tracking-wider",
                            match.matchMethod === 'hardcode' ? "bg-blue-100 text-blue-700" : "bg-purple-100 text-purple-700"
                          )}>
                            {match.matchMethod === 'hardcode' ? (
                              <><FileCode className="w-3 h-3 mr-1" /> {getMatchMethodLabel(match.matchMethod)}</>
                            ) : (
                              <><Brain className="w-3 h-3 mr-1" /> {getMatchMethodLabel(match.matchMethod)}</>
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
                          (getScorePercent(match) ?? -1) >= 70 ? "text-emerald-600" :
                            (getScorePercent(match) ?? -1) >= 50 ? "text-amber-600" : "text-slate-600"
                        )}>
                          {getScorePercent(match) === null ? "—" : `${Math.round(getScorePercent(match)!)}%`}
                        </span>
                        <span className="text-[10px] text-slate-400 font-bold uppercase tracking-widest">Match Score</span>
                      </div>

                      <div className="flex gap-2">
                        {isUnlocked ? (
                          <>
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
                          </>
                        ) : (
                          <Button
                            onClick={() => setUnlockTarget(match)}
                            variant="default"
                            size="sm"
                            className="gap-2 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold shadow-md shadow-amber-500/20"
                          >
                            <Lock className="h-4 w-4" />
                            Mở khóa CV ({match.unlockCost || 50} Coin)
                          </Button>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </CardContent>

      <Dialog open={!!unlockTarget} onOpenChange={(open) => !open && setUnlockTarget(null)}>
        <DialogContent className="sm:max-w-md bg-white">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-xl font-bold text-slate-900">
              <Sparkles className="h-5 w-5 text-amber-500" />
              Mở khóa Hồ sơ Ứng viên
            </DialogTitle>
            <DialogDescription className="text-sm text-slate-600 pt-2">
              Mở khóa để xem đầy đủ Họ tên, Thông tin liên hệ và Tải file CV gốc của ứng viên này.
            </DialogDescription>
          </DialogHeader>

          <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 my-2 space-y-2">
            <div className="flex justify-between items-center text-sm">
              <span className="text-slate-600 font-medium">Chi phí mở khóa:</span>
              <span className="font-bold text-amber-700 flex items-center gap-1">
                <Coins className="h-4 w-4" /> {unlockTarget?.unlockCost || 50} Coin (hoặc 1 Lượt Subscription)
              </span>
            </div>
          </div>

          <DialogFooter className="flex flex-col sm:flex-row gap-2 mt-4">
            <Button variant="outline" onClick={() => setUnlockTarget(null)} disabled={isUnlocking}>
              Hủy bỏ
            </Button>
            <Button
              onClick={handleConfirmUnlock}
              disabled={isUnlocking}
              className="bg-amber-600 hover:bg-amber-700 text-white font-bold gap-2"
            >
              {isUnlocking ? (
                <>
                  <RefreshCcw className="h-4 w-4 animate-spin" />
                  Đang mở khóa...
                </>
              ) : (
                <>
                  <Lock className="h-4 w-4" />
                  Xác nhận Mở khóa
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
