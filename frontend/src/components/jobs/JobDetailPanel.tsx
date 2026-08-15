import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useQueryClient } from '@tanstack/react-query';
import { useJobDetail } from '@/hooks/useJobDetail';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { MapPin, DollarSign, ExternalLink, Monitor, Heart, Clock } from 'lucide-react';
import { useAuthStore } from '@/store/auth.store';
import { ApplyJobModal } from '@/components/jobs/ApplyJobModal';
import { useJobActions } from '@/hooks/useJobActions';
import { CompanyLogo } from '@/components/shared/CompanyLogo';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { JobPostingMarkdownContent } from '@/components/jobs/JobPostingMarkdownContent';
import { WorkLocationScheduleContent } from '@/components/jobs/WorkLocationScheduleContent';
import { useTranslations } from 'next-intl';

interface JobDetailPanelProps {
  jobId: string;
  isCandidateMode?: boolean;
}

export function JobDetailPanel({ jobId, isCandidateMode = false }: JobDetailPanelProps) {
  const t = useTranslations('JobDetailPanel');
  const router = useRouter();
  const queryClient = useQueryClient();
  const { data, isLoading, isError } = useJobDetail(jobId, isCandidateMode);
  const [isApplyModalOpen, setIsApplyModalOpen] = useState(false);
  const [showUnsaveDialog, setShowUnsaveDialog] = useState(false);
  const { saveJob, unsaveJob, isSaving, isUnsaving } = useJobActions();

  if (isLoading) return <div className="h-full flex items-center justify-center"><PageLoader /></div>;
  if (isError || !data?.data) return <div className="h-full flex items-center justify-center p-8"><EmptyState title={t('jobNotFound')} description={t('jobNotFoundDesc')} /></div>;

  const job = data.data;

  const handleApplyClick = () => {
    const { accessToken, user } = useAuthStore.getState();
    const isAuthenticated = !!accessToken;

    if (!isAuthenticated) {
      router.push(`/login?redirect=/jobs`);
      return;
    }

    if (user?.role?.name?.toLowerCase() !== 'candidate') {
      alert(t('onlyCandidatesCanApply'));
      return;
    }

    setIsApplyModalOpen(true);
  };

  const handleSaveClick = async () => {
    const { accessToken } = useAuthStore.getState();
    if (!accessToken) {
      router.push(`/login?redirect=/jobs`);
      return;
    }

    if (job.isSaved) {
      setShowUnsaveDialog(true);
    } else {
      await saveJob(job.id);
    }
  };

  const handleConfirmUnsave = async () => {
    await unsaveJob(job.id);
    setShowUnsaveDialog(false);
  };

  return (
    <div className="flex flex-col h-full bg-white relative">
      <div className="flex-1 overflow-y-auto p-6 lg:p-8">
        {/* Header Section (Logo + Title + Salary) */}
        <div className="flex gap-4 md:gap-6 mb-6">
          <div className="w-20 h-20 md:w-24 md:h-24 rounded-lg overflow-hidden bg-white border border-slate-200 p-2 flex items-center justify-center shrink-0">
            <CompanyLogo src={job.logoUrl} alt={job.companyName} fallbackType="briefcase" fallbackIconClassName="w-10 h-10 text-slate-300" />
          </div>
          <div className="flex flex-col justify-center">
            <h1 className="text-xl md:text-2xl font-bold text-slate-900 flex items-center gap-2">
              {job.title}
              <ExternalLink className="w-4 h-4 text-primary shrink-0" />
            </h1>
            <p className="text-base text-slate-600 uppercase font-medium mt-1">{job.companyName}</p>
            <div className="flex items-center gap-2 mt-2 text-slate-700">
              <DollarSign className="w-5 h-5 text-slate-700" />
              <span className="font-semibold text-sm underline cursor-pointer decoration-slate-400 underline-offset-2">
                {!job.minSalary && !job.maxSalary
                  ? t('negotiable')
                  : (job.minSalary && !job.maxSalary)
                    ? t('fromSalary', { amount: job.minSalary.toLocaleString(), currency: job.currency })
                    : (!job.minSalary && job.maxSalary)
                      ? t('upToSalary', { amount: job.maxSalary.toLocaleString(), currency: job.currency })
                      : t('salaryRange', { min: (job.minSalary ?? 0).toLocaleString(), max: (job.maxSalary ?? 0).toLocaleString(), currency: job.currency })
                }
              </span>
            </div>
          </div>
        </div>

        {/* Action Section */}
        {isCandidateMode && (
          <div className="flex items-center gap-3 mb-6">
            {job.isApplied ? (
              <Button disabled variant="outline" className="flex-1 text-base font-bold h-12 bg-emerald-50 text-emerald-700 border-emerald-200">
                {t('applied')}
              </Button>
            ) : (
              <Button onClick={handleApplyClick} className="flex-1 text-base font-bold h-12" size="lg">
                {t('applyNow')}
              </Button>
            )}
            <Button variant="outline" onClick={handleSaveClick} disabled={isSaving || isUnsaving} className="w-12 h-12 shrink-0 p-0 border-slate-200" title={job.isSaved ? t('unsaveJob') : t('saveJob')}>
              {job.isSaved ? (
                <Heart className="w-6 h-6 text-primary fill-primary" />
              ) : (
                <Heart className="w-6 h-6 text-slate-400 hover:text-primary transition-colors" />
              )}
            </Button>
          </div>
        )}

        <div className="border-b border-dashed border-slate-200 my-6"></div>

        {/* Info Section 1 (Location, Working Model, Time) */}
        <div className="flex flex-col gap-3 text-slate-600 text-sm">
          <div className="flex items-start gap-2">
            <MapPin className="w-4 h-4 mt-0.5 shrink-0" />
            <span className="flex-1 leading-snug">
              {job.location} <ExternalLink className="inline-block w-3.5 h-3.5 ml-1 text-primary" />
            </span>
          </div>
          {job.workingModel && (
            <div className="flex items-center gap-2">
              <Monitor className="w-4 h-4 shrink-0" />
              <span>{job.workingModel}</span>
            </div>
          )}
          <div className="flex items-center gap-2">
            <Clock className="w-4 h-4 shrink-0" />
            <span>{job.publishedAt ? new Date(job.publishedAt).toLocaleDateString() : 'N/A'}</span>
          </div>
        </div>

        <div className="border-b border-dashed border-slate-200 my-6"></div>

        {/* Metadata Section (Skills, Expertise, Domain) */}
        <div className="flex flex-col gap-4 text-sm">
          {job.skills && job.skills.length > 0 && (
            <div className="flex flex-col sm:flex-row sm:items-start gap-2 sm:gap-4">
              <span className="font-bold text-slate-900 w-28 shrink-0 py-1">{t('skills')}</span>
              <div className="flex flex-wrap gap-2">
                {job.skills.map((skill, idx) => (
                  <Badge key={idx} variant="outline" className="font-normal border-slate-200 text-slate-700 bg-white hover:bg-slate-50 px-3 py-1">
                    {skill}
                  </Badge>
                ))}
              </div>
            </div>
          )}

          {job.jobDomain && job.jobDomain.length > 0 && (
            <div className="flex flex-col sm:flex-row sm:items-start gap-2 sm:gap-4">
              <span className="font-bold text-slate-900 w-28 shrink-0 py-1">{t('jobDomain')}</span>
              <div className="flex flex-wrap gap-2">
                {job.jobDomain.map((domain: string, idx: number) => (
                  <Badge key={idx} variant="outline" className="font-normal border-slate-200 text-slate-700 bg-white hover:bg-slate-50 px-3 py-1">
                    {domain}
                  </Badge>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="border-b border-dashed border-slate-200 my-6"></div>

        {/* Content Area */}
        <div className="flex flex-col gap-8">
          {job.description && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">{t('jobDescription')}</h2>
              <JobPostingMarkdownContent value={job.description} legacyMode="bullet" />
            </section>
          )}

          {job.incomeText && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">{t('income')}</h2>
              <JobPostingMarkdownContent value={job.incomeText} legacyMode="lines" />
            </section>
          )}

          {job.workLocationText && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">{t('workLocationSchedule')}</h2>
              <WorkLocationScheduleContent workLocationText={job.workLocationText} />
            </section>
          )}

          {job.requirements && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">{t('requirements')}</h2>
              <JobPostingMarkdownContent value={job.requirements} legacyMode="bullet" />
            </section>
          )}

          {job.benefits && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">{t('benefits')}</h2>
              <JobPostingMarkdownContent value={job.benefits} legacyMode="bullet" />
            </section>
          )}
        </div>
      </div>

      <ApplyJobModal
        isOpen={isApplyModalOpen}
        onClose={() => setIsApplyModalOpen(false)}
        jobId={job.id}
        jobTitle={job.title}
        onSuccess={() => {
          setIsApplyModalOpen(false);
          queryClient.invalidateQueries({ queryKey: ['job-detail', job.id] });
          queryClient.invalidateQueries({ queryKey: ['candidate-jobs'] });
        }}
      />

      <Dialog open={showUnsaveDialog} onOpenChange={setShowUnsaveDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('unsaveDialogTitle')}</DialogTitle>
            <DialogDescription>
              {t('unsaveDialogDesc', { title: job.title })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowUnsaveDialog(false)} disabled={isUnsaving}>
              {t('cancel')}
            </Button>
            <Button variant="destructive" onClick={handleConfirmUnsave} disabled={isUnsaving}>
              {isUnsaving ? t('unsaving') : t('unsave')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
