"use client";

import Link from "next/link";
import { useAuthStore } from "@/store/auth.store";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Briefcase, Users, Eye, FileText, Sparkles, CheckCircle2, Circle, Clock, XCircle, ChevronRight } from "lucide-react";
import { useGetMyCompany, useClaimCompanyNewbieReward } from "@/hooks/useCompany";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  Legend,
} from "recharts";
import { useTranslations } from "next-intl";

import { useState } from "react";
import { useRecruiterDashboard } from "@/hooks/useDashboard";
import { DashboardFilter } from "@/types/dashboard.types";
import { DashboardFilterBar } from "@/components/shared/DashboardFilterBar";
import { Loader2 } from "lucide-react";

const COLORS = ["#3b82f6", "#10b981", "#f59e0b", "#8b5cf6", "#ec4899"];

export default function RecruiterDashboard() {
  const { user } = useAuthStore();
  const { data: company } = useGetMyCompany();
  const { mutate: claimReward, isPending: isClaiming } = useClaimCompanyNewbieReward();
  const t = useTranslations("RecruiterDashboard");

  const [filters, setFilters] = useState<DashboardFilter>({});
  const { data, isLoading, isError } = useRecruiterDashboard(filters);

  if (isLoading) {
    return (
      <div className="w-full h-[60vh] flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const stats = data ? [
    {
      title: "Active Jobs",
      value: data.activeJobs.toLocaleString(),
      change: "Currently open",
      icon: <Briefcase className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Total Applications",
      value: data.totalApplications.toLocaleString(),
      change: "Across all jobs",
      icon: <Users className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Application Status",
      value: `${data.applicationStatus.reduce((acc, curr) => acc + curr.value, 0)} Total`,
      change: "Current pipeline",
      icon: <FileText className="h-4 w-4 text-muted-foreground" />,
    },
  ] : [];

  return (
    <div className="w-full pb-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t('title')}</h1>
        <p className="text-muted-foreground">
          {t('subtitle')}
        </p>
      </div>
      <DashboardFilterBar onFilterChange={setFilters} />

      {/* Recruiter 25,000 Coin Company Verification Reward Banner */}
      {(!company || !company.isNewbieRewardClaimed) && (
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-r from-blue-700 via-indigo-700 to-purple-800 p-6 sm:p-8 text-white shadow-xl shadow-blue-500/20 border border-white/10 transition-all duration-300 hover:shadow-2xl hover:shadow-blue-500/30 mb-8">
          <div className="absolute -top-24 -right-24 w-72 h-72 bg-emerald-500/20 rounded-full blur-3xl pointer-events-none" />
          <div className="absolute -bottom-24 -left-24 w-72 h-72 bg-amber-400/20 rounded-full blur-3xl pointer-events-none" />
          
          <div className="relative z-10 flex flex-col lg:flex-row items-start lg:items-center justify-between gap-6">
            <div className="space-y-3 max-w-2xl">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/20 backdrop-blur-md text-amber-300 text-xs font-bold tracking-wide uppercase border border-white/20 shadow-sm animate-pulse">
                <Sparkles size={14} className="text-amber-300" /> {t('companyReward')}
              </div>
              <h2 className="text-2xl sm:text-3xl font-black tracking-tight text-white flex items-center gap-2">
                {t('receiveCoinTitle1')} <span className="text-transparent bg-clip-text bg-gradient-to-r from-amber-300 via-yellow-200 to-amber-400 font-extrabold">{t('receiveCoinTitle2')}</span> {t('receiveCoinTitle3')}
              </h2>
              <p className="text-blue-100 text-sm leading-relaxed">
                {t('companyRewardDesc')}
              </p>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
                <div className="flex items-center gap-3 p-3 rounded-2xl bg-white/10 backdrop-blur-sm border border-white/10 text-sm">
                  {company && (company.status === 'PENDING' || company.status === 'VERIFIED') ? (
                    <div className="p-1 rounded-full bg-emerald-500 text-white shadow-sm flex-shrink-0"><CheckCircle2 size={16} /></div>
                  ) : (
                    <div className="p-1 rounded-full bg-amber-500/20 text-amber-300 border border-amber-400/30 flex-shrink-0"><Circle size={16} /></div>
                  )}
                  <div className="flex flex-col">
                    <span className="font-semibold text-white">{t('submitLegal')}</span>
                    <span className="text-xs text-blue-200">
                      {!company ? t('notCreated') : company.status === 'VERIFIED' || company.status === 'PENDING' ? t('submitted') : t('needsUpdate')}
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-3 p-3 rounded-2xl bg-white/10 backdrop-blur-sm border border-white/10 text-sm">
                  {company?.status === 'VERIFIED' ? (
                    <div className="p-1 rounded-full bg-emerald-500 text-white shadow-sm flex-shrink-0"><CheckCircle2 size={16} /></div>
                  ) : company?.status === 'PENDING' ? (
                    <div className="p-1 rounded-full bg-yellow-400 text-slate-900 shadow-sm flex-shrink-0"><Clock size={16} /></div>
                  ) : company?.status === 'REJECTED' ? (
                    <div className="p-1 rounded-full bg-red-500 text-white shadow-sm flex-shrink-0"><XCircle size={16} /></div>
                  ) : (
                    <div className="p-1 rounded-full bg-amber-500/20 text-amber-300 border border-amber-400/30 flex-shrink-0"><Circle size={16} /></div>
                  )}
                  <div className="flex flex-col">
                    <span className="font-semibold text-white">{t('adminApproval')}</span>
                    <span className="text-xs text-blue-200">
                      {company?.status === 'VERIFIED' ? t('verified') : company?.status === 'PENDING' ? t('pending') : company?.status === 'REJECTED' ? t('rejected') : t('waitingSubmit')}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <div className="flex flex-col items-stretch sm:items-end w-full lg:w-auto mt-2 lg:mt-0 gap-3">
              {company && company.status === 'VERIFIED' && !company.isNewbieRewardClaimed ? (
                <button
                  onClick={() => claimReward()}
                  disabled={isClaiming}
                  className="relative group overflow-hidden rounded-2xl bg-gradient-to-r from-amber-400 via-amber-300 to-yellow-400 px-8 py-4 text-slate-950 font-black text-base shadow-xl shadow-amber-500/25 hover:shadow-amber-500/40 hover:scale-[1.02] active:scale-[0.98] transition-all duration-300 disabled:opacity-60 flex items-center justify-center gap-2.5"
                >
                  <span className="relative z-10 flex items-center gap-2">
                    <Sparkles className="w-5 h-5 text-blue-700 animate-bounce" />
                    {isClaiming ? t('claiming') : t('claimNow')}
                  </span>
                </button>
              ) : (
                <Link
                  href={!company ? "/recruiter/company/profile" : "/recruiter/company/legal"}
                  className="rounded-2xl bg-white/15 hover:bg-white/20 border border-white/20 px-6 py-4 text-center font-bold text-sm text-white shadow-lg backdrop-blur-md transition-all flex items-center justify-center gap-2 group"
                >
                  <span>{!company ? t('createCompany') : company?.status === 'PENDING' ? t('viewProfile') : t('completeDocuments')}</span>
                  <ChevronRight size={18} className="group-hover:translate-x-1 transition-transform" />
                </Link>
              )}
              <span className="text-xs text-blue-200/80 text-center lg:text-right">
                {t('rewardNote')}
              </span>
            </div>
          </div>
        </div>
      )}

      {data && (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {stats.map((stat, i) => (
          <Card key={i}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">
                {stat.title}
              </CardTitle>
              {stat.icon}
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stat.value}</div>
              <p className="text-xs text-muted-foreground mt-1">
                {stat.change}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>

      )}

      {data && (
        <div className="grid gap-4 grid-cols-1 md:grid-cols-2 lg:grid-cols-7">
        <Card className="col-span-1 lg:col-span-4">
          <CardHeader>
            <CardTitle>{t('dailyApplications')}</CardTitle>
          </CardHeader>
          <CardContent className="pl-2">
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={data.dailyApplications} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                  <XAxis dataKey="day" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <RechartsTooltip cursor={{ fill: 'transparent' }} contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                  <Line type="monotone" name="Applications" dataKey="apps" stroke="#3b82f6" strokeWidth={2} activeDot={{ r: 6 }} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card className="col-span-1 lg:col-span-3">
          <CardHeader>
            <CardTitle>{t('applicationStatus')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full flex items-center justify-center">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={data.applicationStatus}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={80}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {data.applicationStatus.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <RechartsTooltip contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                  <Legend verticalAlign="bottom" height={36} />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card className="col-span-1 lg:col-span-7">
          <CardHeader>
            <CardTitle>{t('topJobs')}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data.topJobs} layout="vertical" margin={{ top: 5, right: 30, left: 40, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={true} vertical={false} stroke="#e5e7eb" />
                  <XAxis type="number" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis dataKey="title" type="category" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} width={100} />
                  <RechartsTooltip cursor={{ fill: '#f3f4f6' }} contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                  <Bar dataKey="applicants" name={t('applicants')} fill="#8b5cf6" radius={[0, 4, 4, 0]} barSize={32} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>
      )}
    </div>
  );
}
