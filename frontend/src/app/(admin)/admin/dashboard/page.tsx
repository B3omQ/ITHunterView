"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Users, Banknote, Coins, Activity } from "lucide-react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import { useTranslations } from "next-intl";

import { useState } from "react";
import { useAdminDashboard } from "@/hooks/useDashboard";
import { DashboardFilter } from "@/types/dashboard.types";
import { DashboardFilterBar } from "@/components/shared/DashboardFilterBar";
import { Loader2 } from "lucide-react";

const COLORS = ["#3b82f6", "#8b5cf6", "#ec4899", "#10b981", "#f59e0b"];

export default function AdminDashboard() {
  const t = useTranslations("AdminDashboard");
  const [filters, setFilters] = useState<DashboardFilter>({});
  const { data, isLoading, isError } = useAdminDashboard(filters);
  if (isLoading) {
    return (
      <div className="w-full h-[60vh] flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="w-full h-[60vh] flex flex-col items-center justify-center text-muted-foreground">
        <p>Failed to load dashboard data.</p>
      </div>
    );
  }

  const stats = [
    {
      title: "Total Revenue",
      value: `$${data.totalRevenue.toLocaleString()}`,
      change: `${data.revenueGrowthPercentage >= 0 ? '+' : ''}${data.revenueGrowthPercentage}% from last period`,
      icon: <Banknote className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Total Users",
      value: data.totalUsers.toLocaleString(),
      change: `${data.userGrowthPercentage >= 0 ? '+' : ''}${data.userGrowthPercentage}% from last period`,
      icon: <Users className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "AI Tokens Used",
      value: `${(data.aiTokensUsed / 1000).toFixed(1)}K`,
      change: `${data.tokensGrowthPercentage >= 0 ? '+' : ''}${data.tokensGrowthPercentage}% from last period`,
      icon: <Coins className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Transactions",
      value: data.transactions.toLocaleString(),
      change: `${data.transactionsGrowthPercentage >= 0 ? '+' : ''}${data.transactionsGrowthPercentage}% from last period`,
      icon: <Activity className="h-4 w-4 text-muted-foreground" />,
    },
  ];

  return (
    <div className="w-full pb-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Admin Dashboard</h1>
        <p className="text-muted-foreground">
          Platform overview, financial growth, and AI usage metrics.
        </p>
      </div>
      <DashboardFilterBar onFilterChange={setFilters} />

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
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

        <div className="grid gap-4 grid-cols-1">
          <Card>
            <CardHeader>
              <CardTitle>User & Revenue Growth</CardTitle>
            </CardHeader>
            <CardContent className="pl-2">
              <div className="h-[300px] w-full">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={data.userRevenueGrowth} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                    <XAxis dataKey="month" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                    <YAxis yAxisId="left" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} tickFormatter={(value) => `$${value}`} />
                    <YAxis yAxisId="right" orientation="right" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                    <RechartsTooltip cursor={{ fill: 'transparent' }} contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                    <Legend />
                    <Line yAxisId="left" type="monotone" name="Revenue" dataKey="revenue" stroke="#3b82f6" strokeWidth={2} activeDot={{ r: 6 }} />
                    <Line yAxisId="right" type="monotone" name="Users" dataKey="users" stroke="#8b5cf6" strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
      );
}

