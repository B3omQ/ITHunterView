'use client';

import React, { useState, useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useGetMyCompany } from '@/hooks/useCompany';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Building2, AlertTriangle, ArrowRight } from 'lucide-react';

export function CompanyReminderModal() {
  const router = useRouter();
  const pathname = usePathname();
  const { data: company, isLoading } = useGetMyCompany();
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    // Only run on client and when fetch is completed
    if (isLoading) return;

    // Check if the user has no company registered
    const hasNoCompany = !company;

    // Do not show the popup if the recruiter is already on the company setup pages
    const isAlreadyOnCompanyPage = pathname.startsWith('/recruiter/company');

    if (hasNoCompany && !isAlreadyOnCompanyPage) {
      // Check if they already dismissed the prompt in the current session
      const isDismissed = sessionStorage.getItem('dismissedCompanyReminder');
      if (!isDismissed) {
        setIsOpen(true);
      }
    } else {
      setIsOpen(false);
    }
  }, [company, isLoading, pathname]);

  const handleDismiss = () => {
    sessionStorage.setItem('dismissedCompanyReminder', 'true');
    setIsOpen(false);
  };

  const handleNavigateToSetup = () => {
    sessionStorage.setItem('dismissedCompanyReminder', 'true');
    setIsOpen(false);
    router.push('/recruiter/company/profile');
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => {
      if (!open) {
        handleDismiss();
      }
    }}>
      <DialogContent className="sm:max-w-md border-zinc-200/80 dark:border-zinc-800/80 shadow-xl rounded-2xl overflow-hidden p-6 gap-6">
        <DialogHeader className="space-y-4">
          <div className="mx-auto w-14 h-14 bg-amber-50 dark:bg-amber-950/30 text-amber-500 rounded-full flex items-center justify-center border border-amber-200 dark:border-amber-900/50">
            <Building2 className="w-7 h-7" />
          </div>
          <div className="space-y-1.5 text-center">
            <DialogTitle className="text-xl font-bold tracking-tight text-zinc-900 dark:text-zinc-50">
              Register Your Company Profile
            </DialogTitle>
            <DialogDescription className="text-zinc-500 dark:text-zinc-400 text-sm leading-relaxed">
              To start posting IT job openings and connecting with qualified candidates, you need to set up your company profile first.
            </DialogDescription>
          </div>
        </DialogHeader>

        <div className="bg-amber-500/5 dark:bg-amber-500/5 border border-amber-500/10 rounded-xl p-4 flex gap-3 items-start">
          <AlertTriangle className="w-5 h-5 text-amber-500 shrink-0 mt-0.5" />
          <p className="text-xs text-amber-600 dark:text-amber-400 font-medium leading-relaxed">
            New job postings will require verification documents. Registering your company details now helps speed up the approval process!
          </p>
        </div>

        <DialogFooter className="flex flex-col-reverse sm:flex-row gap-3 pt-2">
          <Button 
            variant="outline" 
            onClick={handleDismiss} 
            className="w-full sm:w-auto border-zinc-200 dark:border-zinc-800 text-zinc-600 dark:text-zinc-400"
          >
            Maybe Later
          </Button>
          <Button 
            onClick={handleNavigateToSetup} 
            className="w-full sm:w-auto bg-blue-600 hover:bg-blue-700 text-white gap-2 font-medium"
          >
            Create Company Profile
            <ArrowRight className="w-4 h-4" />
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
