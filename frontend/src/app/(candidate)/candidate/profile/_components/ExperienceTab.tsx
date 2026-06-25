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

export function ExperienceTab() {
  const { data: experiences, isLoading, isError } = useCandidateExperiences();
  const { mutate: deleteExperience, isPending: isDeleting } = useDeleteExperience();

  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const handleDelete = () => {
    if (deleteId) {
      deleteExperience(deleteId, {
        onSuccess: () => setDeleteId(null),
      });
    }
  };

  if (isLoading) {
    return <PageLoader message="Loading work experiences..." />;
  }

  if (isError || !experiences) {
    return (
      <div className="p-8 border rounded-xl bg-card text-center text-muted-foreground">
        Failed to load work experiences. Please try again.
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
    <div className="space-y-6">
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4 flex flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Briefcase className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Work Experience</CardTitle>
              <CardDescription className="text-xs">Your employment history and job details</CardDescription>
            </div>
          </div>
          <Button
            onClick={() => setIsAdding(true)}
            disabled={isAdding || !!editingId}
            className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-4 rounded-lg flex items-center gap-1.5 shadow-md shadow-primary/10"
          >
            <Plus className="w-4 h-4" />
            Add Experience
          </Button>
        </CardHeader>
        <CardContent className="p-6">
          {isAdding && (
            <div className="mb-6">
              <ExperienceForm
                initialData={null}
                onCancel={() => setIsAdding(false)}
                onSuccess={() => setIsAdding(false)}
              />
            </div>
          )}

          {!isAdding && sortedExperiences.length === 0 ? (
            <div className="text-center py-12 border border-dashed border-border/30 rounded-xl bg-muted/10">
              <p className="text-sm text-muted-foreground italic mb-3">No work experiences added yet.</p>
              <Button onClick={() => setIsAdding(true)} disabled={!!editingId} variant="outline" className="text-xs font-semibold rounded-lg">
                Add your first experience
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {sortedExperiences.map((exp) => (
                <React.Fragment key={exp.id}>
                  {editingId === exp.id ? (
                    <ExperienceForm
                      initialData={exp}
                      onCancel={() => setEditingId(null)}
                      onSuccess={() => setEditingId(null)}
                    />
                  ) : (
                    <ExperienceCard
                      experience={exp}
                      onEdit={() => {
                        setIsAdding(false);
                        setEditingId(exp.id);
                      }}
                      onDelete={setDeleteId}
                    />
                  )}
                </React.Fragment>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteId} onOpenChange={(open) => {
        if (!open && !isDeleting) setDeleteId(null);
      }}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg z-[60]">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Delete Work Experience</DialogTitle>
            <DialogDescription className="text-xs">
              Are you sure you want to delete this work experience? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setDeleteId(null)}
              disabled={isDeleting}
              className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
            >
              Cancel
            </Button>
            <Button
              onClick={handleDelete}
              disabled={isDeleting}
              className="bg-destructive hover:bg-destructive/95 transition-all text-destructive-foreground font-semibold px-6 shadow-md shadow-destructive/10 rounded-lg flex items-center gap-2"
            >
              {isDeleting && <Loader2 className="w-4 h-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
