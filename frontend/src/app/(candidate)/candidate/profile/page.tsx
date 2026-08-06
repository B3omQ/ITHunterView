'use client';

import React from 'react';
import { useProfileSummary } from '@/hooks/useCandidateProfile';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { ProfileHeader } from './_components/ProfileHeader';
import { SkillsTab } from './_components/SkillsTab';
import { ExperienceTab } from './_components/ExperienceTab';
import { EducationTab } from './_components/EducationTab';
import { useTranslations } from 'next-intl';

export default function ProfilePage() {
  const { data: summary, isLoading, isError } = useProfileSummary();
  const t = useTranslations("CandidateProfile");

  if (isLoading) {
    return (
      <div className="w-full pb-8 space-y-8">
        <PageLoader message={t('loading')} />
      </div>
    );
  }

  if (isError || !summary) {
    return (
      <div className="w-full pb-8 space-y-8">
        <EmptyState
          title={t('errorTitle')}
          description={t('errorDesc')}
        />
      </div>
    );
  }

  return (
    <div className="w-full pb-16">
      <div className="grid grid-cols-1 lg:grid-cols-[360px_1fr] xl:grid-cols-[400px_1fr] gap-8 items-start max-w-7xl mx-auto">
        {/* Left Column */}
        <div className="space-y-6">
          <div id="header">
            <ProfileHeader summary={summary} />
          </div>
        </div>

        {/* Right Column */}
        <div className="space-y-6 min-w-0">
          <div id="skills">
            <SkillsTab />
          </div>

          <div id="experience">
            <ExperienceTab />
          </div>

          <div id="education">
            <EducationTab />
          </div>
        </div>
      </div>
    </div>
  );
}
