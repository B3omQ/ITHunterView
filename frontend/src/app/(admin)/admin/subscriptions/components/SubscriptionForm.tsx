'use client';

import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { useTranslations } from 'next-intl';
import type { SubscriptionDto, CreateSubscriptionDto } from '@/types/subscription.types';

const formSchema = z.object({
  name: z.string().min(1, 'Package name is required').max(100),
  price: z.coerce.number().min(0, 'Price cannot be negative'),
  durationDays: z.coerce.number().min(1, 'Duration must be at least 1 day'),
  role: z.enum(['CANDIDATE', 'RECRUITER']),
  // Candidate AI limits
  cvMatchLimit: z.coerce.number().nullable().optional(),
  cvOptimizeLimit: z.coerce.number().nullable().optional(),
  mockInterviewLimit: z.coerce.number().nullable().optional(),
  learningPathLimit: z.coerce.number().nullable().optional(),
  learningPathSlotLimit: z.coerce.number().nullable().optional(),
  // Recruiter Limits
  jobSlots: z.coerce.number().nullable().optional(),
  jobExtendLimit: z.coerce.number().nullable().optional(),
  unlockCvLimit: z.coerce.number().nullable().optional(),
  pushTopLimit: z.coerce.number().nullable().optional(),
  // Common
  coinCredit: z.coerce.number().nullable().optional(),
});

type FormValues = z.infer<typeof formSchema>;

interface SubscriptionFormProps {
  initialData?: SubscriptionDto | null;
  onSubmit: (data: CreateSubscriptionDto) => void;
  isLoading: boolean;
}

