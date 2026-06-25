'use client';

import React, { useState, useEffect } from 'react';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { Checkbox } from '@/components/ui/checkbox';
import { Slider } from '@/components/ui/slider';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogTrigger, DialogClose } from '@/components/ui/dialog';
import { Search, MapPin, ChevronDown, Filter, X } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { cn } from '@/lib/utils';
import {
  LEVELS,
  WORKING_MODELS,
  JOB_DOMAINS,
  COMPANY_INDUSTRIES,
  COMPANY_TYPES
} from '@/lib/job-constants';

// Constants
const LOCATIONS = [
  "Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Cần Thơ", "Hải Phòng",
  "An Giang", "Bà Rịa - Vũng Tàu", "Bắc Giang", "Bắc Kạn", "Bạc Liêu", "Bắc Ninh", "Bến Tre", "Bình Định", "Bình Dương", "Bình Phước", "Bình Thuận", "Cà Mau", "Cao Bằng", "Đắk Lắk", "Đắk Nông", "Điện Biên", "Đồng Nai", "Đồng Tháp", "Gia Lai", "Hà Giang", "Hà Nam", "Hà Tĩnh", "Hải Dương", "Hậu Giang", "Hòa Bình", "Hưng Yên", "Khánh Hòa", "Kiên Giang", "Kon Tum", "Lai Châu", "Lâm Đồng", "Lạng Sơn", "Lào Cai", "Long An", "Nam Định", "Nghệ An", "Ninh Bình", "Ninh Thuận", "Phú Thọ", "Phú Yên", "Quảng Bình", "Quảng Nam", "Quảng Ngãi", "Quảng Ninh", "Quảng Trị", "Sóc Trăng", "Sơn La", "Tây Ninh", "Thái Bình", "Thái Nguyên", "Thanh Hóa", "Thừa Thiên Huế", "Tiền Giang", "Trà Vinh", "Tuyên Quang", "Vĩnh Long", "Vĩnh Phúc", "Yên Bái",
  "International", "Others"
];

// Helper to parse array params safely
const parseArrayParam = (param: string | null) => param ? param.split(',').filter(Boolean) : [];

const removeAccents = (str: string) => {
  return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
};

const customFilter = (value: string, search: string) => {
  if (removeAccents(value).includes(removeAccents(search))) return 1;
  return 0;
};

// Reusable MultiSelect Dropdown Component for Quick Filters & Modal
const FilterCombobox = ({
  title,
  options,
  selected,
  onChange,
  searchPlaceholder = "Search..."
}: {
  title: string,
  options: string[],
  selected: string[],
  onChange: (val: string[]) => void,
  searchPlaceholder?: string
}) => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger
        render={
          <Button variant="outline" className={cn("justify-between font-normal h-10 w-full bg-white", selected.length > 0 && "border-primary text-primary")}>
            <span className="truncate mr-2">
              {selected.length === 0 ? title : `${title} (${selected.length})`}
            </span>
            <ChevronDown className="h-4 w-4 opacity-50" />
          </Button>
        }
      />
      <PopoverContent className="w-[280px] p-0" align="start">
        <Command filter={customFilter}>
          <CommandInput placeholder={searchPlaceholder} />
          <CommandList>
            <CommandEmpty>No results found.</CommandEmpty>
            <CommandGroup>
              <ScrollArea className="h-64">
                {options.map((opt) => {
                  const isSelected = selected.includes(opt);
                  return (
                    <CommandItem
                      key={opt}
                      onSelect={() => {
                        const newSelected = isSelected
                          ? selected.filter((s) => s !== opt)
                          : [...selected, opt];
                        onChange(newSelected);
                      }}
                      className="flex items-center gap-2 cursor-pointer"
                    >
                      <Checkbox checked={isSelected} className="pointer-events-none" />
                      <span>{opt}</span>
                    </CommandItem>
                  );
                })}
              </ScrollArea>
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
};

