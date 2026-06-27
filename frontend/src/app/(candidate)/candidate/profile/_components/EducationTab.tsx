'use client';

import React, { useState } from 'react';
import {
  useMajors,
  useCandidateEducations,
  useDeleteEducation,
  useCandidateCertifications,
  useDeleteCertification,
} from '@/hooks/useCandidateProfile';
import { PageLoader } from '@/components/shared/PageLoader';
import { EducationCard } from '@/components/shared/EducationCard';
import { CertificationCard } from '@/components/shared/CertificationCard';
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
import { GraduationCap, Award, Plus, AlertTriangle, Loader2 } from 'lucide-react';
import { EducationForm } from './EducationForm';
import { CertificationForm } from './CertificationForm';

export function EducationTab() {
  const { data: majors } = useMajors();
  const { data: educations, isLoading: isLoadingEdu, isError: isErrorEdu } = useCandidateEducations();
  const { data: certifications, isLoading: isLoadingCert, isError: isErrorCert } = useCandidateCertifications();

  const { mutate: deleteEdu, isPending: isDeletingEdu } = useDeleteEducation();
  const { mutate: deleteCert, isPending: isDeletingCert } = useDeleteCertification();

  // Orchestration states for Educations
  const [isAddingEdu, setIsAddingEdu] = useState(false);
  const [editingEduId, setEditingEduId] = useState<string | null>(null);
  const [eduDeleteId, setEduDeleteId] = useState<string | null>(null);

  // Orchestration states for Certifications
  const [isAddingCert, setIsAddingCert] = useState(false);
  const [editingCertId, setEditingCertId] = useState<string | null>(null);
  const [certDeleteId, setCertDeleteId] = useState<string | null>(null);

  const handleEduDelete = () => {
    if (eduDeleteId) {
      deleteEdu(eduDeleteId, {
        onSuccess: () => setEduDeleteId(null),
      });
    }
  };

  const handleCertDelete = () => {
    if (certDeleteId) {
      deleteCert(certDeleteId, {
        onSuccess: () => setCertDeleteId(null),
      });
    }
  };

  if (isLoadingEdu || isLoadingCert) {
    return <PageLoader message="Loading academic details..." />;
  }

  if (isErrorEdu || isErrorCert || !educations || !certifications) {
    return (
      <div className="p-8 border rounded-xl bg-card text-center text-muted-foreground">
        Failed to load academic records. Please try again.
      </div>
    );
  }

  // Map majorName to educations for card display
  const mappedEducations = educations.map((edu) => {
    const major = majors?.find((m) => m.id === edu.majorId);
    return {
      ...edu,
      majorName: major ? major.name : edu.majorName || null,
    };
  });

  return (
    <div className="space-y-8">
      {/* Education Block */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4 flex flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <GraduationCap className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Education</CardTitle>
              <CardDescription className="text-xs">Your academic background and credentials</CardDescription>
            </div>
          </div>
          <Button
            onClick={() => {
              setEditingEduId(null);
              setIsAddingEdu(true);
            }}
            disabled={isAddingEdu || editingEduId !== null}
            className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-4 rounded-lg flex items-center gap-1.5 shadow-md shadow-primary/10"
          >
            <Plus className="w-4 h-4" />
            Add Education
          </Button>
        </CardHeader>
        <CardContent className="p-6">
          {mappedEducations.length === 0 && !isAddingEdu ? (
            <div className="text-center py-12 border border-dashed border-border/30 rounded-xl bg-muted/10">
              <p className="text-sm text-muted-foreground italic mb-3">No education history added yet.</p>
              <Button onClick={() => setIsAddingEdu(true)} variant="outline" className="text-xs font-semibold rounded-lg">
                Add your education details
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {isAddingEdu && (
                <EducationForm
                  onCancel={() => setIsAddingEdu(false)}
                  onSuccess={() => setIsAddingEdu(false)}
                />
              )}
              {mappedEducations.map((edu) => (
                <React.Fragment key={edu.id}>
                  {editingEduId === edu.id ? (
                    <EducationForm
                      initialData={edu}
                      onCancel={() => setEditingEduId(null)}
                      onSuccess={() => setEditingEduId(null)}
                    />
                  ) : (
                    <EducationCard
                      education={edu}
                      onEdit={() => {
                        setIsAddingEdu(false);
                        setEditingEduId(edu.id);
                      }}
                      onDelete={setEduDeleteId}
                    />
                  )}
                </React.Fragment>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Certifications Block */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4 flex flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Award className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Certifications</CardTitle>
              <CardDescription className="text-xs">Professional certifications and licenses</CardDescription>
            </div>
          </div>
          <Button
            onClick={() => {
              setEditingCertId(null);
              setIsAddingCert(true);
            }}
            disabled={isAddingCert || editingCertId !== null}
            className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-4 rounded-lg flex items-center gap-1.5 shadow-md shadow-primary/10"
          >
            <Plus className="w-4 h-4" />
            Add Certification
          </Button>
        </CardHeader>
        <CardContent className="p-6">
          {certifications.length === 0 && !isAddingCert ? (
            <div className="text-center py-12 border border-dashed border-border/30 rounded-xl bg-muted/10">
              <p className="text-sm text-muted-foreground italic mb-3">No certifications added yet.</p>
              <Button onClick={() => setIsAddingCert(true)} variant="outline" className="text-xs font-semibold rounded-lg">
                Add your certifications
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {isAddingCert && (
                <CertificationForm
                  onCancel={() => setIsAddingCert(false)}
                  onSuccess={() => setIsAddingCert(false)}
                />
              )}
              {certifications.map((cert) => (
                <React.Fragment key={cert.id}>
                  {editingCertId === cert.id ? (
                    <CertificationForm
                      initialData={cert}
                      onCancel={() => setEditingCertId(null)}
                      onSuccess={() => setEditingCertId(null)}
                    />
                  ) : (
                    <CertificationCard
                      certification={cert}
                      onEdit={() => {
                        setIsAddingCert(false);
                        setEditingCertId(cert.id);
                      }}
                      onDelete={setCertDeleteId}
                    />
                  )}
                </React.Fragment>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Delete Confirmation Dialog for Education */}
      <Dialog open={!!eduDeleteId} onOpenChange={(open) => {
        if (!open && !isDeletingEdu) setEduDeleteId(null);
      }}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg z-[60]">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Delete Education</DialogTitle>
            <DialogDescription className="text-xs">
              Are you sure you want to delete this academic record? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setEduDeleteId(null)}
              disabled={isDeletingEdu}
              className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
            >
              Cancel
            </Button>
            <Button
              onClick={handleEduDelete}
              disabled={isDeletingEdu}
              className="bg-destructive hover:bg-destructive/95 transition-all text-destructive-foreground font-semibold px-6 shadow-md shadow-destructive/10 rounded-lg flex items-center gap-2"
            >
              {isDeletingEdu && <Loader2 className="w-4 h-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog for Certification */}
      <Dialog open={!!certDeleteId} onOpenChange={(open) => {
        if (!open && !isDeletingCert) setCertDeleteId(null);
      }}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg z-[60]">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Delete Certification</DialogTitle>
            <DialogDescription className="text-xs">
              Are you sure you want to delete this certification? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setCertDeleteId(null)}
              disabled={isDeletingCert}
              className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
            >
              Cancel
            </Button>
            <Button
              onClick={handleCertDelete}
              disabled={isDeletingCert}
              className="bg-destructive hover:bg-destructive/95 transition-all text-destructive-foreground font-semibold px-6 shadow-md shadow-destructive/10 rounded-lg flex items-center gap-2"
            >
              {isDeletingCert && <Loader2 className="w-4 h-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
