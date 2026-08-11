'use client';

import React from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useGetMyCompany } from '@/hooks/useCompany';
import { CompanyStatusBadge } from '@/components/shared/CompanyStatusBadge';
import { cn } from '@/lib/utils';
import { useTranslations } from 'next-intl';

export default function CompanyLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const { data: company, isLoading } = useGetMyCompany();
  const t = useTranslations('RecruiterCompany');

  const tabs = [
    { name: t('tabProfile'), href: '/recruiter/company/profile' },
    { name: t('tabLegal'), href: '/recruiter/company/legal' },
  ];

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">{t('layoutTitle')}</h1>
        {!isLoading && company && (
          <CompanyStatusBadge status={company.status} />
        )}
      </div>

      <div className="border-b">
        <nav className="-mb-px flex space-x-8" aria-label="Tabs">
          {tabs.map((tab) => {
            if (tab.name === t('tabLegal') && !company) return null;
            
            const isActive = pathname === tab.href;
            return (
              <Link
                key={tab.name}
                href={tab.href}
                className={cn(
                  isActive
                    ? 'border-primary text-primary'
                    : 'border-transparent text-muted-foreground hover:border-muted hover:text-foreground',
                  'whitespace-nowrap border-b-2 py-4 px-1 text-sm font-medium transition-colors'
                )}
              >
                {tab.name}
              </Link>
            );
          })}
        </nav>
      </div>

      <div className="mt-6">
        {children}
      </div>
    </div>
  );
}
