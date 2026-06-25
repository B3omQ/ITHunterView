import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useJobDetail } from '@/hooks/useJobDetail';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { MapPin, DollarSign, Calendar, Briefcase, Bookmark, ExternalLink, Award, Monitor, Target, Layers, Heart, Clock } from 'lucide-react';
import { useAuthStore } from '@/store/auth.store';
import { ApplyJobModal } from '@/components/jobs/ApplyJobModal';
import { useJobActions } from '@/hooks/useJobActions';

interface JobDetailPanelProps {
  jobId: string;
  isCandidateMode?: boolean;
}

export function JobDetailPanel({ jobId, isCandidateMode = false }: JobDetailPanelProps) {
  const router = useRouter();
  const { data, isLoading, isError } = useJobDetail(jobId, isCandidateMode);
  const [isApplyModalOpen, setIsApplyModalOpen] = useState(false);
  const { saveJob, unsaveJob, isSaving, isUnsaving } = useJobActions();

  if (isLoading) return <div className="h-full flex items-center justify-center"><PageLoader /></div>;
  if (isError || !data?.data) return <div className="h-full flex items-center justify-center p-8"><EmptyState title="Job not found" description="This job posting may have expired or been removed." /></div>;

  const job = data.data;

  const handleApplyClick = () => {
    const { accessToken, user } = useAuthStore.getState();
    const isAuthenticated = !!accessToken;

    if (!isAuthenticated) {
      router.push(`/login?redirect=/jobs`);
      return;
    }

    if (user?.role?.name?.toLowerCase() !== 'candidate') {
      alert('Only candidates can apply for jobs.');
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
      await unsaveJob(job.id);
    } else {
      await saveJob(job.id);
    }
  };

  return (
    <div className="flex flex-col h-full bg-white relative">
      <div className="flex-1 overflow-y-auto p-6 lg:p-8">
        {/* Header Section (Logo + Title + Salary) */}
        <div className="flex gap-4 md:gap-6 mb-6">
          <div className="w-20 h-20 md:w-24 md:h-24 rounded-lg overflow-hidden bg-white border border-slate-200 p-2 flex items-center justify-center shrink-0">
            {job.logoUrl ? (
              <img src={job.logoUrl} alt={job.companyName} className="object-contain w-full h-full" />
            ) : (
              <Briefcase className="w-10 h-10 text-slate-300" />
            )}
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
                {job.minSalary || job.maxSalary
                  ? `${job.minSalary?.toLocaleString() || "0"} - ${job.maxSalary?.toLocaleString() || "∞"} ${job.currency}`
                  : "Sign in to view salary"}
              </span>
            </div>
          </div>
        </div>

        {/* Action Section */}
        <div className="flex items-center gap-3 mb-6">
          <Button onClick={handleApplyClick} className="flex-1 text-base font-bold h-12" size="lg">
            Apply now
          </Button>
          <Button variant="outline" onClick={handleSaveClick} disabled={isSaving || isUnsaving} className="w-12 h-12 shrink-0 p-0 border-slate-200" title={job.isSaved ? "Unsave Job" : "Save Job"}>
            {job.isSaved ? (
              <Heart className="w-6 h-6 text-primary fill-primary" />
            ) : (
              <Heart className="w-6 h-6 text-slate-400 hover:text-primary transition-colors" />
            )}
          </Button>
        </div>

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
              <span className="font-bold text-slate-900 w-28 shrink-0 py-1">Skills:</span>
              <div className="flex flex-wrap gap-2">
                {job.skills.map((skill, idx) => (
                  <Badge key={idx} variant="outline" className="font-normal border-slate-200 text-slate-700 bg-white hover:bg-slate-50 px-3 py-1">
                    {skill}
                  </Badge>
                ))}
              </div>
            </div>
          )}

          {job.jobExpertise && (
            <div className="flex flex-col sm:flex-row sm:items-start gap-2 sm:gap-4">
              <span className="font-bold text-slate-900 w-28 shrink-0 py-1">Job Expertise:</span>
              <div className="flex flex-wrap gap-2">
                <Badge variant="outline" className="font-normal border-slate-200 text-slate-700 bg-white hover:bg-slate-50 px-3 py-1">
                  {job.jobExpertise}
                </Badge>
              </div>
            </div>
          )}

          {job.jobDomain && job.jobDomain.length > 0 && (
            <div className="flex flex-col sm:flex-row sm:items-start gap-2 sm:gap-4">
              <span className="font-bold text-slate-900 w-28 shrink-0 py-1">Job Domain:</span>
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
              <h2 className="text-xl font-bold text-slate-900 mb-4">Job description</h2>
              <div className="prose prose-slate max-w-none text-slate-700 whitespace-pre-wrap leading-relaxed">{job.description}</div>
            </section>
          )}

          {job.responsibilities && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">Responsibilities</h2>
              <div className="prose prose-slate max-w-none text-slate-700 whitespace-pre-wrap leading-relaxed">{job.responsibilities}</div>
            </section>
          )}

          {job.requirements && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">Requirements</h2>
              <div className="prose prose-slate max-w-none text-slate-700 whitespace-pre-wrap leading-relaxed">{job.requirements}</div>
            </section>
          )}

          {job.benefits && (
            <section>
              <h2 className="text-xl font-bold text-slate-900 mb-4">Benefits & Perks</h2>
              <div className="prose prose-slate max-w-none text-slate-700 whitespace-pre-wrap leading-relaxed">{job.benefits}</div>
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
        }}
      />
    </div>
  );
}
