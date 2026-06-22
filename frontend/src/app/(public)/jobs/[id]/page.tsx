'use client';

import React, { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { useJobDetail } from '@/hooks/useJobDetail';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { MapPin, DollarSign, Calendar, Briefcase, ChevronLeft, Bookmark } from 'lucide-react';
import { useAuthStore } from '@/store/auth.store';
import { ApplyJobModal } from '@/components/jobs/ApplyJobModal';

export default function PublicJobDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { data, isLoading, isError } = useJobDetail(params.id as string, false);
  const [isApplyModalOpen, setIsApplyModalOpen] = useState(false);

  if (isLoading) return <PageLoader />;
  if (isError || !data?.data) return <EmptyState title="Job not found" description="This job posting may have expired or been removed." />;

  const job = data.data;

  const handleApplyClick = () => {
    // Get fresh auth state from the store directly to avoid hydration issues
    const { accessToken, user } = useAuthStore.getState();
    const isAuthenticated = !!accessToken;

    if (!isAuthenticated) {
      router.push(`/login?redirect=/jobs/${params.id}`);
      return;
    }

    // Check if user is a candidate
    if (user?.role?.name?.toLowerCase() !== 'candidate') {
      alert('Only candidates can apply for jobs.');
      return;
    }

    setIsApplyModalOpen(true);
  };

  const handleSaveClick = () => {
    const { accessToken } = useAuthStore.getState();
    if (!accessToken) {
      router.push(`/login?redirect=/jobs/${params.id}`);
      return;
    }
    // Save job logic here if any
  };

  return (
    <div className="container mx-auto py-8 max-w-4xl">
      <Button variant="ghost" onClick={() => router.back()} className="mb-6 -ml-4">
        <ChevronLeft className="w-4 h-4 mr-2" /> Back to jobs
      </Button>

      <div className="bg-white border rounded-lg shadow-sm overflow-hidden">
        {/* Hero Section */}
        <div className="p-8 border-b">
          <div className="flex flex-col md:flex-row justify-between items-start gap-6">
            <div className="flex gap-6 items-center">
              <div className="w-24 h-24 rounded-lg overflow-hidden bg-slate-50 border p-2 flex items-center justify-center shrink-0">
                {job.logoUrl ? (
                  <img src={job.logoUrl} alt={job.companyName} className="object-contain w-full h-full" />
                ) : (
                  <Briefcase className="w-10 h-10 text-slate-300" />
                )}
              </div>
              <div>
                <h1 className="text-3xl font-bold text-slate-900 mb-2">{job.title}</h1>
                <p className="text-xl text-primary font-medium">{job.companyName}</p>
              </div>
            </div>

            <div className="flex flex-col gap-3 w-full md:w-auto">
              <Button size="lg" className="w-full" onClick={handleApplyClick}>Apply Now</Button>
              <Button size="lg" variant="outline" className="w-full" onClick={handleSaveClick}>
                <Bookmark className="w-4 h-4 mr-2" /> Save Job
              </Button>
            </div>
          </div>

          <div className="flex flex-wrap gap-4 mt-8 text-sm font-medium text-slate-600">
            <span className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 rounded-md">
              <MapPin className="w-4 h-4 text-primary" /> {job.location}
            </span>
            {(job.minSalary || job.maxSalary) && (
              <span className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 rounded-md">
                <DollarSign className="w-4 h-4 text-primary" />
                {job.minSalary ? `${job.minSalary}` : 'Up to'} - {job.maxSalary ? `${job.maxSalary}` : 'Negotiable'} {job.currency}
              </span>
            )}
            <span className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 rounded-md">
              <Briefcase className="w-4 h-4 text-primary" /> {job.jobType.replace('_', ' ').toUpperCase()}
            </span>
            {job.publishedAt && (
              <span className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 rounded-md">
                <Calendar className="w-4 h-4 text-primary" /> Posted {new Date(job.publishedAt).toLocaleDateString()}
              </span>
            )}
          </div>
        </div>

        {/* Content Area */}
        <div className="p-8 flex flex-col gap-8">
          {job.skills && job.skills.length > 0 && (
            <section>
              <h3 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3">Required Skills</h3>
              <div className="flex flex-wrap gap-2">
                {job.skills.map((skill, idx) => (
                  <Badge key={idx} variant="secondary" className="px-3 py-1 text-sm font-normal">{skill}</Badge>
                ))}
              </div>
            </section>
          )}

          <section>
            <h3 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3">Job Description</h3>
            <div className="prose max-w-none text-slate-700 whitespace-pre-wrap">{job.description}</div>
          </section>

          <section>
            <h3 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3">Responsibilities</h3>
            <div className="prose max-w-none text-slate-700 whitespace-pre-wrap">{job.responsibilities}</div>
          </section>

          <section>
            <h3 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3">Requirements</h3>
            <div className="prose max-w-none text-slate-700 whitespace-pre-wrap">{job.requirements}</div>
          </section>

          <section>
            <h3 className="text-lg font-semibold mb-3 border-l-4 border-primary pl-3">Benefits</h3>
            <div className="prose max-w-none text-slate-700 whitespace-pre-wrap">{job.benefits}</div>
          </section>
        </div>
      </div>

      <ApplyJobModal
        isOpen={isApplyModalOpen}
        onClose={() => setIsApplyModalOpen(false)}
        jobId={job.id}
        jobTitle={job.title}
        onSuccess={() => {
          // You could optionally refetch the job detail or change button state here
        }}
      />
    </div>
  );
}