export function JobSearchFilter() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  // URL States
  const urlQuery = searchParams.get('query') || '';
  const urlLocation = searchParams.get('location') || '';
  const urlLevels = parseArrayParam(searchParams.get('levels'));
  const urlWorkingModels = parseArrayParam(searchParams.get('workingModels'));
  const urlJobDomains = parseArrayParam(searchParams.get('jobDomains'));
  const urlCompanyIndustries = parseArrayParam(searchParams.get('companyIndustries'));
  const urlCompanyTypes = parseArrayParam(searchParams.get('companyTypes'));
  const urlMinSalary = searchParams.get('minSalary') ? parseInt(searchParams.get('minSalary')!) : 0;
  const urlMaxSalary = searchParams.get('maxSalary') ? parseInt(searchParams.get('maxSalary')!) : 10000;

  // Local States for Inputs/Modals (to avoid premature URL updates)
  const [keyword, setKeyword] = useState(urlQuery);
  const [location, setLocation] = useState(urlLocation);

  // Pending States for Modal
  const [pendingLevels, setPendingLevels] = useState<string[]>(urlLevels);
  const [pendingWorkingModels, setPendingWorkingModels] = useState<string[]>(urlWorkingModels);
  const [pendingJobDomains, setPendingJobDomains] = useState<string[]>(urlJobDomains);
  const [pendingCompanyIndustries, setPendingCompanyIndustries] = useState<string[]>(urlCompanyIndustries);
  const [pendingCompanyTypes, setPendingCompanyTypes] = useState<string[]>(urlCompanyTypes);
  const [pendingSalary, setPendingSalary] = useState<number[]>([urlMinSalary, urlMaxSalary]);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [locationOpen, setLocationOpen] = useState(false);

  // Sync state when URL changes (e.g. from Clear All or browser back button)
  useEffect(() => {
    setKeyword(urlQuery);
    setLocation(urlLocation);
    setPendingLevels(urlLevels);
    setPendingWorkingModels(urlWorkingModels);
    setPendingJobDomains(urlJobDomains);
    setPendingCompanyIndustries(urlCompanyIndustries);
    setPendingCompanyTypes(urlCompanyTypes);
    setPendingSalary([urlMinSalary, urlMaxSalary]);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]);

  // Apply all filters to URL
  const applyFilters = (updates: any = {}) => {
    const params = new URLSearchParams(searchParams.toString());

    const current = {
      query: keyword,
      location: location,
      levels: pendingLevels,
      workingModels: pendingWorkingModels,
      jobDomains: pendingJobDomains,
      companyIndustries: pendingCompanyIndustries,
      companyTypes: pendingCompanyTypes,
      salary: pendingSalary,
      ...updates
    };

    if (current.query) params.set('query', current.query); else params.delete('query');
    if (current.location) params.set('location', current.location); else params.delete('location');

    if (current.levels.length > 0) params.set('levels', current.levels.join(',')); else params.delete('levels');
    if (current.workingModels.length > 0) params.set('workingModels', current.workingModels.join(',')); else params.delete('workingModels');
    if (current.jobDomains.length > 0) params.set('jobDomains', current.jobDomains.join(',')); else params.delete('jobDomains');
    if (current.companyIndustries.length > 0) params.set('companyIndustries', current.companyIndustries.join(',')); else params.delete('companyIndustries');
    if (current.companyTypes.length > 0) params.set('companyTypes', current.companyTypes.join(',')); else params.delete('companyTypes');

    // Salary bounds check
    if (current.salary[0] > 0) params.set('minSalary', current.salary[0].toString()); else params.delete('minSalary');
    if (current.salary[1] < 10000) params.set('maxSalary', current.salary[1].toString()); else params.delete('maxSalary');

    params.set('page', '1');
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    setIsModalOpen(false);
  };

  const handleMainSearch = (e: React.FormEvent) => {
    e.preventDefault();
    applyFilters();
  };

  const resetFilters = () => {
    setPendingLevels([]);
    setPendingWorkingModels([]);
    setPendingJobDomains([]);
    setPendingCompanyIndustries([]);
    setPendingCompanyTypes([]);
    setPendingSalary([0, 10000]);
  };

  const hasActiveFilters =
    pendingLevels.length > 0 ||
    pendingWorkingModels.length > 0 ||
    pendingJobDomains.length > 0 ||
    pendingCompanyIndustries.length > 0 ||
    pendingCompanyTypes.length > 0 ||
    pendingSalary[0] > 0 ||
    pendingSalary[1] < 10000;

  // Quick Filter Action
  const applyQuickFilter = (key: string, val: any) => {
    if (key === 'levels') setPendingLevels(val);
    if (key === 'workingModels') setPendingWorkingModels(val);
    if (key === 'jobDomains') setPendingJobDomains(val);
    applyFilters({ [key]: val });
  };

  return (
    <div className="flex flex-col gap-4 bg-slate-50 p-4 rounded-xl mb-6 border border-slate-200">

      {/* PART 1: Main Search Bar */}
      <form onSubmit={handleMainSearch} className="flex flex-col md:flex-row gap-3">
        {/* Search Input */}
        <div className="flex-[2] relative flex items-center">
          <Search className="absolute left-4 h-5 w-5 text-slate-400" />
          <Input
            placeholder="Enter keyword skills, job title, company..."
            className="pl-12 pr-10 h-12 text-base border-slate-300 focus-visible:ring-primary"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
          />
          {keyword && (
            <button
              type="button"
              onClick={() => {
                setKeyword("");
                applyFilters({ query: "" });
              }}
              className="absolute right-3 p-1 rounded-full text-slate-400 hover:text-slate-600 hover:bg-slate-100 transition-colors"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        {/* Location Combobox */}
        <div className="flex-1 min-w-[200px]">
          <Popover open={locationOpen} onOpenChange={setLocationOpen}>
            <PopoverTrigger
              render={
                <Button variant="outline" className="w-full justify-between h-12 text-base font-normal border-slate-300 bg-white">
                  <div className="flex items-center gap-2 truncate text-slate-600">
                    <MapPin className="h-5 w-5 text-slate-400" />
                    {location || "All Cities"}
                  </div>
                  <ChevronDown className="h-4 w-4 opacity-50" />
                </Button>
              }
            />
            <PopoverContent className="w-[300px] p-0" align="start">
              <Command filter={customFilter}>
                <CommandInput placeholder="Search location..." />
                <CommandList>
                  <CommandEmpty>No location found.</CommandEmpty>
                  <CommandGroup>
                    <ScrollArea className="h-[300px]">
                      <CommandItem onSelect={() => {
                        setLocation("");
                        applyFilters({ location: "" });
                        setLocationOpen(false);
                      }}>
                        All Cities
                      </CommandItem>
                      {LOCATIONS.map((loc) => (
                        <CommandItem key={loc} onSelect={() => {
                          setLocation(loc);
                          applyFilters({ location: loc });
                          setLocationOpen(false);
                        }}>
                          {loc}
                        </CommandItem>
                      ))}
                    </ScrollArea>
                  </CommandGroup>
                </CommandList>
              </Command>
            </PopoverContent>
          </Popover>
        </div>

        {/* Search Button */}
        <Button type="submit" className="md:w-32 h-12 text-base font-semibold">
          Search
        </Button>
      </form>

      {/* PART 2: Quick Filters */}
      <div className="flex flex-wrap items-center gap-3 w-full">
        <div className="w-[180px]">
          <FilterCombobox
            title="Level"
            options={LEVELS}
            selected={pendingLevels}
            onChange={(val) => applyQuickFilter('levels', val)}
          />
        </div>
        <div className="w-[180px]">
          <FilterCombobox
            title="Working Model"
            options={WORKING_MODELS}
            selected={pendingWorkingModels}
            onChange={(val) => applyQuickFilter('workingModels', val)}
          />
        </div>

        {/* Quick Salary Popover */}
        <div className="w-[180px]">
          <Popover>
            <PopoverTrigger
              render={
                <Button variant="outline" className={cn("justify-between font-normal h-10 w-full bg-white", (pendingSalary[0] > 0 || pendingSalary[1] < 10000) && "border-primary text-primary")}>
                  <span className="truncate mr-2">
                    {(pendingSalary[0] > 0 || pendingSalary[1] < 10000) ? `$${pendingSalary[0]} - $${pendingSalary[1]}` : 'Salary (USD)'}
                  </span>
                  <ChevronDown className="h-4 w-4 opacity-50" />
                </Button>
              }
            />
            <PopoverContent className="w-80 p-6" align="start">
              <div className="flex flex-col gap-6">
                <div className="flex justify-between items-center">
                  <span className="font-semibold text-sm">Salary Range</span>
                  <span className="text-sm font-medium text-primary">${pendingSalary[0]} - ${pendingSalary[1]}</span>
                </div>
                <Slider
                  defaultValue={[0, 10000]}
                  value={pendingSalary}
                  min={0}
                  max={10000}
                  step={100}
                  onValueChange={(val) => setPendingSalary(val as number[])}
                />
                <Button className="w-full mt-2" size="sm" onClick={() => applyQuickFilter('salary', pendingSalary)}>Apply</Button>
              </div>
            </PopoverContent>
          </Popover>
        </div>

        <div className="w-[180px]">
          <FilterCombobox
            title="Job Domain"
            options={JOB_DOMAINS}
            selected={pendingJobDomains}
            onChange={(val) => applyQuickFilter('jobDomains', val)}
            searchPlaceholder="Search domain..."
          />
        </div>

        {/* Filter Button (Opens Modal) */}
        <div className="ml-auto flex items-center gap-2">
          {hasActiveFilters && (
            <Button
              variant="ghost"
              onClick={() => {
                resetFilters();
                applyFilters({
                  levels: [],
                  workingModels: [],
                  jobDomains: [],
                  companyIndustries: [],
                  companyTypes: [],
                  salary: [0, 10000]
                });
              }}
              className="h-10 text-slate-500 hover:text-slate-900 px-3"
            >
              Clear Filters
            </Button>
          )}
          <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
            <DialogTrigger
              render={
                <Button variant="outline" className="h-10 flex items-center gap-2 text-slate-700 bg-white">
                  <Filter className="h-4 w-4" />
                  All Filters
                </Button>
              }
            />
            <DialogContent className="sm:max-w-[500px] p-0 overflow-hidden">
              <DialogHeader className="p-6 pb-4 border-b">
                <DialogTitle className="text-xl">Advanced Filters</DialogTitle>
              </DialogHeader>

              {/* PART 3: Advanced Filter Modal Body */}
              <ScrollArea className="h-[500px] px-6 py-4">
                <div className="flex flex-col gap-6">

                  {/* Row 1: Level */}
                  <div className="space-y-3">
                    <h4 className="font-medium text-sm text-slate-700">Level</h4>
                    <div className="flex flex-wrap gap-2">
                      {LEVELS.map(lvl => {
                        const isSelected = pendingLevels.includes(lvl);
                        return (
                          <Badge
                            key={lvl}
                            variant={isSelected ? "default" : "outline"}
                            className={cn("cursor-pointer px-3 py-1.5 text-sm font-normal rounded-full",
                              !isSelected && "bg-white text-slate-600 hover:bg-slate-100 border-slate-200"
                            )}
                            onClick={() => setPendingLevels(prev => isSelected ? prev.filter(x => x !== lvl) : [...prev, lvl])}
                          >
                            {lvl}
                          </Badge>
                        );
                      })}
                    </div>
                  </div>

                  {/* Row 2: Working Model */}
                  <div className="space-y-3">
                    <h4 className="font-medium text-sm text-slate-700">Working Model</h4>
                    <div className="flex flex-wrap gap-2">
                      {WORKING_MODELS.map(wm => {
                        const isSelected = pendingWorkingModels.includes(wm);
                        return (
                          <Badge
                            key={wm}
                            variant={isSelected ? "default" : "outline"}
                            className={cn("cursor-pointer px-3 py-1.5 text-sm font-normal rounded-full",
                              !isSelected && "bg-white text-slate-600 hover:bg-slate-100 border-slate-200"
                            )}
                            onClick={() => setPendingWorkingModels(prev => isSelected ? prev.filter(x => x !== wm) : [...prev, wm])}
                          >
                            {wm}
                          </Badge>
                        );
                      })}
                    </div>
                  </div>

                  {/* Row 3: Salary */}
                  <div className="space-y-4 pt-2">
                    <div className="flex justify-between items-center">
                      <h4 className="font-medium text-sm text-slate-700">Monthly Salary (USD)</h4>
                      <span className="text-sm font-semibold text-primary px-3 py-1 bg-primary/10 rounded-full">
                        ${pendingSalary[0]} - ${pendingSalary[1]}
                      </span>
                    </div>
                    <div className="px-2">
                      <Slider
                        value={pendingSalary}
                        min={0}
                        max={10000}
                        step={100}
                        onValueChange={(val) => setPendingSalary(val as number[])}
                        className="mt-6 mb-2"
                      />
                    </div>
                    <div className="flex justify-between text-xs text-muted-foreground px-1">
                      <span>$0</span>
                      <span>$10,000+</span>
                    </div>
                  </div>

                  {/* Row 4: Job Domain */}
                  <div className="space-y-3">
                    <h4 className="font-medium text-sm text-slate-700">Job Domain</h4>
                    <div className="border border-slate-200 rounded-md overflow-hidden bg-white">
                      <Command filter={customFilter}>
                        <CommandInput placeholder="Search domain..." className="border-none h-10" />
                        <CommandList>
                          <CommandEmpty>No domain found.</CommandEmpty>
                          <CommandGroup>
                            <ScrollArea className="h-[200px]">
                              {JOB_DOMAINS.map(jd => (
                                <CommandItem
                                  key={jd}
                                  onSelect={() => {
                                    setPendingJobDomains(prev => prev.includes(jd) ? prev.filter(x => x !== jd) : [...prev, jd]);
                                  }}
                                  className="flex items-center gap-2 cursor-pointer px-3 py-2"
                                >
                                  <Checkbox checked={pendingJobDomains.includes(jd)} className="pointer-events-none" />
                                  <span className="truncate">{jd}</span>
                                </CommandItem>
                              ))}
                            </ScrollArea>
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </div>
                  </div>

                  {/* Row 5: Company Industry */}
                  <div className="space-y-3">
                    <h4 className="font-medium text-sm text-slate-700">Company Industry</h4>
                    <div className="border border-slate-200 rounded-md overflow-hidden bg-white">
                      <Command filter={customFilter}>
                        <CommandInput placeholder="Search industry..." className="border-none h-10" />
                        <CommandList>
                          <CommandEmpty>No industry found.</CommandEmpty>
                          <CommandGroup>
                            <ScrollArea className="h-[200px]">
                              {COMPANY_INDUSTRIES.map(ci => (
                                <CommandItem
                                  key={ci}
                                  onSelect={() => {
                                    setPendingCompanyIndustries(prev => prev.includes(ci) ? prev.filter(x => x !== ci) : [...prev, ci]);
                                  }}
                                  className="flex items-center gap-2 cursor-pointer px-3 py-2"
                                >
                                  <Checkbox checked={pendingCompanyIndustries.includes(ci)} className="pointer-events-none" />
                                  <span className="truncate" title={ci}>{ci}</span>
                                </CommandItem>
                              ))}
                            </ScrollArea>
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </div>
                  </div>

                  {/* Row 6: Company Type */}
                  <div className="space-y-3 pb-6">
                    <h4 className="font-medium text-sm text-slate-700">Company Type</h4>
                    <div className="flex flex-wrap gap-2">
                      {COMPANY_TYPES.map(ct => {
                        const isSelected = pendingCompanyTypes.includes(ct);
                        return (
                          <Badge
                            key={ct}
                            variant={isSelected ? "default" : "outline"}
                            className={cn("cursor-pointer px-3 py-1.5 text-sm font-normal rounded-full",
                              !isSelected && "bg-white text-slate-600 hover:bg-slate-100 border-slate-200"
                            )}
                            onClick={() => setPendingCompanyTypes(prev => isSelected ? prev.filter(x => x !== ct) : [...prev, ct])}
                          >
                            {ct}
                          </Badge>
                        );
                      })}
                    </div>
                  </div>

                </div>
              </ScrollArea>

              {/* Modal Footer */}
              <DialogFooter className="p-4 border-t flex sm:justify-between items-center bg-slate-50">
                <Button variant="ghost" onClick={resetFilters} className="text-slate-500 hover:text-slate-900">
                  Reset Filter
                </Button>
                <Button onClick={() => applyFilters()} className="w-32">
                  Apply
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
      </div>

    </div>
  );
}
