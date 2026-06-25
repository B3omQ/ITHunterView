'use client';

import React, { useState } from 'react';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Search, MapPin, Briefcase } from 'lucide-react';

export function JobSearchFilter() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const [keyword, setKeyword] = useState(searchParams.get('query') || '');
  const location = searchParams.get('location') || 'ALL';
  const jobType = searchParams.get('jobType') || 'ALL';

  const updateUrl = (newKeyword: string, newLocation: string, newJobType: string) => {
    const params = new URLSearchParams(searchParams.toString());
    
    if (newKeyword) params.set('query', newKeyword);
    else params.delete('query');
    
    if (newLocation && newLocation !== 'ALL') params.set('location', newLocation);
    else params.delete('location');
    
    if (newJobType && newJobType !== 'ALL') params.set('jobType', newJobType);
    else params.delete('jobType');
    
    // Reset to page 1 on new search
    params.set('page', '1');
    
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    updateUrl(keyword, location, jobType);
  };

  const handleLocationChange = (val: string) => {
    updateUrl(keyword, val, jobType);
  };

  const handleJobTypeChange = (val: string) => {
    updateUrl(keyword, location, val);
  };

  return (
    <form onSubmit={handleSearch} className="flex flex-col md:flex-row gap-4 w-full p-4 bg-white rounded-lg border border-slate-200">
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
        <Select value={location} onValueChange={handleLocationChange}>
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
        <Select value={jobType} onValueChange={handleJobTypeChange}>
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
