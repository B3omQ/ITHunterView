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
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  Legend,
} from "recharts";

const userGrowthData = [
  { month: "Jan", users: 1200, revenue: 4000 },
  { month: "Feb", users: 1900, revenue: 5500 },
  { month: "Mar", users: 2400, revenue: 6800 },
  { month: "Apr", users: 2800, revenue: 7500 },
  { month: "May", users: 3500, revenue: 9200 },
  { month: "Jun", users: 4100, revenue: 11000 },
  { month: "Jul", users: 4800, revenue: 13500 },
];

const tokenUsageData = [
  { day: "Mon", tokens: 125000 },
  { day: "Tue", tokens: 142000 },
  { day: "Wed", tokens: 185000 },
  { day: "Thu", tokens: 195000 },
  { day: "Fri", tokens: 160000 },
  { day: "Sat", tokens: 85000 },
  { day: "Sun", tokens: 75000 },
];

const subscriptionData = [
  { name: "Basic", value: 45 },
  { name: "Pro", value: 35 },
  { name: "Enterprise", value: 20 },
];

const COLORS = ["#3b82f6", "#8b5cf6", "#ec4899"];

export default function AdminDashboard() {
  const stats = [
    {
      title: "Total Revenue",
      value: "$13,500",
      change: "+15% from last month",
      icon: <Banknote className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Total Users",
      value: "4,800",
      change: "+12% from last month",
      icon: <Users className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "AI Tokens Used",
      value: "967K",
      change: "+5% from last week",
      icon: <Coins className="h-4 w-4 text-muted-foreground" />,
    },
    {
      title: "Transactions",
      value: "1,204",
      change: "+8% from last month",
      icon: <Activity className="h-4 w-4 text-muted-foreground" />,
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Admin Dashboard</h1>
        <p className="text-muted-foreground">
          Platform overview, financial growth, and AI usage metrics.
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
            <CardTitle>User & Revenue Growth</CardTitle>
          </CardHeader>
          <CardContent className="pl-2">
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={userGrowthData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
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

        <Card className="col-span-1 lg:col-span-3">
          <CardHeader>
            <CardTitle>AI Token Usage (Weekly)</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={tokenUsageData} margin={{ top: 5, right: 0, bottom: 5, left: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e5e7eb" />
                  <XAxis dataKey="day" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} tickFormatter={(value) => `${value / 1000}k`} />
                  <RechartsTooltip cursor={{ fill: '#f3f4f6' }} contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb' }} />
                  <Bar dataKey="tokens" name="Tokens" fill="#ec4899" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <Card className="col-span-1 lg:col-span-3">
          <CardHeader>
            <CardTitle>Subscriptions Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px] w-full flex items-center justify-center">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={subscriptionData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={80}
                    paddingAngle={5}
                    dataKey="value"
                  >
                    {subscriptionData.map((entry, index) => (
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
      </div>
    </div>
  );
}
