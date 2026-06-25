'use client';

import React, { useState } from 'react';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Search, MapPin, Briefcase, Building2, Code2, DollarSign, Calendar } from 'lucide-react';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";

export function JobSearchFilter() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const [keyword, setKeyword] = useState(searchParams.get('query') || '');
  const [skill, setSkill] = useState(searchParams.get('skill') || '');
  const [companyName, setCompanyName] = useState(searchParams.get('companyName') || '');
  const [minSalary, setMinSalary] = useState(searchParams.get('minSalary') || '');
  
  const location = searchParams.get('location') || 'ALL';
  const jobType = searchParams.get('jobType') || 'ALL';
  const currency = searchParams.get('currency') || 'USD';
  const postedWithinDays = searchParams.get('postedWithinDays') || 'ALL';

  const updateUrl = (
    newKeyword: string, 
    newLocation: string, 
    newJobType: string,
    newSkill: string,
    newCompanyName: string,
    newMinSalary: string,
    newCurrency: string,
    newPostedWithinDays: string
  ) => {
    const params = new URLSearchParams(searchParams.toString());
    
    if (newKeyword) params.set('query', newKeyword);
    else params.delete('query');
    
    if (newLocation && newLocation !== 'ALL') params.set('location', newLocation);
    else params.delete('location');
    
    if (newJobType && newJobType !== 'ALL') params.set('jobType', newJobType);
    else params.delete('jobType');

    if (newSkill) params.set('skill', newSkill);
    else params.delete('skill');

    if (newCompanyName) params.set('companyName', newCompanyName);
    else params.delete('companyName');

    if (newMinSalary) params.set('minSalary', newMinSalary);
    else params.delete('minSalary');

    if (newMinSalary && newCurrency) params.set('currency', newCurrency);
    else params.delete('currency');

    if (newPostedWithinDays && newPostedWithinDays !== 'ALL') params.set('postedWithinDays', newPostedWithinDays);
    else params.delete('postedWithinDays');
    
    // Reset to page 1 on new search
    params.set('page', '1');
    
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    updateUrl(keyword, location, jobType, skill, companyName, minSalary, currency, postedWithinDays);
  };

  const handleLocationChange = (val: string) => {
    updateUrl(keyword, val, jobType, skill, companyName, minSalary, currency, postedWithinDays);
  };

  const handleJobTypeChange = (val: string) => {
    updateUrl(keyword, location, val, skill, companyName, minSalary, currency, postedWithinDays);
  };

  const handleCurrencyChange = (val: string) => {
    updateUrl(keyword, location, jobType, skill, companyName, minSalary, val, postedWithinDays);
  };

  const handleDatePostedChange = (val: string) => {
    updateUrl(keyword, location, jobType, skill, companyName, minSalary, currency, val);
  };

  return (
    <div className="bg-white rounded-lg border border-slate-200 p-4">
      <form onSubmit={handleSearch} className="flex flex-col gap-4 w-full">
        <div className="flex flex-col md:flex-row gap-4 w-full">
          <div className="flex-[2] relative">
            <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
            <Input 
              placeholder="Job title, keyword..." 
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
        </div>

        <Accordion type="single" collapsible className="w-full">
          <AccordionItem value="advanced-filters" className="border-b-0">
            <AccordionTrigger className="text-sm py-2 hover:no-underline text-muted-foreground hover:text-foreground">
              Advanced Filters
            </AccordionTrigger>
            <AccordionContent>
              <div className="flex flex-col md:flex-row gap-4 pt-2">
                <div className="flex-1 relative">
                  <Code2 className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                  <Input 
                    placeholder="Skill (e.g. React, Node.js)" 
                    className="pl-9" 
                    value={skill}
                    onChange={(e) => setSkill(e.target.value)}
                  />
                </div>
                
                <div className="flex-1 relative">
                  <Building2 className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                  <Input 
                    placeholder="Company Name" 
                    className="pl-9" 
                    value={companyName}
                    onChange={(e) => setCompanyName(e.target.value)}
                  />
                </div>

                <div className="flex-1 flex gap-2">
                  <div className="relative flex-[2]">
                    <DollarSign className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                    <Input 
                      placeholder="Min Salary" 
                      type="number"
                      className="pl-9" 
                      value={minSalary}
                      onChange={(e) => setMinSalary(e.target.value)}
                    />
                  </div>
                  <Select value={currency} onValueChange={handleCurrencyChange}>
                    <SelectTrigger className="w-[90px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="USD">USD</SelectItem>
                      <SelectItem value="VND">VND</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="flex-1 relative">
                  <Calendar className="absolute left-3 top-3 h-4 w-4 text-muted-foreground z-10" />
                  <Select value={postedWithinDays} onValueChange={handleDatePostedChange}>
                    <SelectTrigger className="pl-9">
                      <SelectValue placeholder="Date Posted" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="ALL">Any time</SelectItem>
                      <SelectItem value="1">Past 24 hours</SelectItem>
                      <SelectItem value="7">Past 7 days</SelectItem>
                      <SelectItem value="30">Past 30 days</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </form>
    </div>
  );
}
