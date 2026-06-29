"use client"

import React from 'react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { PROVINCE_OPTIONS } from "@/lib/job-constants"

interface ProvinceSelectProps {
  value: string;
  onChange: (value: string) => void;
  className?: string;
  placeholder?: string;
}

export function ProvinceSelect({ value, onChange, className, placeholder = "Select Province/City" }: ProvinceSelectProps) {
  return (
    <Select value={value} onValueChange={(val) => { if (val) onChange(val); }}>
      <SelectTrigger className={className}>
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {PROVINCE_OPTIONS.map((prov) => (
          <SelectItem key={prov.value} value={prov.value}>
            {prov.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
