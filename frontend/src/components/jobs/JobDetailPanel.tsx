import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useJobDetail } from '@/hooks/useJobDetail';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { MapPin, DollarSign, Calendar, Briefcase, Bookmark, ExternalLink, Award, Monitor, Target, Layers } from 'lucide-react';
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

        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 bg-slate-50 p-5 rounded-2xl border border-slate-100 mb-10">
          <div className="flex items-start gap-2.5">
            <MapPin className="h-5 w-5 text-emerald-500 mt-0.5 shrink-0" />
            <div>
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">Location</span>
              <span className="text-sm font-semibold text-slate-700">{job.location}</span>
            </div>
          </div>

          <div className="flex items-start gap-2.5">
            <DollarSign className="h-5 w-5 text-amber-500 mt-0.5 shrink-0" />
            <div>
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">Salary Range</span>
              <span className="text-sm font-semibold text-slate-700">
                {job.minSalary || job.maxSalary
                  ? `${job.minSalary?.toLocaleString() || "0"} - ${job.maxSalary?.toLocaleString() || "∞"} ${job.currency}`
                  : "Negotiable"}
              </span>
            </div>
          </div>

          <div className="flex items-start gap-2.5">
            <Calendar className="h-5 w-5 text-purple-500 mt-0.5 shrink-0" />
            <div>
              <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">Published Date</span>
              <span className="text-sm font-semibold text-slate-700">{job.publishedAt ? new Date(job.publishedAt).toLocaleDateString() : 'N/A'}</span>
            </div>
          </div>

          {job.level && (
            <div className="flex items-start gap-2.5">
              <Award className="h-5 w-5 text-indigo-500 mt-0.5 shrink-0" />
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">Level</span>
                <span className="text-sm font-semibold text-slate-700">{job.level}</span>
              </div>
            </div>
          )}

          {job.workingModel && (
            <div className="flex items-start gap-2.5">
              <Monitor className="h-5 w-5 text-cyan-500 mt-0.5 shrink-0" />
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">Working Model</span>
                <span className="text-sm font-semibold text-slate-700">{job.workingModel}</span>
              </div>
            </div>
          )}

          {job.jobExpertise && (
            <div className="flex items-start gap-2.5">
              <Target className="h-5 w-5 text-rose-500 mt-0.5 shrink-0" />
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">Expertise</span>
                <span className="text-sm font-semibold text-slate-700">{job.jobExpertise}</span>
              </div>
            </div>
          )}

          {job.jobDomain && job.jobDomain.length > 0 && (
            <div className="flex items-start gap-2.5 md:col-span-3">
              <Layers className="h-5 w-5 text-fuchsia-500 mt-0.5 shrink-0" />
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block tracking-wider">Job Domains</span>
                <div className="flex flex-wrap gap-1.5 mt-1">
                  {job.jobDomain.map((domain: string, idx: number) => (
                    <Badge key={idx} variant="outline" className="bg-fuchsia-50/50 text-fuchsia-700 border-fuchsia-200/60 font-normal">
                      {domain}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
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
