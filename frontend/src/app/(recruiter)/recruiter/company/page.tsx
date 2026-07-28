'use client';

import React from 'react';
import Link from 'next/link';
import { useGetMyCompany, useClaimCompanyNewbieReward } from '@/hooks/useCompany';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Building2, PlusCircle, CheckCircle2, Clock, XCircle, ExternalLink, Sparkles } from 'lucide-react';
import { CompanyLogo } from '@/components/shared/CompanyLogo';
export default function CompanyOverviewPage() {
  const { data: company, isLoading } = useGetMyCompany();
  const { mutate: claimReward, isPending: isClaiming } = useClaimCompanyNewbieReward();

  if (isLoading) {
    return <div className="py-12 text-center text-muted-foreground animate-pulse">Loading company overview...</div>;
  }

  // Empty state: No company registered yet
  if (!company) {
    return (
      <div className="flex flex-col items-center justify-center py-20 px-4 text-center border-2 border-dashed rounded-xl bg-muted/10">
        <div className="bg-primary/10 p-4 rounded-full mb-4">
          <Building2 className="w-12 h-12 text-primary" />
        </div>
        <h2 className="text-2xl font-bold tracking-tight mb-2">No Company Registered</h2>
        <p className="text-muted-foreground max-w-md mb-8">
          You haven't set up your company profile yet. Create a company profile to start posting jobs and connecting with top candidates.
        </p>
        <Button size="lg" className="gap-2 h-12 px-8 text-md shadow-lg hover:shadow-xl transition-all hover:-translate-y-1" onClick={() => window.location.href = '/recruiter/company/profile'}>
          <PlusCircle className="w-5 h-5" />
          Create Company Profile
        </Button>
      </div>
    );
  }

  // Company exists: Show overview
  return (
    <div className="w-full pb-8 space-y-6">
      <Card className="border-l-4 border-l-primary shadow-md">
        <CardHeader className="pb-4">
          <div className="flex justify-between items-start">
            <div className="flex items-center gap-4">
              <div className="w-16 h-16 rounded-lg bg-muted border overflow-hidden flex items-center justify-center shrink-0">
                <CompanyLogo src={company.logoUrl} alt={company.name} fallbackType="building" fallbackIconClassName="w-8 h-8 text-muted-foreground" imageClassName="w-full h-full object-cover" />
              </div>
              <div>
                <CardTitle className="text-2xl">{company.name}</CardTitle>
                <CardDescription className="text-base mt-1 flex items-center gap-2">
                  {company.industry} • {company.companyType && `${company.companyType} • `}{company.companySize} employees
                </CardDescription>
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid md:grid-cols-2 gap-8">
            <div className="space-y-4">
              <div>
                <h4 className="text-sm font-semibold text-muted-foreground mb-1">Headquarters</h4>
                <p>{company.headquartersAddress || 'Not provided'}</p>
              </div>
              <div>
                <h4 className="text-sm font-semibold text-muted-foreground mb-1">Website</h4>
                {company.website ? (
                  <a href={company.website} target="_blank" rel="noopener noreferrer" className="text-blue-600 hover:underline flex items-center gap-1">
                    {company.website} <ExternalLink className="w-3 h-3" />
                  </a>
                ) : (
                  <p className="text-muted-foreground">Not provided</p>
                )}
              </div>
            </div>

            <div className="bg-muted/30 rounded-lg p-4 border space-y-3">
              <h4 className="font-semibold border-b pb-2">Verification Status</h4>
              <div className="flex items-center gap-3">
                {company.status === 'VERIFIED' && <CheckCircle2 className="w-6 h-6 text-green-500" />}
                {company.status === 'PENDING' && <Clock className="w-6 h-6 text-yellow-500" />}
                {company.status === 'REJECTED' && <XCircle className="w-6 h-6 text-red-500" />}
                
                <div>
                  <p className="font-medium capitalize">{company.status.toLowerCase()}</p>
                  {company.status === 'PENDING' && (
                    <p className="text-xs text-muted-foreground">Awaiting admin review</p>
                  )}
                  {company.status === 'REJECTED' && (
                    <p className="text-xs text-red-500">Please review and resubmit documents</p>
                  )}
                  {company.status === 'VERIFIED' && (
                    <p className="text-xs text-green-600">Ready to post jobs</p>
                  )}
                </div>
              </div>
              
              {company.status === 'VERIFIED' && !company.isNewbieRewardClaimed ? (
                <div className="p-3.5 rounded-xl bg-gradient-to-r from-amber-500/15 via-yellow-500/10 to-amber-500/15 border border-amber-500/30 space-y-2 mt-2">
                  <div className="flex items-center gap-2 text-amber-500 font-bold text-xs uppercase tracking-wider">
                    <Sparkles className="w-4 h-4 text-amber-500 animate-pulse" />
                    <span>Thưởng 25.000 Coin</span>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Chúc mừng công ty của bạn đã được duyệt! Bấm bên dưới để nhận ngay 25.000 Coin thưởng vào ví.
                  </p>
                  <Button
                    size="sm"
                    disabled={isClaiming}
                    onClick={() => claimReward()}
                    className="w-full bg-gradient-to-r from-amber-500 to-yellow-500 hover:from-amber-600 hover:to-yellow-600 text-slate-950 font-bold shadow-md hover:shadow-lg transition-all"
                  >
                    {isClaiming ? 'ĐANG NHẬN...' : '🎁 NHẬN 25.000 COIN NGAY'}
                  </Button>
                </div>
              ) : company.status === 'VERIFIED' && company.isNewbieRewardClaimed ? (
                <div className="p-2.5 rounded-lg bg-emerald-500/10 border border-emerald-500/20 flex items-center gap-2 text-xs text-emerald-600 dark:text-emerald-400 font-medium">
                  <Sparkles className="w-4 h-4 shrink-0" />
                  <span>Đã nhận thưởng xác thực 25.000 Coin</span>
                </div>
              ) : null}

              <div className="pt-2">
                <Button variant="outline" size="sm" className="w-full" onClick={() => window.location.href = '/recruiter/company/legal'}>
                  Manage Verification
                </Button>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
