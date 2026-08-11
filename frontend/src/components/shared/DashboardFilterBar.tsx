"use client";

import { useState, useEffect } from "react";
import { DashboardFilter } from "@/types/dashboard.types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Filter, RotateCcw } from "lucide-react";

interface DashboardFilterBarProps {
  onFilterChange: (filters: DashboardFilter) => void;
}

export function DashboardFilterBar({ onFilterChange }: DashboardFilterBarProps) {
  const [year, setYear] = useState<string>("");
  const [month, setMonth] = useState<string>("");
  const [fromDate, setFromDate] = useState<string>("");
  const [toDate, setToDate] = useState<string>("");

  const isDateSelected = !!fromDate || !!toDate;

  // Auto-clear year/month if a date is selected, per rules
  useEffect(() => {
    if (isDateSelected) {
      setYear("");
      setMonth("");
    }
  }, [isDateSelected]);

  const handleApply = () => {
    onFilterChange({
      year: year ? parseInt(year, 10) : null,
      month: month ? parseInt(month, 10) : null,
      fromDate: fromDate || null,
      toDate: toDate || null,
    });
  };

  const handleReset = () => {
    setYear("");
    setMonth("");
    setFromDate("");
    setToDate("");
    onFilterChange({
      year: null,
      month: null,
      fromDate: null,
      toDate: null,
    });
  };

  const currentYear = new Date().getFullYear();
  const years = Array.from({ length: 5 }, (_, i) => currentYear - i);
  const months = Array.from({ length: 12 }, (_, i) => i + 1);

  return (
    <div className="bg-card border border-border rounded-xl p-4 flex flex-col sm:flex-row items-end gap-4 shadow-sm mb-6">
      <div className="grid grid-cols-2 md:flex flex-1 gap-4 w-full">
        <div className="space-y-1.5 flex-1 min-w-[120px]">
          <label className="text-xs font-semibold text-muted-foreground">Year</label>
          <Select value={year} onValueChange={(val) => setYear(val || "")} disabled={isDateSelected}>
            <SelectTrigger className="w-full h-9">
              <SelectValue placeholder="All Years" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Years</SelectItem>
              {years.map((y) => (
                <SelectItem key={y} value={y.toString()}>
                  {y}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-1.5 flex-1 min-w-[120px]">
          <label className="text-xs font-semibold text-muted-foreground">Month</label>
          <Select value={month} onValueChange={(val) => setMonth(val || "")} disabled={isDateSelected || !year || year === "all"}>
            <SelectTrigger className="w-full h-9">
              <SelectValue placeholder="All Months" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Months</SelectItem>
              {months.map((m) => (
                <SelectItem key={m} value={m.toString()}>
                  Month {m}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-1.5 flex-1 min-w-[140px]">
          <label className="text-xs font-semibold text-muted-foreground">From Date</label>
          <Input 
            type="date" 
            value={fromDate} 
            onChange={(e) => setFromDate(e.target.value)} 
            className="h-9"
          />
        </div>

        <div className="space-y-1.5 flex-1 min-w-[140px]">
          <label className="text-xs font-semibold text-muted-foreground">To Date</label>
          <Input 
            type="date" 
            value={toDate} 
            onChange={(e) => setToDate(e.target.value)} 
            className="h-9"
          />
        </div>
      </div>

      <div className="flex items-center gap-2 mt-4 sm:mt-0">
        <Button onClick={handleReset} variant="outline" size="sm" className="h-9 whitespace-nowrap">
          <RotateCcw className="w-4 h-4 mr-2" />
          Reset
        </Button>
        <Button onClick={handleApply} size="sm" className="h-9 whitespace-nowrap">
          <Filter className="w-4 h-4 mr-2" />
          Filter
        </Button>
      </div>
    </div>
  );
}
