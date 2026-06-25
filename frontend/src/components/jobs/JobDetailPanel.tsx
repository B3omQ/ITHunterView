import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useJobDetail } from '@/hooks/useJobDetail';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { MapPin, DollarSign, Calendar, Briefcase, Bookmark, ExternalLink } from 'lucide-react';
import { useAuthStore } from '@/store/auth.store';
import { ApplyJobModal } from '@/components/jobs/ApplyJobModal';

interface JobDetailPanelProps {
  jobId: string;
  isCandidateMode?: boolean;
}

export function JobDetailPanel({ jobId, isCandidateMode = false }: JobDetailPanelProps) {
  const router = useRouter();
  const { data, isLoading, isError } = useJobDetail(jobId, isCandidateMode);
  const [isApplyModalOpen, setIsApplyModalOpen] = useState(false);

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

  const handleSaveClick = () => {
    const { accessToken } = useAuthStore.getState();
    if (!accessToken) {
      router.push(`/login?redirect=/jobs`);
      return;
    }
    // TODO: Connect to save job mutation
  };

  return (
    <div className="flex flex-col h-full bg-white relative">
      {/* Sticky Header Actions */}
      <div className="sticky top-0 z-10 bg-white/80 backdrop-blur-md border-b p-4 flex items-center justify-between shadow-sm">
        <div className="font-medium text-slate-800 line-clamp-1 mr-4">{job.title}</div>
        <div className="flex items-center gap-2 shrink-0">
          <Button variant="outline" size="sm" onClick={handleSaveClick}>
            <Bookmark className="w-4 h-4 mr-2" /> Save
          </Button>
          <Button size="sm" onClick={handleApplyClick}>Apply Now</Button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-6 lg:p-8">
        {/* Hero Section */}
        <div className="flex flex-col md:flex-row gap-6 mb-8">
          <div className="w-20 h-20 rounded-xl overflow-hidden bg-white border shadow-sm p-2 flex items-center justify-center shrink-0">
            {job.logoUrl ? (
              <img src={job.logoUrl} alt={job.companyName} className="object-contain w-full h-full" />
            ) : (
              <Briefcase className="w-8 h-8 text-slate-300" />
            )}
          </div>
          <div>
            <h1 className="text-2xl font-bold text-slate-900 mb-2 leading-tight">{job.title}</h1>
            <p className="text-lg text-primary font-medium">{job.companyName}</p>
          </div>
        </div>

        <div className="flex flex-wrap gap-3 mb-10 text-sm font-medium text-slate-600 bg-slate-50 p-4 rounded-xl border border-slate-100">
          <span className="flex items-center gap-1.5 px-2 py-1">
            <MapPin className="w-4 h-4 text-slate-400" /> {job.location}
          </span>
          {(job.minSalary || job.maxSalary) && (
            <span className="flex items-center gap-1.5 px-2 py-1 text-emerald-600">
              <DollarSign className="w-4 h-4" />
              {job.minSalary ? `${job.minSalary}` : 'Up to'} - {job.maxSalary ? `${job.maxSalary}` : 'Negotiable'} {job.currency}
            </span>
          )}
          <span className="flex items-center gap-1.5 px-2 py-1">
            <Briefcase className="w-4 h-4 text-slate-400" /> {job.workingModel || job.jobExpertise || 'Unknown'}
          </span>
          {job.publishedAt && (
            <span className="flex items-center gap-1.5 px-2 py-1">
              <Calendar className="w-4 h-4 text-slate-400" /> Posted {new Date(job.publishedAt).toLocaleDateString()}
            </span>
          )}
        </div>

        {/* Content Area */}
        <div className="flex flex-col gap-8">
          {job.skills && job.skills.length > 0 && (
            <section>
              <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400 mb-4">Required Skills</h3>
              <div className="flex flex-wrap gap-2">
                {job.skills.map((skill, idx) => (
                  <Badge key={idx} variant="secondary" className="px-3 py-1 text-sm font-normal bg-blue-50 text-blue-700 hover:bg-blue-100 border-none">
                    {skill}
                  </Badge>
                ))}
              </div>
            </section>
          )}

          {job.description && (
            <section>
              <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400 mb-4">Job Description</h3>
              <div className="prose prose-slate max-w-none text-slate-700 whitespace-pre-wrap leading-relaxed">{job.description}</div>
            </section>
          )}

          {job.responsibilities && (
            <section>
              <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400 mb-4">Responsibilities</h3>
              <div className="prose prose-slate max-w-none text-slate-700 whitespace-pre-wrap leading-relaxed">{job.responsibilities}</div>
            </section>
          )}

          {job.requirements && (
            <section>
              <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400 mb-4">Requirements</h3>
              <div className="prose prose-slate max-w-none text-slate-700 whitespace-pre-wrap leading-relaxed">{job.requirements}</div>
            </section>
          )}

          {job.benefits && (
            <section>
              <h3 className="text-sm font-bold uppercase tracking-wider text-slate-400 mb-4">Benefits & Perks</h3>
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
