'use client';

import React from 'react';
import Link from 'next/link';
import { useGetMyCompany } from '@/hooks/useCompany';
import { Button } from '@/components/ui/button';

export default function CreateJobPage() {
  const { data: company, isLoading } = useGetMyCompany();

  if (isLoading) {
    return <div className="p-8 text-center text-muted-foreground">Checking company status...</div>;
  }

  if (!company || company.status !== 'VERIFIED') {
    return (
      <div className="max-w-2xl mx-auto mt-12 p-8 border rounded-xl bg-card text-center space-y-4">
        <div className="mx-auto w-16 h-16 bg-amber-100 text-amber-600 rounded-full flex items-center justify-center mb-4">
          <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <h2 className="text-2xl font-bold">Verification Required</h2>
        <p className="text-muted-foreground">
          Your company needs to be verified before you can post new jobs. 
          Please complete your Legal Verification and wait for admin approval.
        </p>
        <div className="pt-4 flex justify-center gap-4">
          <Button variant="outline" onClick={() => window.location.href = '/recruiter/dashboard'}>
            Return to Dashboard
          </Button>
          <Button onClick={() => window.location.href = '/recruiter/company/legal'}>
            Complete Verification
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto p-6">
      <h1 className="text-2xl font-bold mb-6">Post a New Job</h1>
      <p>Job posting form goes here...</p>
      {/* TODO: Implement Job Form */}
    </div>
  );
}
