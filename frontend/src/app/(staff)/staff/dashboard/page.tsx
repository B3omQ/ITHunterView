"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { FileQuestion, HelpCircle, Building2, AlertTriangle } from "lucide-react";
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

const categoryData = [
  { name: "Frontend", value: 340 },
  { name: "Backend", value: 410 },
  { name: "DevOps", value: 180 },
  { name: "Data", value: 120 },
  { name: "Mobile", value: 90 },
];

const levelData = [
  { level: "Intern", count: 80 },
  { level: "Fresher", count: 250 },
  { level: "Junior", count: 420 },
  { level: "Middle", count: 280 },
  { level: "Senior", count: 110 },
];

const verificationData = [
  { week: "Week 1", new: 45, verified: 30 },
  { week: "Week 2", new: 52, verified: 48 },
  { week: "Week 3", new: 38, verified: 40 },
  { week: "Week 4", new: 65, verified: 55 },
];

const COLORS = ["#3b82f6", "#10b981", "#f59e0b", "#8b5cf6", "#ec4899"];

export default function StaffDashboard() {
  const t = useTranslations("StaffDashboard");

  const stats = [
    {
      title: t("qTitle"),
      value: "1,140",
      change: t("qChange"),
      icon: <FileQuestion className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: t("newQTitle"),
      value: "35",
      change: t("newQChange"),
      icon: <HelpCircle className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: t("pendTitle"),
      value: "18",
      change: t("pendChange"),
      icon: <Building2 className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: t("auditTitle"),
      value: "3",
      change: t("auditChange"),
      icon: <AlertTriangle className="h-4 w-4 text-amber-500" />,
    },
  ];

  return (
    <div className="w-full pb-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground">
          {t("desc")}
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

      <div className="grid gap-4 grid-cols-1 md:grid-cols-2 lg:grid-cols-6">
        <Card className="col-span-1 lg:col-span-2">
          <CardHeader>
            <CardTitle>{t("chartQCat")}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full flex items-center justify-center">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={categoryData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={80}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {categoryData.map((entry, index) => (
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

        <Card className="col-span-1 lg:col-span-4">
          <CardHeader>
            <CardTitle>{t("chartQLevel")}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={levelData} margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                  <XAxis dataKey="level" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <RechartsTooltip cursor={{ fill: '#f3f4f6' }} contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                  <Bar dataKey="count" name="Questions" fill="#3b82f6" radius={[4, 4, 0, 0]} barSize={40} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card className="col-span-1 lg:col-span-6">
          <CardHeader>
            <CardTitle>{t("chartVerif")}</CardTitle>
          </CardHeader>
          <CardContent className="pl-2">
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={verificationData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                  <XAxis dataKey="week" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <RechartsTooltip cursor={{ fill: 'transparent' }} contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                  <Legend />
                  <Line type="monotone" name="New Companies" dataKey="new" stroke="#8b5cf6" strokeWidth={2} activeDot={{ r: 6 }} />
                  <Line type="monotone" name="Verified Companies" dataKey="verified" stroke="#10b981" strokeWidth={2} activeDot={{ r: 6 }} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
