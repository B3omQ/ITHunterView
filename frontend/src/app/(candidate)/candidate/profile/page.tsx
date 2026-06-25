'use client';

import React, { useState } from 'react';
import { useProfileSummary } from '@/hooks/useCandidateProfile';
import { PageLoader } from '@/components/shared/PageLoader';
import { EmptyState } from '@/components/shared/EmptyState';
import { ProfileHeader } from './_components/ProfileHeader';
import { PersonalInfoTab } from './_components/PersonalInfoTab';
import { SkillsTab } from './_components/SkillsTab';
import { ExperienceTab } from './_components/ExperienceTab';
import { EducationTab } from './_components/EducationTab';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { User, Briefcase, GraduationCap, Award } from 'lucide-react';

export default function ProfilePage() {
  const { data: summary, isLoading, isError } = useProfileSummary();
  const [activeTab, setActiveTab] = useState('personal-info');

  if (isLoading) {
    return (
      <div className="container max-w-5xl mx-auto py-12 px-4">
        <PageLoader message="Loading profile..." />
      </div>
    );
  }

  if (isError || !summary) {
    return (
      <div className="container max-w-5xl mx-auto py-12 px-4">
        <EmptyState
          title="Could not load profile"
          description="Please check your connection and try again."
        />
      </div>
    );
  }

  return (
    <div className="container max-w-5xl mx-auto py-8 px-4 space-y-8">
      {/* Profile Header */}
      <ProfileHeader summary={summary} />

      {/* Profile Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <div className="border-b border-border/40 pb-2">
          <TabsList className="bg-muted p-1 rounded-xl flex w-full md:w-max">
            <TabsTrigger value="personal-info" className="flex items-center gap-2 flex-1 md:flex-initial py-2.5 px-4 rounded-lg text-sm font-semibold transition-all">
              <User className="w-4 h-4" />
              Personal Info
            </TabsTrigger>
            <TabsTrigger value="skills" className="flex items-center gap-2 flex-1 md:flex-initial py-2.5 px-4 rounded-lg text-sm font-semibold transition-all">
              <Award className="w-4 h-4" />
              Skills
            </TabsTrigger>
            <TabsTrigger value="experience" className="flex items-center gap-2 flex-1 md:flex-initial py-2.5 px-4 rounded-lg text-sm font-semibold transition-all">
              <Briefcase className="w-4 h-4" />
              Experience
            </TabsTrigger>
            <TabsTrigger value="education" className="flex items-center gap-2 flex-1 md:flex-initial py-2.5 px-4 rounded-lg text-sm font-semibold transition-all">
              <GraduationCap className="w-4 h-4" />
              Education
            </TabsTrigger>
          </TabsList>
        </div>

        {/* Tab Content Placeholders */}
        <TabsContent value="personal-info" className="outline-none">
          <PersonalInfoTab />
        </TabsContent>

        <TabsContent value="skills" className="outline-none">
          <SkillsTab />
        </TabsContent>

        <TabsContent value="experience" className="outline-none">
          <ExperienceTab />
        </TabsContent>

        <TabsContent value="education" className="outline-none">
          <EducationTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
