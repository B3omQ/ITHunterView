"use client";

import { useAuthStore } from "@/store/auth.store";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Briefcase, Users, Eye, FileText } from "lucide-react";
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

const dailyApplicationsData = [
  { day: "1", apps: 12 },
  { day: "5", apps: 19 },
  { day: "10", apps: 15 },
  { day: "15", apps: 28 },
  { day: "20", apps: 22 },
  { day: "25", apps: 35 },
  { day: "30", apps: 41 },
];

const topJobsData = [
  { title: "Senior React Dev", applicants: 120 },
  { title: "Backend Node.js", applicants: 85 },
  { title: "UI/UX Designer", applicants: 65 },
  { title: "DevOps Engineer", applicants: 45 },
  { title: "Product Manager", applicants: 30 },
];

const applicationStatusData = [
  { name: "Applied", value: 156 },
  { name: "Viewed", value: 89 },
];

const COLORS = ["#3b82f6", "#10b981"];

export default function RecruiterDashboard() {
  const { user } = useAuthStore();

  const stats = [
    {
      title: "Active Jobs",
      value: "12",
      change: "2 new this week",
      icon: <Briefcase className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Total Applications",
      value: "245",
      change: "+18% from last month",
      icon: <Users className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Total Views",
      value: "12.5K",
      change: "+24% from last month",
      icon: <Eye className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Application Status",
      value: "89 Viewed",
      change: "156 Applied (Pending)",
      icon: <FileText className="h-4 w-4 text-muted-foreground" />,
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Recruiter Dashboard</h1>
        <p className="text-muted-foreground">
          Analytics for your job postings and candidate pipeline.
        </p>
      </div>

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

      <div className="grid gap-4 grid-cols-1 md:grid-cols-2 lg:grid-cols-7">
        <Card className="col-span-1 lg:col-span-4">
          <CardHeader>
            <CardTitle>Daily Applications (This Month)</CardTitle>
          </CardHeader>
          <CardContent className="pl-2">
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={dailyApplicationsData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
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
            <CardTitle>Application Status</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full flex items-center justify-center">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={applicationStatusData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={80}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {applicationStatusData.map((entry, index) => (
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
            <CardTitle>Top Jobs by Applications</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={topJobsData} layout="vertical" margin={{ top: 5, right: 30, left: 40, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={true} vertical={false} stroke="#e5e7eb" />
                  <XAxis type="number" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis dataKey="title" type="category" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} width={100} />
                  <RechartsTooltip cursor={{ fill: '#f3f4f6' }} contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                  <Bar dataKey="applicants" name="Applicants" fill="#8b5cf6" radius={[0, 4, 4, 0]} barSize={32} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
