'use client';

import React, { useState, useEffect } from 'react';
import { usePersonalInfo, useUpdatePersonalInfo } from '@/hooks/useCandidateProfile';
import { PageLoader } from '@/components/shared/PageLoader';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Globe, Mail, Phone, MapPin, AlignLeft, User, AlertTriangle } from 'lucide-react';

const LinkedinIcon = (props: React.SVGProps<SVGSVGElement>) => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    {...props}
  >
    <path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z" />
    <rect width="4" height="12" x="2" y="9" />
    <circle cx="4" cy="4" r="2" />
  </svg>
);

const GithubIcon = (props: React.SVGProps<SVGSVGElement>) => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    {...props}
  >
    <path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4" />
    <path d="M9 18c-4.51 2-5-2-7-2" />
  </svg>
);

export function PersonalInfoTab() {
  const { data: info, isLoading, isError } = usePersonalInfo();
  const { mutate: updateInfo, isPending } = useUpdatePersonalInfo();

  // Local form states
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [location, setLocation] = useState('');
  const [aboutMe, setAboutMe] = useState('');
  const [portfolioUrl, setPortfolioUrl] = useState('');
  const [linkedInUrl, setLinkedInUrl] = useState('');
  const [githubUrl, setGithubUrl] = useState('');
  
  // Modal state
  const [showConfirmModal, setShowConfirmModal] = useState(false);

  // Sync form states with API response data
  const resetForm = () => {
    if (info) {
      setFirstName(info.firstName || '');
      setLastName(info.lastName || '');
      setEmail(info.email || '');
      setPhone(info.phone || '');
      setLocation(info.location || '');
      setAboutMe(info.aboutMe || '');
      setPortfolioUrl(info.portfolioUrl || '');
      setLinkedInUrl(info.linkedInUrl || '');
      setGithubUrl(info.githubUrl || '');
    }
  };

  useEffect(() => {
    resetForm();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [info]);

  if (isLoading) {
    return <PageLoader message="Loading personal info..." />;
  }

  if (isError || !info) {
    return (
      <div className="p-8 border rounded-xl bg-card text-center text-muted-foreground">
        Failed to load personal info. Please try again.
      </div>
    );
  }

  const isDirty =
    firstName !== (info?.firstName || '') ||
    lastName !== (info?.lastName || '') ||
    phone !== (info?.phone || '') ||
    location !== (info?.location || '') ||
    aboutMe !== (info?.aboutMe || '') ||
    portfolioUrl !== (info?.portfolioUrl || '') ||
    linkedInUrl !== (info?.linkedInUrl || '') ||
    githubUrl !== (info?.githubUrl || '');

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    updateInfo({
      firstName,
      lastName,
      phone: phone || null,
      location: location || null,
      aboutMe: aboutMe || null,
      portfolioUrl: portfolioUrl || null,
      linkedInUrl: linkedInUrl || null,
      githubUrl: githubUrl || null,
    });
  };

  const handleDiscard = () => {
    if (isDirty) {
      setShowConfirmModal(true);
    } else {
      resetForm();
    }
  };

  const confirmDiscard = () => {
    resetForm();
    setShowConfirmModal(false);
  };

  return (
    <>
      <form onSubmit={handleSave} className="space-y-6">
      {/* Basic Information */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <User className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Basic Information</CardTitle>
              <CardDescription className="text-xs">Your personal identification details</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6 grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="space-y-2">
            <Label htmlFor="firstName" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">First Name</Label>
            <Input
              id="firstName"
              placeholder="First name"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="lastName" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Last Name</Label>
            <Input
              id="lastName"
              placeholder="Last name"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="email" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
              <Mail className="w-3.5 h-3.5 text-muted-foreground" /> Email Address
            </Label>
            <Input
              id="email"
              type="email"
              placeholder="Email address"
              value={email}
              readOnly
              className="bg-muted/50 border-border/40 text-muted-foreground cursor-not-allowed"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="phone" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
              <Phone className="w-3.5 h-3.5 text-muted-foreground" /> Phone Number
            </Label>
            <Input
              id="phone"
              placeholder="Phone number"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
            />
          </div>

          <div className="space-y-2 md:col-span-2">
            <Label htmlFor="location" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
              <MapPin className="w-3.5 h-3.5 text-muted-foreground" /> Location
            </Label>
            <Input
              id="location"
              placeholder="e.g. San Francisco, CA"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
            />
          </div>
        </CardContent>
      </Card>

      {/* About Me */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <AlignLeft className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">About Me</CardTitle>
              <CardDescription className="text-xs">Introduce yourself to recruiters</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6 space-y-2">
          <div className="flex justify-between items-center">
            <Label htmlFor="aboutMe" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Bio</Label>
            <span className={`text-xs font-semibold ${aboutMe.length > 500 ? 'text-destructive font-bold' : 'text-muted-foreground/80'}`}>
              {aboutMe.length}/500 characters
            </span>
          </div>
          <Textarea
            id="aboutMe"
            placeholder="Write a brief introduction about your career background, skills, and goals..."
            value={aboutMe}
            onChange={(e) => setAboutMe(e.target.value.slice(0, 500))}
            rows={5}
            className="bg-background/50 border-border/60 focus-visible:ring-primary/30 resize-y"
          />
        </CardContent>
      </Card>

      {/* Online Presence */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Globe className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Online Presence</CardTitle>
              <CardDescription className="text-xs">Your personal websites and social profiles</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-6 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="portfolioUrl" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
              <Globe className="w-3.5 h-3.5 text-muted-foreground" /> Portfolio URL
            </Label>
            <Input
              id="portfolioUrl"
              placeholder="https://yourportfolio.com"
              value={portfolioUrl}
              onChange={(e) => setPortfolioUrl(e.target.value)}
              className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="linkedInUrl" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
              <LinkedinIcon className="w-3.5 h-3.5 text-muted-foreground" /> LinkedIn URL
            </Label>
            <Input
              id="linkedInUrl"
              placeholder="linkedin.com/in/username"
              value={linkedInUrl}
              onChange={(e) => setLinkedInUrl(e.target.value)}
              className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="githubUrl" className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
              <GithubIcon className="w-3.5 h-3.5 text-muted-foreground" /> GitHub URL
            </Label>
            <Input
              id="githubUrl"
              placeholder="github.com/username"
              value={githubUrl}
              onChange={(e) => setGithubUrl(e.target.value)}
              className="bg-background/50 border-border/60 focus-visible:ring-primary/30"
            />
          </div>
        </CardContent>
      </Card>

      {/* Action Buttons */}
      <div className="flex justify-end gap-3 pt-2">
        <Button
          type="button"
          variant="outline"
          onClick={handleDiscard}
          disabled={!isDirty || isPending}
          className="border-border/60 hover:bg-muted/40 transition-all font-semibold rounded-lg"
        >
          Discard Changes
        </Button>
        <Button
          type="submit"
          disabled={!isDirty || isPending || firstName.trim() === '' || lastName.trim() === ''}
          className="bg-primary hover:bg-primary/95 transition-all text-primary-foreground font-semibold px-6 shadow-md shadow-primary/20 rounded-lg flex items-center gap-2"
        >
          {isPending ? 'Saving...' : 'Save Changes'}
        </Button>
      </div>
    </form>

      {/* Confirmation Modal */}
      <Dialog open={showConfirmModal} onOpenChange={setShowConfirmModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-destructive" />
              Discard Changes?
            </DialogTitle>
            <DialogDescription>
              You have unsaved changes in your personal information. Are you sure you want to discard them? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowConfirmModal(false)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={confirmDiscard}
            >
              Discard Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
