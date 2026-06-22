'use client';

import React, { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Search, MapPin, Briefcase } from 'lucide-react';
import type { JobSearchQuery } from '@/types/job.types';

interface JobSearchFilterProps {
  initialQuery?: JobSearchQuery;
  onSearch: (query: JobSearchQuery) => void;
}

export function JobSearchFilter({ initialQuery, onSearch }: JobSearchFilterProps) {
  const [keyword, setKeyword] = useState(initialQuery?.keyword || '');
  const [location, setLocation] = useState(initialQuery?.location || 'ALL');
  const [jobType, setJobType] = useState(initialQuery?.jobType || 'ALL');

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    onSearch({
      keyword,
      location: location === 'ALL' ? undefined : location,
      jobType: jobType === 'ALL' ? undefined : jobType,
      page: 1, // Reset to first page on new search
    });
  };

  return (
    <form onSubmit={handleSearch} className="flex flex-col md:flex-row gap-4 w-full p-4 bg-white rounded-lg border shadow-sm">
      <div className="flex-1 relative">
        <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
        <Input 
          placeholder="Job title, keyword, or company" 
          className="pl-9" 
          value={keyword}
          onChange={(e) => setKeyword(e.target.value)}
        />
      </div>
      
      <div className="flex-1 relative">
        <MapPin className="absolute left-3 top-3 h-4 w-4 text-muted-foreground z-10" />
        <Select value={location} onValueChange={(val) => setLocation(val || 'ALL')}>
          <SelectTrigger className="pl-9">
            <SelectValue placeholder="All Locations" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">All Locations</SelectItem>
            <SelectItem value="Hà Nội">Hà Nội</SelectItem>
            <SelectItem value="Hồ Chí Minh">Hồ Chí Minh</SelectItem>
            <SelectItem value="Đà Nẵng">Đà Nẵng</SelectItem>
            <SelectItem value="Remote">Remote</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="flex-1 relative">
        <Briefcase className="absolute left-3 top-3 h-4 w-4 text-muted-foreground z-10" />
        <Select value={jobType} onValueChange={(val) => setJobType(val || 'ALL')}>
          <SelectTrigger className="pl-9">
            <SelectValue placeholder="Job Type" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">All Types</SelectItem>
            <SelectItem value="FULL_TIME">Full Time</SelectItem>
            <SelectItem value="PART_TIME">Part Time</SelectItem>
            <SelectItem value="CONTRACT">Contract</SelectItem>
            <SelectItem value="FREELANCE">Freelance</SelectItem>
            <SelectItem value="INTERNSHIP">Internship</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Button type="submit" className="md:w-32">Search</Button>
    </form>
  );
}