export function SubscriptionForm({ initialData, onSubmit, isLoading }: SubscriptionFormProps) {
  const t = useTranslations('AdminSubscriptions');
  const isEdit = !!initialData;
  const isUsed = initialData?.isUsed || false;

  const form = useForm<any>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      price: 0,
      durationDays: 30,
      role: 'CANDIDATE',
      cvMatchLimit: null,
      cvOptimizeLimit: null,
      mockInterviewLimit: null,
      learningPathLimit: null,
      learningPathSlotLimit: null,
      jobSlots: null,
      jobExtendLimit: null,
      unlockCvLimit: null,
      pushTopLimit: null,
      coinCredit: 0,
    },
  });

  const selectedRole = form.watch('role');

  useEffect(() => {
    if (initialData) {
      const cfg = initialData.featuresConfig || {};
      form.reset({
        name: initialData.name,
        price: initialData.price,
        durationDays: initialData.durationDays,
        role: cfg.role || 'CANDIDATE',
        cvMatchLimit: cfg.cvMatchLimit ?? null,
        cvOptimizeLimit: cfg.cvOptimizeLimit ?? null,
        mockInterviewLimit: cfg.mockInterviewLimit ?? null,
        learningPathLimit: cfg.learningPathLimit ?? null,
        learningPathSlotLimit: cfg.learningPathSlotLimit ?? null,
        jobSlots: cfg.jobSlots ?? null,
        jobExtendLimit: cfg.jobExtendLimit ?? null,
        unlockCvLimit: cfg.unlockCvLimit ?? null,
        pushTopLimit: cfg.pushTopLimit ?? null,
        coinCredit: cfg.coinCredit ?? 0,
      });
    } else {
      form.reset({
        name: '',
        price: 0,
        durationDays: 30,
        role: 'CANDIDATE',
        cvMatchLimit: null,
        cvOptimizeLimit: null,
        mockInterviewLimit: null,
        learningPathLimit: null,
        learningPathSlotLimit: null,
        jobSlots: null,
        jobExtendLimit: null,
        unlockCvLimit: null,
        pushTopLimit: null,
        coinCredit: 0,
      });
    }
  }, [initialData, form]);

  const handleFormSubmit = (values: FormValues) => {
    // Transform values back to DTO
    const featuresConfig: any = {
      role: values.role,
    };

    featuresConfig.coinCredit = values.coinCredit;
    if (values.role === 'CANDIDATE') {
      featuresConfig.cvMatchLimit = values.cvMatchLimit;
      featuresConfig.cvOptimizeLimit = values.cvOptimizeLimit;
      featuresConfig.mockInterviewLimit = values.mockInterviewLimit;
      featuresConfig.learningPathLimit = values.learningPathLimit;
      featuresConfig.learningPathSlotLimit = values.learningPathSlotLimit;
    } else {
      featuresConfig.jobSlots = values.jobSlots;
      featuresConfig.jobExtendLimit = values.jobExtendLimit;
      featuresConfig.unlockCvLimit = values.unlockCvLimit;
      featuresConfig.pushTopLimit = values.pushTopLimit;
    }

    onSubmit({
      name: values.name,
      price: values.price,
      durationDays: values.durationDays,
      featuresConfig,
    });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-4">
        {/* Name */}
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('formPackageName')}</FormLabel>
              <FormControl>
                <Input placeholder={t('formPlaceholderPackageName')} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          {/* Price */}
          <FormField
            control={form.control}
            name="price"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('formPrice')}</FormLabel>
                <FormControl>
                  <Input type="number" disabled={isUsed} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Duration Days */}
          <FormField
            control={form.control}
            name="durationDays"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('formDuration')}</FormLabel>
                <FormControl>
                  <Input type="number" disabled={isUsed} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Role Selector */}
        <FormField
          control={form.control}
          name="role"
          render={({ field }) => (
            <FormItem>
              <FormLabel>{t('formTargetAudience')}</FormLabel>
              <Select
                disabled={isUsed || isEdit}
                onValueChange={field.onChange}
                value={field.value}
              >
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder={t('formSelectRole')} />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="CANDIDATE">{t('roleCandidate')}</SelectItem>
                  <SelectItem value="RECRUITER">{t('roleRecruiter')}</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Conditionally Render Fields based on Role */}
        <div className="p-4 border rounded-lg bg-neutral-50/50 space-y-4">
          <h4 className="text-sm font-semibold text-neutral-900 mb-2">{t('formFeatureLimitTitle')}</h4>
          
          {selectedRole === 'CANDIDATE' && (
            <div className="space-y-4">
              {/* CV Match Limit */}
              <FormField
                control={form.control}
                name="cvMatchLimit"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('formCvMatchLimit')}</FormLabel>
                    <FormControl>
                      <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* CV Optimize Limit */}
              <FormField
                control={form.control}
                name="cvOptimizeLimit"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('formCvOptimizeLimit')}</FormLabel>
                    <FormControl>
                      <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* Mock Interview Limit */}
              <FormField
                control={form.control}
                name="mockInterviewLimit"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('formMockInterviewLimit')}</FormLabel>
                    <FormControl>
                      <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
               {/* Learning Path Limit */}
               <FormField
                 control={form.control}
                 name="learningPathLimit"
                 render={({ field }) => (
                   <FormItem>
                     <FormLabel>{t('formLearningPathLimit')}</FormLabel>
                     <FormControl>
                       <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                     </FormControl>
                     <FormMessage />
                   </FormItem>
                 )}
               />
               {/* Learning Path Slot Limit */}
               <FormField
                 control={form.control}
                 name="learningPathSlotLimit"
                 render={({ field }) => (
                   <FormItem>
                     <FormLabel>{t('formLearningPathSlotLimit')}</FormLabel>
                     <FormControl>
                       <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                     </FormControl>
                     <FormMessage />
                   </FormItem>
                 )}
               />
            </div>
          )}

          {selectedRole === 'RECRUITER' && (
            <div className="space-y-4">
              {/* Job Slots */}
              <FormField
                control={form.control}
                name="jobSlots"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('formJobSlots')}</FormLabel>
                    <FormControl>
                      <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* Job Extend Limit */}
              <FormField
                control={form.control}
                name="jobExtendLimit"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('formJobExtendLimit')}</FormLabel>
                    <FormControl>
                      <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* Unlock CV Limit */}
              <FormField
                control={form.control}
                name="unlockCvLimit"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('formUnlockCvLimit')}</FormLabel>
                    <FormControl>
                      <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {/* Push Top Limit */}
              <FormField
                control={form.control}
                name="pushTopLimit"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('formPushTopLimit')}</FormLabel>
                    <FormControl>
                      <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
          )}

          {/* Common Field: Coin Credit */}
          <FormField
            control={form.control}
            name="coinCredit"
            render={({ field }) => (
              <FormItem className="mt-4">
                <FormLabel>{t('formCoinCredit')}</FormLabel>
                <FormControl>
                  <Input type="number" disabled={isUsed} {...field} value={field.value ?? ''} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {isUsed && (
          <p className="text-xs text-amber-600 font-medium">
            {t('formUsedWarning')}
          </p>
        )}

        <div className="flex justify-end gap-2 pt-4 border-t">
          <Button type="submit" disabled={isLoading}>
            {isLoading ? t('btnProcessing') : isEdit ? t('btnSaveChanges') : t('btnCreatePackage')}
          </Button>
        </div>
      </form>
    </Form>
  );
}
