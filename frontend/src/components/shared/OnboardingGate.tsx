'use client';

import React, { useState } from 'react';
import { useProfileCompletionStatus, useUpdateOnboardingProfile } from '@/hooks/useCandidateProfile';
import { PageLoader } from '@/components/shared/PageLoader';
import { OnboardingWizard } from './OnboardingWizard';
import { usePathname } from 'next/navigation';

export function OnboardingGate({ children }: { children: React.ReactNode }) {
  const { data: status, isLoading } = useProfileCompletionStatus();
  const pathname = usePathname();

  // Allow access to settings or legal pages without completing onboarding
  const isExceptionRoute = pathname.startsWith('/settings') || pathname.startsWith('/legal');

  if (isLoading) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-background">
        <PageLoader message="Đang kiểm tra hồ sơ..." />
      </div>
    );
  }

  // If complete or exception route, render children normally
  if (!status || status.isComplete || isExceptionRoute) {
    return <>{children}</>;
  }

  // Otherwise, render children but overlay the non-dismissible wizard
  return (
    <>
      {children}
      <OnboardingWizard missingFields={status.missingFields} />
    </>
  );
}
