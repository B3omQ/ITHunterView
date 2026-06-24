'use client';

import React, { useState } from 'react';
import {
  useMajors,
  useCandidateEducations,
  useCreateEducation,
  useUpdateEducation,
  useDeleteEducation,
  useCandidateCertifications,
  useCreateCertification,
  useUpdateCertification,
  useDeleteCertification,
} from '@/hooks/useCandidateProfile';
import type {
  CandidateEducation,
  EducationUpsertRequest,
  CandidateCertification,
  CertificationUpsertRequest,
} from '@/types/candidate.types';
import { PageLoader } from '@/components/shared/PageLoader';
import { EducationCard } from '@/components/shared/EducationCard';
import { CertificationCard } from '@/components/shared/CertificationCard';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { GraduationCap, Award, Plus, AlertTriangle, Loader2 } from 'lucide-react';

export function EducationTab() {
  const { data: majors } = useMajors();
  const { data: educations, isLoading: isLoadingEdu, isError: isErrorEdu } = useCandidateEducations();
  const { data: certifications, isLoading: isLoadingCert, isError: isErrorCert } = useCandidateCertifications();

  // Education mutations
  const { mutate: createEdu, isPending: isCreatingEdu } = useCreateEducation();
  const { mutate: updateEdu, isPending: isUpdatingEdu } = useUpdateEducation();
  const { mutate: deleteEdu, isPending: isDeletingEdu } = useDeleteEducation();

  // Certification mutations
  const { mutate: createCert, isPending: isCreatingCert } = useCreateCertification();
  const { mutate: updateCert, isPending: isUpdatingCert } = useUpdateCertification();
  const { mutate: deleteCert, isPending: isDeletingCert } = useDeleteCertification();

  // Dialog Education states
  const [isEduOpen, setIsEduOpen] = useState(false);
  const [editingEdu, setEditingEdu] = useState<CandidateEducation | null>(null);
  const [eduDeleteId, setEduDeleteId] = useState<string | null>(null);

  // Form Education states
  const [degree, setDegree] = useState('');
  const [institutionName, setInstitutionName] = useState('');
  const [majorId, setMajorId] = useState<string>('');
  const [gpa, setGpa] = useState('');
  const [maxGpa, setMaxGpa] = useState('4.0');
  const [eduStartDate, setEduStartDate] = useState('');
  const [eduEndDate, setEduEndDate] = useState('');
  const [eduDescription, setEduDescription] = useState('');

  // Dialog Certification states
  const [isCertOpen, setIsCertOpen] = useState(false);
  const [editingCert, setEditingCert] = useState<CandidateCertification | null>(null);
  const [certDeleteId, setCertDeleteId] = useState<string | null>(null);

  // Form Certification states
  const [certName, setCertName] = useState('');
  const [issuingOrganization, setIssuingOrganization] = useState('');
  const [issueDate, setIssueDate] = useState('');
  const [expirationDate, setExpirationDate] = useState('');
  const [credentialUrl, setCredentialUrl] = useState('');

  // --- Education Handlers ---
  const openAddEduDialog = () => {
    setEditingEdu(null);
    setDegree('');
    setInstitutionName('');
    setMajorId('');
    setGpa('');
    setMaxGpa('4.0');
    setEduStartDate('');
    setEduEndDate('');
    setEduDescription('');
    setIsEduOpen(true);
  };

  const openEditEduDialog = (edu: CandidateEducation) => {
    setEditingEdu(edu);
    setDegree(edu.degree || '');
    setInstitutionName(edu.institutionName || '');
    setMajorId(edu.majorId ? String(edu.majorId) : '');
    setGpa(edu.gpa !== null ? String(edu.gpa) : '');
    setMaxGpa(edu.maxGpa !== null ? String(edu.maxGpa) : '4.0');
    setEduStartDate(edu.startDate ? edu.startDate.split('T')[0] : '');
    setEduEndDate(edu.endDate ? edu.endDate.split('T')[0] : '');
    setEduDescription(edu.description || '');
    setIsEduOpen(true);
  };

  const handleEduSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!degree.trim() || !institutionName.trim()) return;

    const payload: EducationUpsertRequest = {
      degree,
      institutionName,
      majorId: majorId ? Number(majorId) : null,
      gpa: gpa ? Number(gpa) : null,
      maxGpa: maxGpa ? Number(maxGpa) : null,
      startDate: eduStartDate || null,
      endDate: eduEndDate || null,
      description: eduDescription || null,
    };

    if (editingEdu) {
      updateEdu(
        { id: editingEdu.id, payload },
        {
          onSuccess: () => setIsEduOpen(false),
        }
      );
    } else {
      createEdu(payload, {
        onSuccess: () => setIsEduOpen(false),
      });
    }
  };

  const handleEduDelete = () => {
    if (eduDeleteId) {
      deleteEdu(eduDeleteId, {
        onSuccess: () => setEduDeleteId(null),
      });
    }
  };

  // --- Certification Handlers ---
  const openAddCertDialog = () => {
    setEditingCert(null);
    setCertName('');
    setIssuingOrganization('');
    setIssueDate('');
    setExpirationDate('');
    setCredentialUrl('');
    setIsCertOpen(true);
  };

  const openEditCertDialog = (cert: CandidateCertification) => {
    setEditingCert(cert);
    setCertName(cert.name || '');
    setIssuingOrganization(cert.issuingOrganization || '');
    setIssueDate(cert.issueDate ? cert.issueDate.split('T')[0] : '');
    setExpirationDate(cert.expirationDate ? cert.expirationDate.split('T')[0] : '');
    setCredentialUrl(cert.credentialUrl || '');
    setIsCertOpen(true);
  };

  const handleCertSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!certName.trim() || !issuingOrganization.trim()) return;

    const payload: CertificationUpsertRequest = {
      name: certName,
      issuingOrganization,
      issueDate: issueDate || null,
      expirationDate: expirationDate || null,
      credentialUrl: credentialUrl || null,
    };

    if (editingCert) {
      updateCert(
        { id: editingCert.id, payload },
        {
          onSuccess: () => setIsCertOpen(false),
        }
      );
    } else {
      createCert(payload, {
        onSuccess: () => setIsCertOpen(false),
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
            onClick={openAddEduDialog}
            className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-4 rounded-lg flex items-center gap-1.5 shadow-md shadow-primary/10"
          >
            <Plus className="w-4 h-4" />
            Add Education
          </Button>
        </CardHeader>
        <CardContent className="p-6">
          {mappedEducations.length === 0 ? (
            <div className="text-center py-12 border border-dashed border-border/30 rounded-xl bg-muted/10">
              <p className="text-sm text-muted-foreground italic mb-3">No education history added yet.</p>
              <Button onClick={openAddEduDialog} variant="outline" className="text-xs font-semibold rounded-lg">
                Add your education details
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {mappedEducations.map((edu) => (
                <EducationCard
                  key={edu.id}
                  education={edu}
                  onEdit={openEditEduDialog}
                  onDelete={setEduDeleteId}
                />
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
            onClick={openAddCertDialog}
            className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-4 rounded-lg flex items-center gap-1.5 shadow-md shadow-primary/10"
          >
            <Plus className="w-4 h-4" />
            Add Certification
          </Button>
        </CardHeader>
        <CardContent className="p-6">
          {certifications.length === 0 ? (
            <div className="text-center py-12 border border-dashed border-border/30 rounded-xl bg-muted/10">
              <p className="text-sm text-muted-foreground italic mb-3">No certifications added yet.</p>
              <Button onClick={openAddCertDialog} variant="outline" className="text-xs font-semibold rounded-lg">
                Add your certifications
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {certifications.map((cert) => (
                <CertificationCard
                  key={cert.id}
                  certification={cert}
                  onEdit={openEditCertDialog}
                  onDelete={setCertDeleteId}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* --- EDUCATION DIALOG --- */}
      <Dialog open={isEduOpen} onOpenChange={setIsEduOpen}>
        <DialogContent className="max-w-lg rounded-2xl border-border/40 backdrop-blur-lg">
          <form onSubmit={handleEduSubmit} className="space-y-4">
            <DialogHeader>
              <DialogTitle className="text-lg font-bold">
                {editingEdu ? 'Edit Education' : 'Add Education'}
              </DialogTitle>
              <DialogDescription className="text-xs">
                Fill in details about your degree, institution, and major
              </DialogDescription>
            </DialogHeader>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="degree" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Degree *</Label>
                <Input
                  id="degree"
                  placeholder="e.g. B.S. Computer Science"
                  value={degree}
                  onChange={(e) => setDegree(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                  required
                />
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="institutionName" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Institution Name *</Label>
                <Input
                  id="institutionName"
                  placeholder="e.g. UC Berkeley"
                  value={institutionName}
                  onChange={(e) => setInstitutionName(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                  required
                />
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="major" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Major / Field of Study</Label>
                <select
                  id="major"
                  value={majorId}
                  onChange={(e) => setMajorId(e.target.value)}
                  className="w-full rounded-lg border border-border/60 bg-background/50 px-3 py-2 text-sm shadow-sm transition-all focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30"
                >
                  <option value="">Select major...</option>
                  {majors?.map((m) => (
                    <option key={m.id} value={m.id}>
                      {m.name} ({m.code})
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="gpa" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">GPA</Label>
                <Input
                  id="gpa"
                  type="number"
                  step="0.01"
                  min="0"
                  max="10"
                  placeholder="e.g. 3.8"
                  value={gpa}
                  onChange={(e) => setGpa(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="maxGpa" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Scale (Max GPA)</Label>
                <Input
                  id="maxGpa"
                  type="number"
                  step="0.1"
                  placeholder="e.g. 4.0"
                  value={maxGpa}
                  onChange={(e) => setMaxGpa(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="eduStartDate" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Start Date</Label>
                <Input
                  id="eduStartDate"
                  type="date"
                  value={eduStartDate}
                  onChange={(e) => setEduStartDate(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="eduEndDate" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">End Date</Label>
                <Input
                  id="eduEndDate"
                  type="date"
                  value={eduEndDate}
                  onChange={(e) => setEduEndDate(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="eduDescription" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Activities and Societies / Description</Label>
                <Textarea
                  id="eduDescription"
                  placeholder="Describe your research, honors, societies, or activities..."
                  value={eduDescription}
                  onChange={(e) => setEduDescription(e.target.value)}
                  rows={3}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30 resize-none"
                />
              </div>
            </div>

            <DialogFooter className="pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsEduOpen(false)}
                className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={isCreatingEdu || isUpdatingEdu}
                className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-6 shadow-md shadow-primary/10 rounded-lg flex items-center gap-2"
              >
                {(isCreatingEdu || isUpdatingEdu) && <Loader2 className="w-4 h-4 animate-spin" />}
                {editingEdu ? 'Update' : 'Add'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* --- CERTIFICATION DIALOG --- */}
      <Dialog open={isCertOpen} onOpenChange={setIsCertOpen}>
        <DialogContent className="max-w-lg rounded-2xl border-border/40 backdrop-blur-lg">
          <form onSubmit={handleCertSubmit} className="space-y-4">
            <DialogHeader>
              <DialogTitle className="text-lg font-bold">
                {editingCert ? 'Edit Certification' : 'Add Certification'}
              </DialogTitle>
              <DialogDescription className="text-xs">
                Fill in the certification details below
              </DialogDescription>
            </DialogHeader>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="certName" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Certification Name *</Label>
                <Input
                  id="certName"
                  placeholder="e.g. AWS Certified Developer"
                  value={certName}
                  onChange={(e) => setCertName(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                  required
                />
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="issuingOrg" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Issuing Organization *</Label>
                <Input
                  id="issuingOrg"
                  placeholder="e.g. Amazon Web Services"
                  value={issuingOrganization}
                  onChange={(e) => setIssuingOrganization(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="issueDate" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Issue Date</Label>
                <Input
                  id="issueDate"
                  type="date"
                  value={issueDate}
                  onChange={(e) => setIssueDate(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="expDate" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Expiration Date</Label>
                <Input
                  id="expDate"
                  type="date"
                  value={expirationDate}
                  onChange={(e) => setExpirationDate(e.target.value)}
                  placeholder="No expiry"
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>

              <div className="space-y-1.5 sm:col-span-2">
                <Label htmlFor="credentialUrl" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Credential URL</Label>
                <Input
                  id="credentialUrl"
                  placeholder="e.g. https://aws.amazon.com/verify/..."
                  value={credentialUrl}
                  onChange={(e) => setCredentialUrl(e.target.value)}
                  className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
                />
              </div>
            </div>

            <DialogFooter className="pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsCertOpen(false)}
                className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={isCreatingCert || isUpdatingCert}
                className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-6 shadow-md shadow-primary/10 rounded-lg flex items-center gap-2"
              >
                {(isCreatingCert || isUpdatingCert) && <Loader2 className="w-4 h-4 animate-spin" />}
                {editingCert ? 'Update' : 'Add'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Education Delete Confirmation */}
      <Dialog open={!!eduDeleteId} onOpenChange={(open) => !open && setEduDeleteId(null)}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Delete Education Record</DialogTitle>
            <DialogDescription className="text-xs">
              Are you sure you want to delete this education record? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setEduDeleteId(null)}
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

      {/* Certification Delete Confirmation */}
      <Dialog open={!!certDeleteId} onOpenChange={(open) => !open && setCertDeleteId(null)}>
        <DialogContent className="max-w-md rounded-2xl border-border/40 backdrop-blur-lg">
          <DialogHeader>
            <div className="w-12 h-12 rounded-xl bg-destructive/10 text-destructive flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6" />
            </div>
            <DialogTitle className="text-lg font-bold">Delete Certification Record</DialogTitle>
            <DialogDescription className="text-xs">
              Are you sure you want to delete this certification? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="pt-2">
            <Button
              variant="outline"
              onClick={() => setCertDeleteId(null)}
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
