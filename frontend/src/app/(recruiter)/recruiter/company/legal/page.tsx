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
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';

import { useGetMyCompany, useVerifyCompanyLegal, useSubmitCompanyUpdateRequest } from '@/hooks/useCompany';
import { uploadService } from '@/services/upload.service';

const legalSchema = z.object({
  taxCode: z.string().min(1, 'Tax ID is required'),
  companyName: z.string().min(1, 'Company name is required'),
  headquartersAddress: z.string().min(1, 'Headquarters address is required'),
  verificationMethod: z.enum(['BUSINESS_REGISTRATION', 'POA_AND_ID'], {
    message: 'Please select a verification method',
  }),
  verificationDocumentUrl: z.string().min(1, 'Please upload a verification document'),
});

type LegalFormValues = z.infer<typeof legalSchema>;

export default function LegalVerificationPage() {
  const router = useRouter();
  const { data: company, isLoading: isFetchingCompany } = useGetMyCompany();
  const { mutateAsync: verifyLegal, isPending: isSubmitting } = useVerifyCompanyLegal();
  const { mutateAsync: submitUpdateRequest, isPending: isUpdatingRequest } = useSubmitCompanyUpdateRequest();
  
  const [isUploading, setIsUploading] = useState(false);
  const [isEditingVerified, setIsEditingVerified] = useState(false);

  const form = useForm<LegalFormValues>({
    resolver: zodResolver(legalSchema),
    defaultValues: {
      taxCode: '',
      companyName: '',
      headquartersAddress: '',
      verificationMethod: 'BUSINESS_REGISTRATION',
      verificationDocumentUrl: '',
    },
  });

  useEffect(() => {
    if (!isFetchingCompany && !company) {
      toast.error('Please complete your Company Profile first.');
      router.push('/recruiter/company/profile');
    } else if (company) {
      form.reset({
        taxCode: company.hasPendingChange ? (company.pendingTaxCode || '') : (company.taxCode || ''),
        companyName: company.hasPendingChange ? (company.pendingName || '') : (company.name || ''),
        headquartersAddress: company.hasPendingChange ? (company.pendingHeadquartersAddress || '') : (company.headquartersAddress || ''),
        verificationMethod: company.hasPendingChange ? (company.pendingVerificationMethod || 'BUSINESS_REGISTRATION') : (company.verificationMethod || 'BUSINESS_REGISTRATION'),
        verificationDocumentUrl: company.hasPendingChange ? (company.pendingVerificationDocumentUrl || '') : (company.verificationDocumentUrl || ''),
      });
    }
  }, [company, isFetchingCompany, form, router]);

  const isReadOnly = 
    company?.status === 'PENDING' || 
    company?.hasPendingChange || 
    (company?.status === 'VERIFIED' && !isEditingVerified);

  const handleDocumentUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'];
    if (!validTypes.includes(file.type)) {
      toast.error('Please upload jpeg, jpg, png, or pdf');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      toast.error('Maximum file size is 5MB');
      return;
    }

    try {
      setIsUploading(true);
      const res = await uploadService.uploadFile(file, 'legal_documents');
      form.setValue('verificationDocumentUrl', res.data || '', { shouldValidate: true });
      toast.success('Document uploaded successfully');
    } catch (error) {
      toast.error('Failed to upload document');
    } finally {
      setIsUploading(false);
    }
  };

  const onSubmit = async (values: LegalFormValues) => {
    if (!company) return;
    try {
      if (company.status === 'VERIFIED') {
        await submitUpdateRequest({
          id: company.id,
          dto: {
            verificationMethod: values.verificationMethod,
            verificationDocumentUrl: values.verificationDocumentUrl,
            taxCode: values.taxCode,
            companyName: values.companyName,
            headquartersAddress: values.headquartersAddress,
          }
        });
        toast.success('Legal verification update request submitted successfully!');
        setIsEditingVerified(false);
      } else {
        await verifyLegal({
          id: company.id,
          dto: {
            verificationMethod: values.verificationMethod,
            verificationDocumentUrl: values.verificationDocumentUrl,
            taxCode: values.taxCode,
            companyName: values.companyName,
            headquartersAddress: values.headquartersAddress,
          }
        });
        toast.success('Legal verification submitted successfully!');
      }
    } catch (error) {
      toast.error('Failed to submit legal verification');
    }
  };

  if (isFetchingCompany) {
    return <div className="p-8 text-center text-muted-foreground">Loading legal profile...</div>;
  }

  if (!company) return null;

  const currentDoc = form.watch('verificationDocumentUrl');

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-xl font-bold">Legal Verification</h2>
        <p className="text-muted-foreground">Please complete your company details and upload matching corporate documents for verification.</p>
      </div>

      {company.hasPendingChange && (
        <div className="mb-6 bg-blue-50 border border-blue-200 rounded-xl p-4 text-sm text-blue-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">ℹ Verification Update Request Pending</p>
          <p className="text-blue-700/80 text-xs">
            Your request to update company details is pending review by our staff.
          </p>
        </div>
      )}

      {company.status === 'PENDING' && !company.hasPendingChange && (
        <div className="mb-6 bg-yellow-50 border border-yellow-200 rounded-xl p-4 text-sm text-yellow-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">⏳ Verification Pending</p>
          <p className="text-yellow-700/80 text-xs">
            Your company verification is pending review by our staff.
          </p>
        </div>
      )}

      {company.status === 'VERIFIED' && !company.hasPendingChange && (
        <div className="mb-6 bg-green-50 border border-green-200 rounded-xl p-4 text-sm text-green-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">✅ Verified</p>
          <p className="text-green-700/80 text-xs">
            Your company has been verified. To update details, click the "Request Update" button below.
          </p>
        </div>
      )}

      {company.status === 'REJECTED' && (
        <div className="mb-6 bg-red-50 border border-red-200 rounded-xl p-4 text-sm text-red-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">❌ Verification Rejected</p>
          <p className="text-red-700/80 text-xs">
            Your verification request was rejected. Reason: <strong>{company.rejectReason || 'No reason specified'}</strong>. Please update your details and resubmit.
          </p>
        </div>
      )}

      {company.status === 'VERIFIED' && !company.hasPendingChange && company.rejectReason && (
        <div className="mb-6 bg-red-50 border border-red-200 rounded-xl p-4 text-sm text-red-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">❌ Update Request Rejected</p>
          <p className="text-red-700/80 text-xs">
            Your previous request to update company details was rejected. Reason: <strong>{company.rejectReason}</strong>. You can request changes again below.
          </p>
        </div>
      )}

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="grid md:grid-cols-2 gap-8">
          
          {/* Left Column: Details */}
          <div className="bg-card rounded-xl border p-6 space-y-6">
            <h3 className="font-semibold text-lg border-b pb-2">Company Details</h3>
            
            <FormField
              control={form.control}
              name="taxCode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Tax ID / Registration Number *</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. 0102345678" {...field} disabled={isReadOnly} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="companyName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Company Name *</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Acme Technologies Joint Stock Company" {...field} disabled={isReadOnly} />
                  </FormControl>
                  <p className="text-xs text-muted-foreground">
                    {company.status === 'VERIFIED' ? 'Modify to request name update' : 'Synced from Company Profile'}
                  </p>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="headquartersAddress"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Headquarters Address *</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Acme Tower, District 1, Ho Chi Minh City" {...field} disabled={isReadOnly} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          {/* Right Column: Documents */}
          <div className="space-y-6">
            <div className="bg-card rounded-xl border p-6 space-y-6">
              <h3 className="font-semibold text-lg border-b pb-2">Attached Documents</h3>
              <p className="text-sm text-muted-foreground">Please select your preferred verification method. View the submission guide here.</p>

              <FormField
                control={form.control}
                name="verificationMethod"
                render={({ field }) => (
                  <FormItem className="space-y-3">
                    <FormControl>
                      <RadioGroup
                        onValueChange={field.onChange}
                        defaultValue={field.value}
                        value={field.value}
                        disabled={isReadOnly}
                        className="flex flex-col space-y-1 bg-muted/50 p-4 rounded-lg"
                      >
                        <FormItem className="flex items-center space-x-3 space-y-0">
                          <FormControl>
                            <RadioGroupItem value="BUSINESS_REGISTRATION" />
                          </FormControl>
                          <FormLabel className="font-normal">
                            Business Registration Certificate or equivalent document
                          </FormLabel>
                        </FormItem>
                        <FormItem className="flex items-center space-x-3 space-y-0">
                          <FormControl>
                            <RadioGroupItem value="POA_AND_ID" />
                          </FormControl>
                          <FormLabel className="font-normal">
                            Power of Attorney (POA) and Identity Document
                          </FormLabel>
                        </FormItem>
                      </RadioGroup>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="space-y-2">
                <FormLabel>Business Certificate *</FormLabel>
                <div className="border-2 border-dashed rounded-lg p-6 flex flex-col items-center justify-center text-center bg-muted/20">
                  {currentDoc ? (
                    <div className="space-y-2">
                      <p className="text-sm font-medium text-primary">Document Uploaded Successfully</p>
                      <a href={currentDoc} target="_blank" rel="noreferrer" className="text-xs text-blue-500 hover:underline">View Document</a>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <p className="text-sm font-medium">Click or drag file to this area to upload</p>
                      <p className="text-xs text-muted-foreground">Maximum file size 5MB. Supported formats: jpeg, jpg, png, pdf</p>
                    </div>
                  )}
                  
                  {!isReadOnly && (
                    <div className="mt-4">
                      <Button type="button" variant="outline" size="sm" className="relative" disabled={isUploading}>
                        {isUploading ? 'Uploading...' : (currentDoc ? 'Replace File' : 'Upload File')}
                        <input 
                          type="file" 
                          className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" 
                          accept=".jpg,.jpeg,.png,.pdf" 
                          onChange={handleDocumentUpload} 
                          disabled={isUploading || isReadOnly}
                        />
                      </Button>
                    </div>
                  )}
                </div>
                {form.formState.errors.verificationDocumentUrl && (
                  <p className="text-[0.8rem] font-medium text-destructive">{form.formState.errors.verificationDocumentUrl.message}</p>
                )}
              </div>

              <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 text-sm text-amber-800">
                <p className="font-semibold mb-2">⚠ Document Guidelines:</p>
                <ul className="list-disc list-inside space-y-1 text-amber-700/80 text-xs">
                  <li>The document must be fully visible with no signs of editing, cropping, or blurred information.</li>
                  <li>Ensure that the business information submitted perfectly matches the records on the official tax authority portal.</li>
                </ul>
              </div>
            </div>
          </div>

          <div className="md:col-span-2 flex items-center justify-end pt-6 border-t gap-3 mt-4">
            {company.status === 'VERIFIED' && !company.hasPendingChange ? (
              !isEditingVerified ? (
                <Button 
                  type="button" 
                  onClick={() => setIsEditingVerified(true)}
                  className="bg-primary text-primary-foreground hover:bg-primary/95 cursor-pointer"
                >
                  Request Update
                </Button>
              ) : (
                <>
                  <Button 
                    type="button" 
                    variant="outline" 
                    onClick={() => {
                      setIsEditingVerified(false);
                      form.reset();
                    }}
                    disabled={isSubmitting || isUpdatingRequest || isUploading}
                    className="cursor-pointer"
                  >
                    Cancel
                  </Button>
                  <Button 
                    type="submit" 
                    disabled={isSubmitting || isUpdatingRequest || isUploading}
                    className="cursor-pointer"
                  >
                    {isUpdatingRequest ? 'Submitting...' : 'Submit Update Request'}
                  </Button>
                </>
              )
            ) : (
              <>
                <Button 
                  type="button" 
                  variant="outline"
                  onClick={() => form.reset()}
                  disabled={isReadOnly}
                  className="cursor-pointer"
                >
                  Reset
                </Button>
                <Button 
                  type="submit" 
                  disabled={isSubmitting || isUploading || isReadOnly}
                  className="cursor-pointer"
                >
                  {isSubmitting ? 'Saving...' : 'Save Legal Profile'}
                </Button>
              </>
            )}
          </div>
        </form>
      </Form>
    </div>
  );
}
