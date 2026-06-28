'use client';

import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';
import { Check, ChevronsUpDown, Info } from 'lucide-react';
import { cn } from '@/lib/utils';

import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { ScrollArea } from '@/components/ui/scroll-area';

import { COMPANY_INDUSTRIES, COMPANY_TYPES } from '@/lib/job-constants';
import { useGetMyCompany, useCreateOrUpdateProfile, useUpdateProfile } from '@/hooks/useCompany';
import { uploadService } from '@/services/upload.service';
import { CompanyLogo } from '@/components/shared/CompanyLogo';

const profileObjectSchema = z.object({
  name: z.string().min(1, 'Please enter company name'),
  sameAsCompanyName: z.boolean().optional(),
  tradeName: z.string().min(1, 'Please enter trade name'),
  contactPhone: z.string().min(1, 'Please enter contact phone number'),
  industry: z.string().min(1, 'Please select industry'),
  mainField: z.string().min(1, 'Please enter main business field'),
  companyEmail: z.string().min(1, 'Please enter company email').email('Invalid email address'),
  companySize: z.string().min(1, 'Please select company size'),
  headquartersAddress: z.string().min(1, 'Please enter headquarters address'),
  description: z.string().min(500, 'Company description must be at least 500 characters'),
  noWebsite: z.boolean().optional(),
  website: z.string().optional(),
  logoUrl: z.string().optional(),
  companyType: z.string().optional(),
  employeeBenefits: z.string().optional(),
  targetCustomers: z.array(z.string()).default([]),
  operatingMarkets: z.array(z.string()).default([]),
  companyImages: z.array(z.string()).default([]),
});

type ProfileFormValues = z.infer<typeof profileObjectSchema>;

const profileSchema = profileObjectSchema.refine((data) => {
  if (data.noWebsite) return true;
  if (!data.website || data.website.trim() === '') return false;
  try {
    const urlStr = data.website.includes('://') ? data.website : `https://${data.website}`;
    new URL(urlStr);
    return true;
  } catch {
    return false;
  }
}, {
  message: 'Please enter a valid website URL (or check "No website")',
  path: ['website'],
});

const COMPANY_SIZES = ['1-10', '11-50', '51-200', '201-500', '500+'];
const TARGET_CUSTOMERS_OPTIONS = ['B2B', 'B2C', 'B2G', 'Other'];
const OPERATING_MARKETS_OPTIONS = ['Domestic', 'Asia', 'Europe', 'Americas', 'Australia', 'Other'];

