'use client';

import React, { useState } from 'react';
import {
  useCandidateExperiences,
  useDeleteExperience,
} from '@/hooks/useCandidateProfile';
import { PageLoader } from '@/components/shared/PageLoader';
import { ExperienceCard } from '@/components/shared/ExperienceCard';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { toast } from 'sonner';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Briefcase, Plus, AlertTriangle, Loader2 } from 'lucide-react';
import { ExperienceForm } from './ExperienceForm';
import { useTranslations } from 'next-intl';

export function ExperienceTab() {
  const { data: experiences, isLoading, isError } = useCandidateExperiences();
  const { mutate: deleteExperience, isPending: isDeleting } = useDeleteExperience();
  const t = useTranslations("CandidateProfile");

  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const handleDelete = () => {
    if (deleteId) {
      deleteExperience(deleteId, {
        onSuccess: () => {
          toast.success(t('expDeleteSuccess'));
          setDeleteId(null);
        },
        onError: (error: any) => {
          toast.error(error?.response?.data?.message || error.message || t('expDeleteError'));
        },
      });
    }
  };

  if (isLoading) {
    return <PageLoader message={t('expLoading')} />;
  }

  if (isError || !experiences) {
    return (
      <div className="p-8 border rounded-xl bg-card text-center text-muted-foreground">
        {t('expLoadError')}
      </div>
    );
  }

  // Sắp xếp: experiences có isCurrent trước, sau đó theo startDate giảm dần
  const sortedExperiences = [...experiences].sort((a, b) => {
    if (a.isCurrent && !b.isCurrent) return -1;
    if (!a.isCurrent && b.isCurrent) return 1;

    const dateA = a.startDate ? new Date(a.startDate).getTime() : 0;
    const dateB = b.startDate ? new Date(b.startDate).getTime() : 0;
    return dateB - dateA;
  });

  return (
    <div className="space-y-6 w-full">
      <Card>
        <CardHeader className="border-b pb-4 flex flex-row items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <CardTitle className="text-xl font-bold mt-1">{t('workExperience')}</CardTitle>
            {sortedExperiences.length === 0 && (
              <CardDescription className="text-sm">{t('workExpDesc')}</CardDescription>
            )}
          </div>
          <Button
            onClick={() => setIsAdding(true)}
            disabled={isAdding || !!editingId}
            variant="outline"
            size="icon"
            className="rounded-full border-primary text-primary hover:bg-primary/10 w-8 h-8 shrink-0 transition-colors mt-0"
          >
            <Plus className="w-5 h-5" />
          </Button>
        </CardHeader>
        <CardContent className={sortedExperiences.length === 0 ? "p-0" : "px-6 py-2"}>
          {sortedExperiences.length > 0 && (
            <div className="flex flex-col">
              {sortedExperiences.map((exp) => (
                <React.Fragment key={exp.id}>
                  <ExperienceCard
                    experience={exp}
                    onEdit={() => {
                      setIsAdding(false);
                      setEditingId(exp.id);
                    }}
                    onDelete={setDeleteId}
                  />
                </React.Fragment>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Form Dialog */}
      <Dialog 
        disablePointerDismissal
        open={isAdding || !!editingId} 
        onOpenChange={(open) => {
          if (!open) {
            setIsAdding(false);
            setEditingId(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="text-xl">
              {isAdding ? t('addWorkExp') : t('editWorkExp')}
            </DialogTitle>
            <DialogDescription>
              {t('workExpFormDesc')}
            </DialogDescription>
          </DialogHeader>
          <div className="pt-2">
            <ExperienceForm
              initialData={editingId ? sortedExperiences.find(e => e.id === editingId) || null : null}
              onCancel={() => {
                setIsAdding(false);
                setEditingId(null);
              }}
              onSuccess={() => {
                setIsAdding(false);
                setEditingId(null);
              }}
            />
          </div>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteId} onOpenChange={(open) => {
        if (!open && !isDeleting) setDeleteId(null);
      }}>
        <DialogContent className="max-w-md z-[60]">
          <DialogHeader>
            <div className="w-12 h-12 rounded-md bg-muted text-muted-foreground flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">{t('deleteWorkExp')}</DialogTitle>
            <DialogDescription className="text-sm">
              {t('deleteWorkExpConfirm')}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setDeleteId(null)}
              disabled={isDeleting}
            >
              {t('cancel')}
            </Button>
            <Button
              onClick={handleDelete}
              disabled={isDeleting}
              variant="destructive"
              className="flex items-center gap-2"
            >
              {isDeleting && <Loader2 className="w-4 h-4 animate-spin" />}
              {t('delete')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
