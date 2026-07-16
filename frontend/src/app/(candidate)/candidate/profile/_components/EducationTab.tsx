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
    <div className="space-y-6">
      {/* Education Block */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b pb-4 flex flex-row items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <CardTitle className="text-xl font-bold mt-1">Education</CardTitle>
            {mappedEducations.length === 0 && (
              <CardDescription className="text-sm">Your academic background and credentials</CardDescription>
            )}
          </div>
          <Button
            onClick={() => {
              setEditingEduId(null);
              setIsAddingEdu(true);
            }}
            disabled={isAddingEdu || editingEduId !== null}
            variant="outline"
            size="icon"
            className="rounded-full border-primary text-primary hover:bg-primary/10 w-8 h-8 shrink-0 transition-colors mt-0"
          >
            <Plus className="w-5 h-5" />
          </Button>
        </CardHeader>
        <CardContent className={mappedEducations.length === 0 ? "p-0" : "px-6 py-2"}>
          {mappedEducations.length > 0 && (
            <div className="flex flex-col">
              {mappedEducations.map((edu) => (
                <React.Fragment key={edu.id}>
                  <EducationCard
                    education={edu}
                    onEdit={() => {
                      setIsAddingEdu(false);
                      setEditingEduId(edu.id);
                    }}
                    onDelete={setEduDeleteId}
                  />
                </React.Fragment>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog 
        open={isAddingEdu || !!editingEduId} 
        onOpenChange={(open) => {
          if (!open) {
            setIsAddingEdu(false);
            setEditingEduId(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="text-xl">
              {isAddingEdu ? 'Add Education' : 'Edit Education'}
            </DialogTitle>
            <DialogDescription>
              Fill in the details of your academic background below.
            </DialogDescription>
          </DialogHeader>
          <div className="pt-2">
            <EducationForm
              initialData={editingEduId ? mappedEducations.find(e => e.id === editingEduId) || null : null}
              onCancel={() => {
                setIsAddingEdu(false);
                setEditingEduId(null);
              }}
              onSuccess={() => {
                setIsAddingEdu(false);
                setEditingEduId(null);
              }}
            />
          </div>
        </DialogContent>
      </Dialog>

      {/* Certifications Block */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b pb-4 flex flex-row items-start justify-between gap-4">
          <div className="flex flex-col gap-1">
            <CardTitle className="text-xl font-bold mt-1">Certifications</CardTitle>
            {certifications.length === 0 && (
              <CardDescription className="text-sm">Professional certifications and licenses</CardDescription>
            )}
          </div>
          <Button
            onClick={() => {
              setEditingCertId(null);
              setIsAddingCert(true);
            }}
            disabled={isAddingCert || editingCertId !== null}
            variant="outline"
            size="icon"
            className="rounded-full border-primary text-primary hover:bg-primary/10 w-8 h-8 shrink-0 transition-colors mt-0"
          >
            <Plus className="w-5 h-5" />
          </Button>
        </CardHeader>
        <CardContent className={certifications.length === 0 ? "p-0" : "px-6 py-2"}>
          {certifications.length > 0 && (
            <div className="flex flex-col">
              {certifications.map((cert) => (
                <React.Fragment key={cert.id}>
                  <CertificationCard
                    certification={cert}
                    onEdit={() => {
                      setIsAddingCert(false);
                      setEditingCertId(cert.id);
                    }}
                    onDelete={setCertDeleteId}
                  />
                </React.Fragment>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog 
        open={isAddingCert || !!editingCertId} 
        onOpenChange={(open) => {
          if (!open) {
            setIsAddingCert(false);
            setEditingCertId(null);
          }
        }}
      >
        <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="text-xl">
              {isAddingCert ? 'Add Certification' : 'Edit Certification'}
            </DialogTitle>
            <DialogDescription>
              Fill in the details of your certification below.
            </DialogDescription>
          </DialogHeader>
          <div className="pt-2">
            <CertificationForm
              initialData={editingCertId ? certifications.find(c => c.id === editingCertId) || null : null}
              onCancel={() => {
                setIsAddingCert(false);
                setEditingCertId(null);
              }}
              onSuccess={() => {
                setIsAddingCert(false);
                setEditingCertId(null);
              }}
            />
          </div>
        </DialogContent>
      </Dialog>

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
