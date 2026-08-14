'use client';

import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';
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
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';

import { useGetMyCompany, useVerifyCompanyLegal, useSubmitCompanyUpdateRequest } from '@/hooks/useCompany';
import { uploadService } from '@/services/upload.service';

export default function LegalVerificationPage() {
  const router = useRouter();
  const t = useTranslations('RecruiterCompanyLegal');

  const legalSchema = React.useMemo(() => {
    return z.object({
      taxCode: z.string().min(1, t('zodTaxCode')),
      companyName: z.string().min(1, t('zodCompanyName')),
      headquartersAddress: z.string().min(1, t('zodHqAddress')),
      verificationMethod: z.enum(['BUSINESS_REGISTRATION', 'POA_AND_ID'], {
        message: t('zodVerificationMethod'),
      }),
      verificationDocumentUrl: z.string().min(1, t('zodVerificationDoc')),
    });
  }, [t]);

  type LegalFormValues = z.infer<typeof legalSchema>;
  const { data: company, isLoading: isFetchingCompany } = useGetMyCompany();
  const { mutateAsync: verifyLegal, isPending: isSubmitting } = useVerifyCompanyLegal();
  const { mutateAsync: submitUpdateRequest, isPending: isUpdatingRequest } = useSubmitCompanyUpdateRequest();
  
  const [isUploading, setIsUploading] = useState(false);
  const [isEditingVerified, setIsEditingVerified] = useState(false);

  const form = useForm<LegalFormValues>({
    resolver: zodResolver(legalSchema) as any,
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
      toast.error(t('profileRequired'));
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
      toast.error(t('uploadTypeError'));
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      toast.error(t('uploadSizeError'));
      return;
    }

    try {
      setIsUploading(true);
      const res = await uploadService.uploadFile(file, 'legal_documents');
      form.setValue('verificationDocumentUrl', res.data || '', { shouldValidate: true });
      toast.success(t('uploadSuccess'));
    } catch (error) {
      toast.error(t('uploadFail'));
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
        toast.success(t('submitUpdateSuccess'));
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
        toast.success(t('submitSuccess'));
      }
    } catch (error) {
      toast.error(t('submitFail'));
    }
  };

  if (isFetchingCompany) {
    return <div className="p-8 text-center text-muted-foreground">{t('loadingLegal')}</div>;
  }

  if (!company) return null;

  const currentDoc = form.watch('verificationDocumentUrl');

  return (
    <div className="w-full pb-8 space-y-6">
      <div className="mb-6">
        <h2 className="text-xl font-bold">{t('pageTitle')}</h2>
        <p className="text-muted-foreground">{t('pageDesc')}</p>
      </div>

      {company.hasPendingChange && (
        <div className="mb-6 bg-blue-50 border border-blue-200 rounded-xl p-4 text-sm text-blue-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">{t('pendingUpdate')}</p>
          <p className="text-blue-700/80 text-xs">
            {t('pendingUpdateDesc')}
          </p>
        </div>
      )}

      {company.status === 'PENDING' && !company.hasPendingChange && (
        <div className="mb-6 bg-yellow-50 border border-yellow-200 rounded-xl p-4 text-sm text-yellow-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">{t('pendingVer')}</p>
          <p className="text-yellow-700/80 text-xs">
            {t('pendingVerDesc')}
          </p>
        </div>
      )}

      {company.status === 'VERIFIED' && !company.hasPendingChange && (
        <div className="mb-6 bg-green-50 border border-green-200 rounded-xl p-4 text-sm text-green-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">{t('verified')}</p>
          <p className="text-green-700/80 text-xs">
            {t('verifiedDesc')}
          </p>
        </div>
      )}

      {company.status === 'REJECTED' && (
        <div className="mb-6 bg-red-50 border border-red-200 rounded-xl p-4 text-sm text-red-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">{t('rejected')}</p>
          <p className="text-red-700/80 text-xs" dangerouslySetInnerHTML={{__html: t.raw('rejectedDesc').replace('{reason}', company.rejectReason || t('noReason'))}}>
          </p>
        </div>
      )}

      {company.status === 'VERIFIED' && !company.hasPendingChange && company.rejectReason && (
        <div className="mb-6 bg-red-50 border border-red-200 rounded-xl p-4 text-sm text-red-800 flex flex-col gap-1">
          <p className="font-semibold flex items-center gap-1.5">{t('updateRejected')}</p>
          <p className="text-red-700/80 text-xs" dangerouslySetInnerHTML={{__html: t.raw('updateRejectedDesc').replace('{reason}', company.rejectReason || '')}}>
          </p>
        </div>
      )}

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="grid md:grid-cols-2 gap-8">
          
          {/* Left Column: Details */}
          <div className="bg-card rounded-xl border p-6 space-y-6">
            <h3 className="font-semibold text-lg border-b pb-2">{t('companyDetails')}</h3>
            
            <FormField
              control={form.control}
              name="taxCode"
              render={({ field }) => (
              <FormItem>
                  <FormLabel>{t('taxId')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('taxIdPlaceholder')} {...field} disabled={isReadOnly} />
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
                  <FormLabel>{t('companyName')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('companyNamePlaceholder')} {...field} disabled={isReadOnly} />
                  </FormControl>
                  <p className="text-xs text-muted-foreground">
                    {company.status === 'VERIFIED' ? t('modifyName') : t('syncedName')}
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
                  <FormLabel>{t('hqAddress')}</FormLabel>
                  <FormControl>
                    <Input placeholder={t('hqAddressPlaceholder')} {...field} disabled={isReadOnly} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>

          {/* Right Column: Documents */}
          <div className="space-y-6">
            <div className="bg-card rounded-xl border p-6 space-y-6">
              <h3 className="font-semibold text-lg border-b pb-2">{t('attachedDocs')}</h3>
              <p className="text-sm text-muted-foreground">{t('docDesc')}</p>

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
                            {t('methodBrc')}
                          </FormLabel>
                        </FormItem>
                        <FormItem className="flex items-center space-x-3 space-y-0">
                          <FormControl>
                            <RadioGroupItem value="POA_AND_ID" />
                          </FormControl>
                          <FormLabel className="font-normal">
                            {t('methodPoa')}
                          </FormLabel>
                        </FormItem>
                      </RadioGroup>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="space-y-2">
                <FormLabel>{t('businessCert')}</FormLabel>
                <div className="border-2 border-dashed rounded-lg p-6 flex flex-col items-center justify-center text-center bg-muted/20">
                  {currentDoc ? (
                    <div className="space-y-2">
                      <p className="text-sm font-medium text-primary">{t('docUploaded')}</p>
                      <a href={currentDoc} target="_blank" rel="noreferrer" className="text-xs text-blue-500 hover:underline">{t('viewDoc')}</a>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <p className="text-sm font-medium">{t('uploadHint')}</p>
                      <p className="text-xs text-muted-foreground">{t('uploadLimits')}</p>
                    </div>
                  )}
                  
                  {!isReadOnly && (
                    <div className="mt-4">
                      <Button type="button" variant="outline" size="sm" className="relative" disabled={isUploading}>
                        {isUploading ? t('uploading') : (currentDoc ? t('replaceFile') : t('uploadFile'))}
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
                <p className="font-semibold mb-2">{t('docGuidelines')}</p>
                <ul className="list-disc list-inside space-y-1 text-amber-700/80 text-xs">
                  <li>{t('guide1')}</li>
                  <li>{t('guide2')}</li>
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
                  {t('requestUpdate')}
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
                    {t('cancel')}
                  </Button>
                  <Button 
                    type="submit" 
                    disabled={isSubmitting || isUpdatingRequest || isUploading}
                    className="cursor-pointer"
                  >
                    {isUpdatingRequest ? t('submitting') : t('submitUpdateReq')}
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
                  {t('reset')}
                </Button>
                <Button 
                  type="submit" 
                  disabled={isSubmitting || isUploading || isReadOnly}
                  className="cursor-pointer"
                >
                  {isSubmitting ? t('saving') : t('saveLegal')}
                </Button>
              </>
            )}
          </div>
        </form>
      </Form>
    </div>
  );
}
