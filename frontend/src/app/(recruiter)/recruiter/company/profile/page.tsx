'use client';

import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';

import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

import { useGetMyCompany, useCreateOrUpdateProfile } from '@/hooks/useCompany';
import { uploadService } from '@/services/upload.service';
import { CompanyLogo } from '@/components/shared/CompanyLogo';

const profileSchema = z.object({
  name: z.string().min(2, 'Company name must be at least 2 characters'),
  industry: z.string().min(1, 'Please select an industry'),
  companySize: z.string().min(1, 'Please select company size'),
  website: z.string().url('Please enter a valid URL').or(z.literal('')),
  description: z.string().min(100, 'Minimum 100 characters recommended'),
  logoUrl: z.string().optional(),
});

type ProfileFormValues = z.infer<typeof profileSchema>;

const INDUSTRIES = [
  'Information Technology',
  'Software Development',
  'Fintech',
  'E-commerce',
  'Artificial Intelligence',
  'Cybersecurity',
  'Telecommunications',
  'Other',
];

const COMPANY_SIZES = ['1-10', '11-50', '51-200', '201-500', '500+'];

export default function CompanyProfilePage() {
  const router = useRouter();
  const { data: company, isLoading: isFetchingCompany } = useGetMyCompany();
  const { mutateAsync: updateProfile, isPending: isUpdating } = useCreateOrUpdateProfile();
  
  const [isUploading, setIsUploading] = useState(false);

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      name: '',
      industry: '',
      companySize: '',
      website: '',
      description: '',
      logoUrl: '',
    },
  });

  useEffect(() => {
    if (company) {
      form.reset({
        name: company.name || '',
        industry: company.industry || '',
        companySize: company.companySize || '',
        website: company.website || '',
        description: company.description || '',
        logoUrl: company.logoUrl || '',
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
      toast.error('File size must be less than 5MB');
      return;
    }

    try {
      setIsUploading(true);
      const res = await uploadService.uploadFile(file, 'company_logos');
      form.setValue('logoUrl', res.data || '', { shouldValidate: true });
      toast.success('Logo uploaded successfully');
    } catch (error) {
      toast.error('Failed to upload logo');
    } finally {
      setIsUploading(false);
    }
  };

  const onSubmit = async (values: ProfileFormValues) => {
    try {
      await updateProfile(values);
      toast.success('Company profile saved successfully!');
      router.push('/recruiter/company');
    } catch (error) {
      toast.error('Failed to save company profile');
    }
  };

  if (isFetchingCompany) {
    return <div className="p-8 text-center text-muted-foreground">Loading profile...</div>;
  }

  const descriptionLength = form.watch('description')?.length || 0;
  const currentLogo = form.watch('logoUrl');

  return (
    <div className="grid md:grid-cols-[minmax(0,1fr)_400px] gap-8 w-full">
      <div className="bg-card rounded-xl border p-6">
        <h2 className="text-xl font-semibold mb-6">Company Details</h2>
        
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <div className="flex items-center gap-6">
              <div className="relative w-24 h-24 border rounded-lg overflow-hidden bg-muted flex items-center justify-center shrink-0">
                <CompanyLogo src={currentLogo} alt="Company Logo" fallbackType="building" fallbackIconClassName="w-10 h-10 text-muted-foreground" imageClassName="w-full h-full object-cover" />
                {isUploading && (
                  <div className="absolute inset-0 bg-background/80 flex items-center justify-center">
                    <span className="text-xs">Uploading...</span>
                  </div>
                )}
              </div>
              <div>
                <FormLabel>Company Logo</FormLabel>
                <p className="text-sm text-muted-foreground mb-2">Square image, min 200 × 200 px</p>
                <label className="cursor-pointer text-sm font-medium text-primary hover:underline">
                  Upload Image
                  <input type="file" className="hidden" accept="image/jpeg,image/png,image/jpg" onChange={handleLogoUpload} disabled={isUploading} />
                </label>
              </div>
            </div>

            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Company Name *</FormLabel>
                  <FormControl>
                    <Input 
                      placeholder="e.g. Acme Technologies Inc." 
                      {...field} 
                      disabled={company?.status === 'VERIFIED'} 
                    />
                  </FormControl>
                  {company?.status === 'VERIFIED' && (
                    <p className="text-xs text-amber-600 mt-1 font-medium">
                      Company name has been verified. To change it, please submit an update request in the Legal tab.
                    </p>
                  )}
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="industry"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Industry *</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select industry..." />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {INDUSTRIES.map(ind => (
                        <SelectItem key={ind} value={ind}>{ind}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="companySize"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Company Size *</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value} value={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select company size..." />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {COMPANY_SIZES.map(size => (
                        <SelectItem key={size} value={size}>{size}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="website"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Website URL</FormLabel>
                  <FormControl>
                    <Input placeholder="https://yourcompany.com" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Company Description *</FormLabel>
                  <FormControl>
                    <Textarea 
                      placeholder="Describe your company's mission, culture, products, and what makes you a great place to work..." 
                      className="min-h-[150px]" 
                      {...field} 
                    />
                  </FormControl>
                  <div className="flex justify-between items-center text-xs text-muted-foreground mt-2">
                    <span>Minimum 100 characters recommended</span>
                    <span className={descriptionLength < 100 ? 'text-destructive' : 'text-green-600'}>
                      {descriptionLength} characters
                    </span>
                  </div>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="flex items-center justify-between pt-6 border-t">
              <p className="text-sm text-muted-foreground">All changes are saved automatically when you click Save.</p>
              <div className="flex items-center gap-3">
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={() => router.push('/recruiter/company')}
                  className="cursor-pointer"
                >
                  Return
                </Button>
                <Button type="submit" disabled={isUpdating || isUploading} className="cursor-pointer">
                  {isUpdating ? 'Saving...' : 'Save Company Profile'}
                </Button>
              </div>
            </div>
          </form>
        </Form>
      </div>

      <div className="bg-card rounded-xl border p-6 self-start">
         <h3 className="font-semibold mb-2">Profile Completion Tips</h3>
         <ul className="text-sm text-muted-foreground space-y-2 list-disc list-inside">
           <li>Upload a high-quality logo to build trust.</li>
           <li>Write a compelling description that highlights your culture.</li>
           <li>Add your official website so candidates can learn more.</li>
         </ul>
      </div>
    </div>
  );
}