export default function CompanyProfilePage() {
  const router = useRouter();
  const { data: company, isLoading: isFetchingCompany } = useGetMyCompany();
  const { mutateAsync: createProfile, isPending: isCreating } = useCreateOrUpdateProfile();
  const { mutateAsync: updateProfile, isPending: isUpdating } = useUpdateProfile();
  
  const [isUploading, setIsUploading] = useState(false);
  const [isUploadingImages, setIsUploadingImages] = useState(false);

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema) as any,
    defaultValues: {
      name: '',
      sameAsCompanyName: false,
      tradeName: '',
      contactPhone: '',
      industry: '',
      mainField: '',
      companyEmail: '',
      companySize: '',
      headquartersAddress: '',
      description: '',
      noWebsite: false,
      website: '',
      logoUrl: '',
      companyType: '',
      employeeBenefits: '',
      targetCustomers: [],
      operatingMarkets: [],
      companyImages: [],
    },
  });

  const watchNoWebsite = form.watch('noWebsite');
  const watchName = form.watch('name');
  const watchSameAsCompanyName = form.watch('sameAsCompanyName');

  useEffect(() => {
    if (watchSameAsCompanyName) {
      form.setValue('tradeName', watchName || '', { shouldValidate: true });
    }
  }, [watchName, watchSameAsCompanyName, form]);

  useEffect(() => {
    if (company) {
      form.reset({
        name: company.name || '',
        sameAsCompanyName: !!company.name && company.name === company.tradeName,
        tradeName: company.tradeName || '',
        contactPhone: company.contactPhone || '',
        industry: company.industry || '',
        mainField: company.mainField || '',
        companyEmail: company.companyEmail || '',
        companySize: company.companySize || '',
        headquartersAddress: company.headquartersAddress || '',
        description: company.description || '',
        noWebsite: !company.website || company.website.trim() === '',
        website: company.website || '',
        logoUrl: company.logoUrl || '',
        companyType: company.companyType || '',
        employeeBenefits: company.employeeBenefits || '',
        targetCustomers: company.targetCustomers || [],
        operatingMarkets: company.operatingMarkets || [],
        companyImages: company.companyImages || [],
      });
    }
  }, [company, form]);

  const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      toast.error('Please upload an image file (jpeg, jpg, png)');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      toast.error('Logo size must be smaller than 5MB');
      return;
    }

    try {
      setIsUploading(true);
      const res = await uploadService.uploadFile(file, 'company_logos');
      form.setValue('logoUrl', res.data || '', { shouldValidate: true });
      toast.success('Logo uploaded successfully');
    } catch (error) {
      toast.error('Error uploading logo');
    } finally {
      setIsUploading(false);
    }
  };

  const handleImagesUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    const currentImages = form.getValues('companyImages') || [];
    if (currentImages.length + files.length > 5) {
      toast.error('You can only upload up to 5 company images');
      return;
    }

    try {
      setIsUploadingImages(true);
      const newUrls = [...currentImages];
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (!file.type.startsWith('image/')) {
          toast.error(`${file.name} is not an image file.`);
          continue;
        }
        if (file.size > 5 * 1024 * 1024) {
          toast.error(`${file.name} is too large (max 5MB).`);
          continue;
        }
        const res = await uploadService.uploadFile(file, 'company_images');
        if (res.data) {
          newUrls.push(res.data);
        }
      }
      form.setValue('companyImages', newUrls, { shouldValidate: true });
      toast.success('Company gallery images uploaded successfully');
    } catch (error) {
      toast.error('Error uploading images');
    } finally {
      setIsUploadingImages(false);
    }
  };

  const removeImage = (index: number) => {
    const currentImages = form.getValues('companyImages') || [];
    const newUrls = currentImages.filter((_, i) => i !== index);
    form.setValue('companyImages', newUrls, { shouldValidate: true });
  };

  const onSubmit = async (values: ProfileFormValues) => {
    try {
      const payload = {
        ...values,
        website: values.noWebsite ? '' : values.website,
      };

      if (company?.id) {
        await updateProfile({ id: company.id, dto: payload });
      } else {
        await createProfile(payload);
      }
      toast.success('Company profile saved successfully!');
      router.push('/recruiter/company');
    } catch (error) {
      toast.error('An error occurred while saving profile');
    }
  };

  if (isFetchingCompany) {
    return <div className="p-8 text-center text-muted-foreground">Loading profile information...</div>;
  }

  const descriptionLength = form.watch('description')?.length || 0;
  const currentLogo = form.watch('logoUrl');
  const currentImages = form.watch('companyImages') || [];

  return (
    <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-8 w-full">
      <div className="bg-card rounded-xl border p-6 shadow-sm">
        <h2 className="text-xl font-semibold mb-6">Company Profile Details</h2>
        
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            
            {/* Logo Section */}
            <div className="flex items-center gap-6 p-4 bg-muted/30 rounded-lg border">
              <div className="relative w-24 h-24 border rounded-lg overflow-hidden bg-muted flex items-center justify-center shrink-0">
                <CompanyLogo src={currentLogo} alt="Company Logo" fallbackType="building" fallbackIconClassName="w-10 h-10 text-muted-foreground" imageClassName="w-full h-full object-cover" />
                {isUploading && (
                  <div className="absolute inset-0 bg-background/80 flex items-center justify-center">
                    <span className="text-xs">Uploading...</span>
                  </div>
                )}
              </div>
              <div>
                <FormLabel className="text-base font-semibold">Company Logo</FormLabel>
                <p className="text-xs text-muted-foreground mb-2">Square image, recommended size at least 200 × 200 px. JPEG/PNG format.</p>
                <label className="cursor-pointer text-sm font-medium text-primary hover:underline">
                  Upload image
                  <input type="file" className="hidden" accept="image/jpeg,image/png,image/jpg" onChange={handleLogoUpload} disabled={isUploading} />
                </label>
              </div>
            </div>

            {/* Inputs Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="font-semibold">Company Name *</FormLabel>
                    <FormControl>
                      <Input 
                        placeholder="e.g. Acme Technology Joint Stock Company" 
                        {...field} 
                        disabled={company?.status === 'VERIFIED'} 
                      />
                    </FormControl>
                    {company?.status === 'VERIFIED' && (
                      <p className="text-[11px] text-amber-600 mt-1 flex items-start gap-1 font-medium">
                        <Info className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                        Name verified. To change, please submit an update request in the Legal Verification tab.
                      </p>
                    )}
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Sibling Fields for Trade Name and sameAsCompanyName Checkbox */}
              <div className="space-y-3">
                <div className="flex items-center justify-between h-5">
                  <span className="text-sm font-semibold">Trade Name *</span>
                  <FormField
                    control={form.control}
                    name="sameAsCompanyName"
                    render={({ field }) => (
                      <div className="flex items-center space-x-1.5">
                        <Checkbox
                          checked={field.value}
                          onCheckedChange={(checked) => {
                            field.onChange(checked);
                            if (checked) {
                              form.setValue('tradeName', form.getValues('name') || '', { shouldValidate: true });
                            }
                          }}
                        />
                        <span 
                          className="text-xs text-muted-foreground font-semibold select-none cursor-pointer"
                          onClick={() => {
                            const newVal = !field.value;
                            field.onChange(newVal);
                            if (newVal) {
                              form.setValue('tradeName', form.getValues('name') || '', { shouldValidate: true });
                            }
                          }}
                        >
                          Same as company name
                        </span>
                      </div>
                    )}
                  />
                </div>
                <FormField
                  control={form.control}
                  name="tradeName"
                  render={({ field }) => (
                    <FormItem>
                      <FormControl>
                        <Input 
                          placeholder="e.g. Acme Tech" 
                          {...field} 
                          disabled={watchSameAsCompanyName} 
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="industry"
                render={({ field }) => (
                  <FormItem className="flex flex-col justify-end">
                    <FormLabel className="font-semibold mb-2">Industry *</FormLabel>
                    <Popover>
                      <FormControl>
                        <PopoverTrigger render={
                          <Button
                            variant="outline"
                            role="combobox"
                            className={cn(
                              "justify-between w-full font-normal text-left h-10",
                              !field.value && "text-muted-foreground"
                            )}
                          >
                            <span className="truncate">
                              {field.value
                                ? COMPANY_INDUSTRIES.find((industry) => industry === field.value)
                                : "Select industry..."}
                            </span>
                            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                          </Button>
                        } />
                      </FormControl>
                      <PopoverContent className="w-[300px] p-0" align="start">
                        <Command>
                          <CommandInput placeholder="Search industry..." />
                          <CommandList>
                            <CommandEmpty>No matching industry found.</CommandEmpty>
                            <CommandGroup>
                              <ScrollArea className="h-64">
                                {COMPANY_INDUSTRIES.map((industry) => (
                                  <CommandItem
                                    value={industry}
                                    key={industry}
                                    onSelect={() => {
                                      form.setValue("industry", industry, { shouldValidate: true })
                                    }}
                                  >
                                    <Check
                                      className={cn(
                                        "mr-2 h-4 w-4",
                                        industry === field.value
                                          ? "opacity-100"
                                          : "opacity-0"
                                      )}
                                    />
                                    {industry}
                                  </CommandItem>
                                ))}
                              </ScrollArea>
                            </CommandGroup>
                          </CommandList>
                        </Command>
                      </PopoverContent>
                    </Popover>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="mainField"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="font-semibold">Main Field *</FormLabel>
                    <FormControl>
                      <Input placeholder="e.g. Software Development, IT Services..." {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="contactPhone"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="font-semibold">Contact Phone *</FormLabel>
                    <FormControl>
                      <Input placeholder="e.g. 0987654321" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="companyEmail"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="font-semibold">Company Email *</FormLabel>
                    <FormControl>
                      <Input placeholder="e.g. contact@acme.com" type="email" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="companySize"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="font-semibold">Company Size *</FormLabel>
                    <Select onValueChange={field.onChange} defaultValue={field.value} value={field.value}>
                      <FormControl>
                        <SelectTrigger className="h-10">
                          <SelectValue placeholder="Select size..." />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {COMPANY_SIZES.map(size => (
                          <SelectItem key={size} value={size}>{size} employees</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="companyType"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="font-semibold">Company Type</FormLabel>
                    <Select onValueChange={field.onChange} defaultValue={field.value} value={field.value}>
                      <FormControl>
                        <SelectTrigger className="h-10">
                          <SelectValue placeholder="Select type..." />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {COMPANY_TYPES.map(type => (
                          <SelectItem key={type} value={type}>{type}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Website Input with Toggle Checkbox */}
              <div className="col-span-1 md:col-span-2 space-y-4 p-4 bg-muted/20 border rounded-lg grid grid-cols-1 md:grid-cols-2 gap-4 items-end">
                <FormField
                  control={form.control}
                  name="noWebsite"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-center space-x-2 space-y-0 h-10">
                      <FormControl>
                        <Checkbox
                          checked={field.value}
                          onCheckedChange={(checked) => {
                            field.onChange(checked);
                            if (checked) {
                              form.setValue('website', '');
                              form.clearErrors('website');
                            }
                          }}
                        />
                      </FormControl>
                      <span 
                        className="text-xs text-muted-foreground font-semibold select-none cursor-pointer"
                        onClick={() => {
                          const newVal = !field.value;
                          field.onChange(newVal);
                          if (newVal) {
                            form.setValue('website', '');
                            form.clearErrors('website');
                          }
                        }}
                      >
                        No website
                      </span>
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="website"
                  render={({ field }) => (
                    <FormItem className="space-y-2">
                      <FormLabel className="font-semibold">Company Website</FormLabel>
                      <FormControl>
                        <Input 
                          placeholder="https://acme.com" 
                          {...field} 
                          disabled={watchNoWebsite} 
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="headquartersAddress"
                render={({ field }) => (
                  <FormItem className="col-span-1 md:col-span-2">
                    <FormLabel className="font-semibold">Headquarters Address *</FormLabel>
                    <FormControl>
                      <Input 
                        placeholder="e.g. Acme Building, 10 Street, District 1, Ho Chi Minh City" 
                        {...field} 
                        disabled={company?.status === 'VERIFIED'} 
                      />
                    </FormControl>
                    {company?.status === 'VERIFIED' && (
                      <p className="text-[11px] text-amber-600 mt-1 flex items-start gap-1 font-medium">
                        <Info className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                        Address verified. To change, please submit an update request in the Legal Verification tab.
                      </p>
                    )}
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Target Customers Checkboxes */}
              <FormField
                control={form.control}
                name="targetCustomers"
                render={({ field }) => (
                  <FormItem className="p-4 border rounded-lg bg-muted/15">
                    <FormLabel className="text-sm font-semibold">Target Customers</FormLabel>
                    <div className="grid grid-cols-2 gap-3 mt-3">
                      {TARGET_CUSTOMERS_OPTIONS.map((option) => {
                        const value = field.value || [];
                        return (
                          <div key={option} className="flex flex-row items-center space-x-2 space-y-0">
                            <Checkbox
                              checked={value.includes(option)}
                              onCheckedChange={(checked) => {
                                const newValue = checked
                                  ? [...value, option]
                                  : value.filter((val: string) => val !== option);
                                field.onChange(newValue);
                              }}
                            />
                            <span 
                              className="text-xs font-medium cursor-pointer select-none" 
                              onClick={() => {
                                const checked = value.includes(option);
                                const newValue = checked
                                  ? value.filter((val: string) => val !== option)
                                  : [...value, option];
                                field.onChange(newValue);
                              }}
                            >
                              {option}
                            </span>
                          </div>
                        );
                      })}
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Operating Markets Checkboxes */}
              <FormField
                control={form.control}
                name="operatingMarkets"
                render={({ field }) => (
                  <FormItem className="p-4 border rounded-lg bg-muted/15">
                    <FormLabel className="text-sm font-semibold">Operating Markets</FormLabel>
                    <div className="grid grid-cols-2 gap-3 mt-3">
                      {OPERATING_MARKETS_OPTIONS.map((option) => {
                        const value = field.value || [];
                        return (
                          <div key={option} className="flex flex-row items-center space-x-2 space-y-0">
                            <Checkbox
                              checked={value.includes(option)}
                              onCheckedChange={(checked) => {
                                const newValue = checked
                                  ? [...value, option]
                                  : value.filter((val: string) => val !== option);
                                field.onChange(newValue);
                              }}
                            />
                            <span 
                              className="text-xs font-medium cursor-pointer select-none" 
                              onClick={() => {
                                const checked = value.includes(option);
                                const newValue = checked
                                  ? value.filter((val: string) => val !== option)
                                  : [...value, option];
                                field.onChange(newValue);
                              }}
                            >
                              {option}
                            </span>
                          </div>
                        );
                      })}
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="employeeBenefits"
                render={({ field }) => (
                  <FormItem className="col-span-1 md:col-span-2">
                    <FormLabel className="font-semibold">Employee Benefits</FormLabel>
                    <FormControl>
                      <Textarea 
                        placeholder="e.g. Premium Health Insurance, Free Lunch, 13th Month Salary..." 
                        className="min-h-[100px]" 
                        {...field} 
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Images upload Section */}
              <div className="space-y-3 col-span-1 md:col-span-2 border p-4 rounded-lg bg-muted/20">
                <FormLabel className="text-sm font-semibold">Company Gallery (Max 5 images)</FormLabel>
                <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-4 mt-2">
                  {currentImages.map((imgUrl, idx) => (
                    <div key={idx} className="relative aspect-video rounded-lg border overflow-hidden bg-muted group shadow-sm">
                      <img src={imgUrl} alt={`Company Image ${idx + 1}`} className="w-full h-full object-cover" />
                      <button
                        type="button"
                        onClick={() => removeImage(idx)}
                        className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 flex items-center justify-center text-white transition-opacity duration-200 text-xs font-semibold cursor-pointer"
                      >
                        Remove
                      </button>
                    </div>
                  ))}
                  {currentImages.length < 5 && (
                    <label className="relative aspect-video rounded-lg border-2 border-dashed border-muted-foreground/30 hover:border-primary flex flex-col items-center justify-center bg-background/50 hover:bg-muted/10 cursor-pointer transition-colors shadow-sm min-h-[70px]">
                      <span className="text-xs font-semibold text-muted-foreground">Add image</span>
                      {isUploadingImages && (
                        <span className="text-[10px] text-primary animate-pulse font-medium">Uploading...</span>
                      )}
                      <input
                        type="file"
                        multiple
                        className="hidden"
                        accept="image/jpeg,image/png,image/jpg"
                        onChange={handleImagesUpload}
                        disabled={isUploadingImages}
                      />
                    </label>
                  )}
                </div>
              </div>

              <FormField
                control={form.control}
                name="description"
                render={({ field }) => (
                  <FormItem className="col-span-1 md:col-span-2">
                    <FormLabel className="font-semibold">Company Description *</FormLabel>
                    <FormControl>
                      <Textarea 
                        placeholder="Share your mission, culture, product direction, and what makes your company a great place to work..." 
                        className="min-h-[220px]" 
                        {...field} 
                      />
                    </FormControl>
                    <div className="flex justify-between items-center text-xs text-muted-foreground mt-2 font-medium">
                      <span>Required minimum 500 characters</span>
                      <span className={descriptionLength < 500 ? 'text-destructive font-bold' : 'text-green-600 font-bold'}>
                        {descriptionLength} / 500 characters
                      </span>
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />

            </div>

            <div className="flex items-center justify-between pt-6 border-t gap-4">
              <p className="text-xs text-muted-foreground font-medium">Note: Fields marked with * are required.</p>
              <div className="flex items-center gap-3">
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={() => router.push('/recruiter/company')}
                  className="cursor-pointer"
                >
                  Back
                </Button>
                <Button type="submit" disabled={isCreating || isUpdating || isUploading || isUploadingImages} className="cursor-pointer">
                  {isCreating || isUpdating ? 'Saving...' : 'Save Company Profile'}
                </Button>
              </div>
            </div>
          </form>
        </Form>
      </div>

      {/* Side Column: Tips */}
      <div className="space-y-6">
        <div className="bg-card rounded-xl border p-5 shadow-sm self-start">
           <h3 className="font-bold text-sm mb-3 flex items-center gap-1.5 text-primary">
             <Info className="w-4 h-4" /> Profile Completion Tips
           </h3>
           <ul className="text-xs text-muted-foreground space-y-2.5 list-disc list-inside leading-relaxed">
             <li><strong>Upload high-quality logo:</strong> Increases trust and brand recognition with candidates.</li>
             <li><strong>Detailed description &ge; 500 chars:</strong> Clearly introduce the working environment and benefits to attract talent.</li>
             <li><strong>Provide real images:</strong> Office photos and team building activities help candidates visualize the workplace.</li>
             <li><strong>Complete contact info:</strong> Helps partners and candidates connect easily.</li>
           </ul>
        </div>
      </div>
    </div>
  );
}
