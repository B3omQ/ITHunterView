'use client';

import React, { useState, useEffect } from 'react';
import {
  usePersonalInfo,
  useUpdateBasicInfo,
  useUpdateAboutMe,
  useUpdateSocialLinks,
} from '@/hooks/useCandidateProfile';
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
import { Globe, Mail, Phone, MapPin, AlignLeft, User, AlertTriangle, Edit2, X, Check } from 'lucide-react';
import { toast } from 'sonner';

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
  const { mutate: updateBasicInfo, isPending: isPendingBasic } = useUpdateBasicInfo();
  const { mutate: updateAboutMe, isPending: isPendingAbout } = useUpdateAboutMe();
  const { mutate: updateSocialLinks, isPending: isPendingPresence } = useUpdateSocialLinks();

  const isPending = isPendingBasic || isPendingAbout || isPendingPresence;

  // Edit Mode States
  const [isEditingBasic, setIsEditingBasic] = useState(false);
  const [isEditingAbout, setIsEditingAbout] = useState(false);
  const [isEditingPresence, setIsEditingPresence] = useState(false);

  // Local form states
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [location, setLocation] = useState('');
  
  const [aboutMe, setAboutMe] = useState('');
  
  const [portfolioUrl, setPortfolioUrl] = useState('');
  const [linkedInUrl, setLinkedInUrl] = useState('');
  const [githubUrl, setGithubUrl] = useState('');
  
  // Modal state
  const [discardTarget, setDiscardTarget] = useState<'basic' | 'about' | 'presence' | null>(null);

  // Sync form states with API response data
  const resetBasic = () => {
    if (info) {
      setFirstName(info.firstName || '');
      setLastName(info.lastName || '');
      setPhone(info.phone || '');
      setLocation(info.location || '');
    }
  };

  const resetAbout = () => {
    if (info) {
      setAboutMe(info.aboutMe || '');
    }
  };

  const resetPresence = () => {
    if (info) {
      setPortfolioUrl(info.portfolioUrl || '');
      setLinkedInUrl(info.linkedInUrl || '');
      setGithubUrl(info.githubUrl || '');
    }
  };

  const resetAll = () => {
    resetBasic();
    resetAbout();
    resetPresence();
  };

  useEffect(() => {
    resetAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [info]);

  const isBasicDirty =
    firstName !== (info?.firstName || '') ||
    lastName !== (info?.lastName || '') ||
    phone !== (info?.phone || '') ||
    location !== (info?.location || '');

  const isAboutDirty = aboutMe !== (info?.aboutMe || '');

  const isPresenceDirty =
    portfolioUrl !== (info?.portfolioUrl || '') ||
    linkedInUrl !== (info?.linkedInUrl || '') ||
    githubUrl !== (info?.githubUrl || '');

  const isAnyDirty = isBasicDirty || isAboutDirty || isPresenceDirty;

  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (isAnyDirty) {
        e.preventDefault();
        e.returnValue = '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [isAnyDirty]);

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

  // --- Handlers ---
  const handleSaveBasic = (e: React.FormEvent) => {
    e.preventDefault();
    if (!info) return;
    updateBasicInfo({
      firstName,
      lastName,
      phone: phone || null,
      location: location || null,
    }, {
      onSuccess: () => {
        toast.success('Basic information updated successfully');
        setIsEditingBasic(false);
      },
      onError: () => toast.error('Failed to update basic information. Please try again.')
    });
  };

  const handleSaveAbout = (e: React.FormEvent) => {
    e.preventDefault();
    if (!info) return;
    updateAboutMe({
      aboutMe: aboutMe || null,
    }, {
      onSuccess: () => {
        toast.success('Bio updated successfully');
        setIsEditingAbout(false);
      },
      onError: () => toast.error('Failed to update bio. Please try again.')
    });
  };

  const handleSavePresence = (e: React.FormEvent) => {
    e.preventDefault();
    if (!info) return;
    updateSocialLinks({
      portfolioUrl: portfolioUrl || null,
      linkedInUrl: linkedInUrl || null,
      githubUrl: githubUrl || null,
    }, {
      onSuccess: () => {
        toast.success('Online presence updated successfully');
        setIsEditingPresence(false);
      },
      onError: () => toast.error('Failed to update online presence. Please try again.')
    });
  };

  const requestCancelBasic = () => {
    if (isBasicDirty) setDiscardTarget('basic');
    else setIsEditingBasic(false);
  };

  const requestCancelAbout = () => {
    if (isAboutDirty) setDiscardTarget('about');
    else setIsEditingAbout(false);
  };

  const requestCancelPresence = () => {
    if (isPresenceDirty) setDiscardTarget('presence');
    else setIsEditingPresence(false);
  };

  const confirmDiscard = () => {
    if (discardTarget === 'basic') {
      resetBasic();
      setIsEditingBasic(false);
    } else if (discardTarget === 'about') {
      resetAbout();
      setIsEditingAbout(false);
    } else if (discardTarget === 'presence') {
      resetPresence();
      setIsEditingPresence(false);
    }
    setDiscardTarget(null);
  };

  return (
    <div className="space-y-6">
      {/* Basic Information */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4 flex flex-row items-center justify-between space-y-0">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <User className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Basic Information</CardTitle>
              <CardDescription className="text-xs">Your personal identification details</CardDescription>
            </div>
          </div>
          {!isEditingBasic && (
            <Button variant="ghost" size="sm" onClick={() => setIsEditingBasic(true)} className="text-muted-foreground hover:text-primary">
              <Edit2 className="w-4 h-4 mr-1.5" />
              Edit
            </Button>
          )}
        </CardHeader>
        
        {isEditingBasic ? (
          <form onSubmit={handleSaveBasic}>
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
                <Label className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
                  <Mail className="w-3.5 h-3.5 text-muted-foreground" /> Email Address
                </Label>
                <Input
                  value={info.email || ''}
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
            <div className="px-6 pb-6 pt-2 flex justify-end gap-3 bg-muted/20 border-t border-border/10">
              <Button type="button" variant="outline" size="sm" onClick={requestCancelBasic} disabled={isPending}>
                <X className="w-4 h-4 mr-1.5" /> Cancel
              </Button>
              <Button type="submit" size="sm" disabled={!isBasicDirty || isPending || firstName.trim() === '' || lastName.trim() === ''}>
                <Check className="w-4 h-4 mr-1.5" /> {isPending ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </form>
        ) : (
          <CardContent className="p-6 grid grid-cols-1 md:grid-cols-2 gap-y-6 gap-x-8 text-sm">
            <div className="space-y-1.5">
              <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80">Full Name</p>
              <p className="font-medium text-foreground">{info.firstName} {info.lastName}</p>
            </div>
            <div className="space-y-1.5">
              <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
                <Mail className="w-3.5 h-3.5" /> Email
              </p>
              <p className="font-medium text-foreground">{info.email || '-'}</p>
            </div>
            <div className="space-y-1.5">
              <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
                <Phone className="w-3.5 h-3.5" /> Phone
              </p>
              <p className="font-medium text-foreground">{info.phone || '-'}</p>
            </div>
            <div className="space-y-1.5">
              <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
                <MapPin className="w-3.5 h-3.5" /> Location
              </p>
              <p className="font-medium text-foreground">{info.location || '-'}</p>
            </div>
          </CardContent>
        )}
      </Card>

      {/* About Me */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4 flex flex-row items-center justify-between space-y-0">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <AlignLeft className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">About Me</CardTitle>
              <CardDescription className="text-xs">Introduce yourself to recruiters</CardDescription>
            </div>
          </div>
          {!isEditingAbout && (
            <Button variant="ghost" size="sm" onClick={() => setIsEditingAbout(true)} className="text-muted-foreground hover:text-primary">
              <Edit2 className="w-4 h-4 mr-1.5" />
              Edit
            </Button>
          )}
        </CardHeader>
        
        {isEditingAbout ? (
          <form onSubmit={handleSaveAbout}>
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
            <div className="px-6 pb-6 pt-2 flex justify-end gap-3 bg-muted/20 border-t border-border/10">
              <Button type="button" variant="outline" size="sm" onClick={requestCancelAbout} disabled={isPending}>
                <X className="w-4 h-4 mr-1.5" /> Cancel
              </Button>
              <Button type="submit" size="sm" disabled={!isAboutDirty || isPending}>
                <Check className="w-4 h-4 mr-1.5" /> {isPending ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </form>
        ) : (
          <CardContent className="p-6">
            {info.aboutMe ? (
              <p className="text-sm text-foreground/90 whitespace-pre-wrap leading-relaxed">{info.aboutMe}</p>
            ) : (
              <p className="text-sm text-muted-foreground italic">No introduction provided yet.</p>
            )}
          </CardContent>
        )}
      </Card>

      {/* Online Presence */}
      <Card className="border border-border/40 bg-card/60 backdrop-blur-md rounded-xl shadow-md overflow-hidden">
        <CardHeader className="border-b border-border/10 pb-4 flex flex-row items-center justify-between space-y-0">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Globe className="w-5 h-5" />
            </div>
            <div>
              <CardTitle className="text-lg font-bold">Online Presence</CardTitle>
              <CardDescription className="text-xs">Your personal websites and social profiles</CardDescription>
            </div>
          </div>
          {!isEditingPresence && (
            <Button variant="ghost" size="sm" onClick={() => setIsEditingPresence(true)} className="text-muted-foreground hover:text-primary">
              <Edit2 className="w-4 h-4 mr-1.5" />
              Edit
            </Button>
          )}
        </CardHeader>
        
        {isEditingPresence ? (
          <form onSubmit={handleSavePresence}>
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
            <div className="px-6 pb-6 pt-2 flex justify-end gap-3 bg-muted/20 border-t border-border/10">
              <Button type="button" variant="outline" size="sm" onClick={requestCancelPresence} disabled={isPending}>
                <X className="w-4 h-4 mr-1.5" /> Cancel
              </Button>
              <Button type="submit" size="sm" disabled={!isPresenceDirty || isPending}>
                <Check className="w-4 h-4 mr-1.5" /> {isPending ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </form>
        ) : (
          <CardContent className="p-6 grid grid-cols-1 md:grid-cols-3 gap-6 text-sm">
            <div className="space-y-1.5">
              <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
                <Globe className="w-3.5 h-3.5" /> Portfolio
              </p>
              {info.portfolioUrl ? (
                <a href={info.portfolioUrl} target="_blank" rel="noreferrer" className="font-medium text-primary hover:underline truncate block">
                  {info.portfolioUrl}
                </a>
              ) : <p className="text-muted-foreground">-</p>}
            </div>
            <div className="space-y-1.5">
              <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
                <LinkedinIcon className="w-3.5 h-3.5" /> LinkedIn
              </p>
              {info.linkedInUrl ? (
                <a href={info.linkedInUrl} target="_blank" rel="noreferrer" className="font-medium text-primary hover:underline truncate block">
                  {info.linkedInUrl}
                </a>
              ) : <p className="text-muted-foreground">-</p>}
            </div>
            <div className="space-y-1.5">
              <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground/80 flex items-center gap-1.5">
                <GithubIcon className="w-3.5 h-3.5" /> GitHub
              </p>
              {info.githubUrl ? (
                <a href={info.githubUrl} target="_blank" rel="noreferrer" className="font-medium text-primary hover:underline truncate block">
                  {info.githubUrl}
                </a>
              ) : <p className="text-muted-foreground">-</p>}
            </div>
          </CardContent>
        )}
      </Card>

      {/* Confirmation Modal */}
      <Dialog open={!!discardTarget} onOpenChange={(open) => !open && setDiscardTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-destructive" />
              Discard Changes?
            </DialogTitle>
            <DialogDescription>
              You have unsaved changes in this section. Are you sure you want to discard them? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDiscardTarget(null)}
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
    </div>
  );
}
