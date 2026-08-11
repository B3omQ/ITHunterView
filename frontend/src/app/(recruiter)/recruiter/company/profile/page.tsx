'use client';

import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';
import { Check, ChevronsUpDown, Info } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useTranslations } from 'next-intl';

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
import { LocationPicker } from '@/components/forms/LocationPicker';
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

const COMPANY_SIZES = ['1-10', '11-50', '51-200', '201-500', '500+'];
const TARGET_CUSTOMERS_OPTIONS = ['B2B', 'B2C', 'B2G', 'Other'];
const OPERATING_MARKETS_OPTIONS = ['Domestic', 'Asia', 'Europe', 'Americas', 'Australia', 'Other'];

type ProfileFormValues = {
  name: string;
  sameAsCompanyName?: boolean;
  tradeName: string;
  contactPhone: string;
  industry: string;
  mainField: string;
  companyEmail: string;
  companySize: string;
  provinceCode: string;
  detailedLocation: string;
  latitude: number;
  longitude: number;
  description: string;
  noWebsite?: boolean;
  website?: string;
  logoUrl?: string;
  companyType?: string;
  employeeBenefits?: string;
  targetCustomers: string[];
  operatingMarkets: string[];
  companyImages: string[];
};

export default function CompanyProfilePage() {
  const router = useRouter();
  const t = useTranslations('RecruiterCompanyProfile');
  
  const profileSchema = React.useMemo(() => {
    return z.object({
      name: z.string().min(1, t('zodName')),
      sameAsCompanyName: z.boolean().optional(),
      tradeName: z.string().min(1, t('zodTradeName')),
      contactPhone: z.string().min(1, t('zodPhone')),
      industry: z.string().min(1, t('zodIndustry')),
      mainField: z.string().min(1, t('zodMainField')),
      companyEmail: z.string().min(1, t('zodEmail')).email(t('zodEmailInvalid')),
      companySize: z.string().min(1, t('zodSize')),
      provinceCode: z.string().min(1, t('zodProvince')),
      detailedLocation: z.string().min(1, t('zodLocation')),
      latitude: z.number(),
      longitude: z.number(),
      description: z.string().min(500, t('zodDesc')),
      noWebsite: z.boolean().optional(),
      website: z.string().optional(),
      logoUrl: z.string().optional(),
      companyType: z.string().optional(),
      employeeBenefits: z.string().optional(),
      targetCustomers: z.array(z.string()).default([]),
      operatingMarkets: z.array(z.string()).default([]),
      companyImages: z.array(z.string()).default([]),
    }).refine((data) => {
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
      message: t('zodWebsite'),
      path: ['website'],
    });
  }, [t]);

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
      provinceCode: '',
      detailedLocation: '',
      latitude: 21.028511,
      longitude: 105.804817,
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
        provinceCode: company.provinceCode || '',
        detailedLocation: company.detailedLocation || company.headquartersAddress || '',
        latitude: company.latitude || 21.028511,
        longitude: company.longitude || 105.804817,
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
      toast.error(t('uploadLogoError'));
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      toast.error(t('uploadLogoSizeError'));
      return;
    }

    try {
      setIsUploading(true);
      const res = await uploadService.uploadFile(file, 'company_logos');
      form.setValue('logoUrl', res.data || '', { shouldValidate: true });
      toast.success(t('uploadLogoSuccess'));
    } catch (error) {
      toast.error(t('uploadLogoFail'));
    } finally {
      setIsUploading(false);
    }
  };

  const handleImagesUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    const currentImages = form.getValues('companyImages') || [];
    if (currentImages.length + files.length > 5) {
      toast.error(t('uploadImagesLimitError'));
      return;
    }

    try {
      setIsUploadingImages(true);
      const newUrls = [...currentImages];
      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (!file.type.startsWith('image/')) {
          toast.error(t('uploadImagesTypeError', { name: file.name }));
          continue;
        }
        if (file.size > 5 * 1024 * 1024) {
          toast.error(t('uploadImagesSizeError', { name: file.name }));
          continue;
        }
        const res = await uploadService.uploadFile(file, 'company_images');
        if (res.data) {
          newUrls.push(res.data);
        }
      }
      form.setValue('companyImages', newUrls, { shouldValidate: true });
      toast.success(t('uploadImagesSuccess'));
    } catch (error) {
      toast.error(t('uploadImagesFail'));
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
        headquartersAddress: values.detailedLocation,
      };

      if (company?.id) {
        await updateProfile({ id: company.id, dto: payload });
      } else {
        await createProfile(payload);
      }
      toast.success(t('saveSuccess'));
      router.push('/recruiter/company');
    } catch (error) {
      toast.error(t('saveFail'));
    }
  };

  if (isFetchingCompany) {
    return <div className="p-8 text-center text-muted-foreground">{t('loadingProfile')}</div>;
  }

  const descriptionLength = form.watch('description')?.length || 0;
  const currentLogo = form.watch('logoUrl');
  const currentImages = form.watch('companyImages') || [];

  return (
    <div className="w-full pb-8 grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-8">
      <div className="bg-card rounded-xl border p-6 shadow-sm">
        <h2 className="text-xl font-semibold mb-6">{t('pageTitle')}</h2>
        
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
                <FormLabel className="text-base font-semibold">{t('logoLabel')}</FormLabel>
                <p className="text-xs text-muted-foreground mb-2">{t('logoHint')}</p>
                <label className="cursor-pointer text-sm font-medium text-primary hover:underline">
                  {t('uploadImage')}
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
                    <FormLabel className="font-semibold">{t('companyName')}</FormLabel>
                    <FormControl>
                      <Input 
                        placeholder={t('companyNamePlaceholder')} 
                        {...field} 
                        disabled={company?.status === 'VERIFIED'} 
                      />
                    </FormControl>
                    {company?.status === 'VERIFIED' && (
                      <p className="text-[11px] text-amber-600 mt-1 flex items-start gap-1 font-medium">
                        <Info className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                        {t('nameVerified')}
                      </p>
                    )}
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Sibling Fields for Trade Name and sameAsCompanyName Checkbox */}
              <div className="space-y-3">
                <div className="flex items-center justify-between h-5">
                  <span className="text-sm font-semibold">{t('tradeName')}</span>
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
                          {t('sameAsCompany')}
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
                          placeholder={t('tradeNamePlaceholder')} 
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
                    <FormLabel className="font-semibold mb-2">{t('industry')}</FormLabel>
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
                                : t('selectIndustry')}
                            </span>
                            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                          </Button>
                        } />
                      </FormControl>
                      <PopoverContent className="w-[300px] p-0" align="start">
                        <Command>
                          <CommandInput placeholder={t('searchIndustry')} />
                          <CommandList>
                            <CommandEmpty>{t('noIndustryFound')}</CommandEmpty>
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
                    <FormLabel className="font-semibold">{t('mainField')}</FormLabel>
                    <FormControl>
                      <Input placeholder={t('mainFieldPlaceholder')} {...field} />
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
                    <FormLabel className="font-semibold">{t('contactPhone')}</FormLabel>
                    <FormControl>
                      <Input placeholder={t('contactPhonePlaceholder')} {...field} />
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
                    <FormLabel className="font-semibold">{t('companyEmail')}</FormLabel>
                    <FormControl>
                      <Input placeholder={t('companyEmailPlaceholder')} type="email" {...field} />
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
                    <FormLabel className="font-semibold">{t('companySize')}</FormLabel>
                    <Select onValueChange={field.onChange} defaultValue={field.value} value={field.value}>
                      <FormControl>
                        <SelectTrigger className="h-10">
                          <SelectValue placeholder={t('selectSize')} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {COMPANY_SIZES.map(size => (
                          <SelectItem key={size} value={size}>{t('employees', { size })}</SelectItem>
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
                    <FormLabel className="font-semibold">{t('companyType')}</FormLabel>
                    <Select onValueChange={field.onChange} defaultValue={field.value} value={field.value}>
                      <FormControl>
                        <SelectTrigger className="h-10">
                          <SelectValue placeholder={t('selectType')} />
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
                        {t('noWebsite')}
                      </span>
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="website"
                  render={({ field }) => (
                    <FormItem className="space-y-2">
                      <FormLabel className="font-semibold">{t('website')}</FormLabel>
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

              <div className="col-span-1 md:col-span-2">
                <FormLabel className="font-semibold mb-2 block">{t('hqAddress')}</FormLabel>
                <LocationPicker
                  disabled={company?.status === 'VERIFIED'}
                  value={{
                    provinceCode: form.watch('provinceCode'),
                    detailedLocation: form.watch('detailedLocation'),
                    latitude: form.watch('latitude'),
                    longitude: form.watch('longitude'),
                  }}
                  onChange={(val) => {
                    form.setValue('provinceCode', val.provinceCode, { shouldValidate: true });
                    form.setValue('detailedLocation', val.detailedLocation, { shouldValidate: true });
                    form.setValue('latitude', val.latitude, { shouldValidate: true });
                    form.setValue('longitude', val.longitude, { shouldValidate: true });
                  }}
                />
                {(form.formState.errors.detailedLocation || form.formState.errors.provinceCode) && (
                  <p className="text-[0.8rem] font-medium text-destructive mt-2">
                    {t('locationError')}
                  </p>
                )}
                {company?.status === 'VERIFIED' && (
                  <p className="text-[11px] text-amber-600 mt-1 flex items-start gap-1 font-medium">
                    <Info className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                    {t('addressVerified')}
                  </p>
                )}
              </div>

              {/* Target Customers Checkboxes */}
              <FormField
                control={form.control}
                name="targetCustomers"
                render={({ field }) => (
                  <FormItem className="p-4 border rounded-lg bg-muted/15">
                    <FormLabel className="text-sm font-semibold">{t('targetCustomers')}</FormLabel>
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
                    <FormLabel className="text-sm font-semibold">{t('operatingMarkets')}</FormLabel>
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
                    <FormLabel className="font-semibold">{t('employeeBenefits')}</FormLabel>
                    <FormControl>
                      <Textarea 
                        placeholder={t('employeeBenefitsPlaceholder')} 
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
                <FormLabel className="text-sm font-semibold">{t('companyGallery')}</FormLabel>
                <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-4 mt-2">
                  {currentImages.map((imgUrl, idx) => (
                    <div key={idx} className="relative aspect-video rounded-lg border overflow-hidden bg-muted group shadow-sm">
                      <img src={imgUrl} alt={`Company Image ${idx + 1}`} className="w-full h-full object-cover" />
                      <button
                        type="button"
                        onClick={() => removeImage(idx)}
                        className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 flex items-center justify-center text-white transition-opacity duration-200 text-xs font-semibold cursor-pointer"
                      >
                        {t('remove')}
                      </button>
                    </div>
                  ))}
                  {currentImages.length < 5 && (
                    <label className="relative aspect-video rounded-lg border-2 border-dashed border-muted-foreground/30 hover:border-primary flex flex-col items-center justify-center bg-background/50 hover:bg-muted/10 cursor-pointer transition-colors shadow-sm min-h-[70px]">
                      <span className="text-xs font-semibold text-muted-foreground">{t('addImage')}</span>
                      {isUploadingImages && (
                        <span className="text-[10px] text-primary animate-pulse font-medium">{t('uploading')}</span>
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
                    <FormLabel className="font-semibold">{t('companyDesc')}</FormLabel>
                    <FormControl>
                      <Textarea 
                        placeholder={t('companyDescPlaceholder')} 
                        className="min-h-[220px]" 
                        {...field} 
                      />
                    </FormControl>
                    <div className="flex justify-between items-center text-xs text-muted-foreground mt-2 font-medium">
                      <span>{t('reqMinChars')}</span>
                      <span className={descriptionLength < 500 ? 'text-destructive font-bold' : 'text-green-600 font-bold'}>
                        {t('charCount', { current: descriptionLength })}
                      </span>
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />

            </div>

            <div className="flex items-center justify-between pt-6 border-t gap-4">
              <p className="text-xs text-muted-foreground font-medium">{t('requiredNote')}</p>
              <div className="flex items-center gap-3">
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={() => router.push('/recruiter/company')}
                  className="cursor-pointer"
                >
                  {t('back')}
                </Button>
                <Button type="submit" disabled={isCreating || isUpdating || isUploading || isUploadingImages} className="cursor-pointer">
                  {isCreating || isUpdating ? t('saving') : t('saveProfile')}
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
             <Info className="w-4 h-4" /> {t('tipsTitle')}
           </h3>
           <ul className="text-xs text-muted-foreground space-y-2.5 list-disc list-inside leading-relaxed">
             <li><strong>{t('tip1')}</strong> {t('tip1Desc')}</li>
             <li><strong>{t('tip2')}</strong> {t('tip2Desc')}</li>
             <li><strong>{t('tip3')}</strong> {t('tip3Desc')}</li>
             <li><strong>{t('tip4')}</strong> {t('tip4Desc')}</li>
           </ul>
        </div>
      </div>
    </div>
  );
}
