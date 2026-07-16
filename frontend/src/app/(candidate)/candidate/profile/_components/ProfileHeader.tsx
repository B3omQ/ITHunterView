'use client';

import React, { useRef, useState, useEffect } from 'react';
import { Camera, MapPin, Loader2, AlertCircle, Edit2, Mail, Phone, Globe, Check, X } from 'lucide-react';
import { useUpdateVisibility, useUploadAvatar, usePersonalInfo, useUpdateBasicInfo, useUpdateSocialLinks } from '@/hooks/useCandidateProfile';
import type { ProfileSummary } from '@/types/candidate.types';
import { Card } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { toast } from 'sonner';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { LocationCombobox } from '@/components/shared/LocationCombobox';
import { VIETNAM_PROVINCES } from '@/lib/job-constants';
import { cn } from '@/lib/utils';
import { AboutSection } from './AboutSection';

const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
const MAX_IMAGE_SIZE_BYTES = 3 * 1024 * 1024; // 3MB

const LinkedinIcon = (props: React.SVGProps<SVGSVGElement>) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z" />
    <rect width="4" height="12" x="2" y="9" />
    <circle cx="4" cy="4" r="2" />
  </svg>
);

const GithubIcon = (props: React.SVGProps<SVGSVGElement>) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>
    <path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4" />
    <path d="M9 18c-4.51 2-5-2-7-2" />
  </svg>
);

interface ProfileHeaderProps {
  summary: ProfileSummary;
}

export function ProfileHeader({ summary }: ProfileHeaderProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isTurnOffModalOpen, setIsTurnOffModalOpen] = useState(false);
  const { mutate: updateVisibility, isPending: isUpdatingVisibility } = useUpdateVisibility();
  const { mutate: uploadAvatar, isPending: isUploadingAvatar } = useUploadAvatar();
  
  const { data: info } = usePersonalInfo();
  const { mutate: updateBasicInfo, isPending: isPendingBasic } = useUpdateBasicInfo();
  const { mutate: updateSocialLinks, isPending: isPendingPresence } = useUpdateSocialLinks();

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [imageError, setImageError] = useState(false);

  // Form states
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [location, setLocation] = useState('');
  const [locationType, setLocationType] = useState('Hồ Chí Minh');
  const [portfolioUrl, setPortfolioUrl] = useState('');
  const [linkedInUrl, setLinkedInUrl] = useState('');
  const [githubUrl, setGithubUrl] = useState('');

  const isPending = isPendingBasic || isPendingPresence;

  useEffect(() => {
    setImageError(false);
  }, [summary.avatarUrl]);

  useEffect(() => {
    if (info && isEditModalOpen) {
      setFirstName(info.firstName || '');
      setLastName(info.lastName || '');
      setPhone(info.phone || '');
      setLocation(info.location || '');
      
      const standardLocations = ["TP Hồ Chí Minh", "Hà Nội", "Đà Nẵng", ...VIETNAM_PROVINCES]
      if (info.location) {
        if (standardLocations.includes(info.location)) {
          setLocationType(info.location)
        } else {
          setLocationType("Other")
        }
      } else {
        setLocationType("TP Hồ Chí Minh");
      }

      setPortfolioUrl(info.portfolioUrl || '');
      setLinkedInUrl(info.linkedInUrl || '');
      setGithubUrl(info.githubUrl || '');
    }
  }, [info, isEditModalOpen]);

  const handleAvatarClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
        toast.error('Chỉ chấp nhận ảnh định dạng JPG, JPEG, PNG hoặc WebP.');
        if (fileInputRef.current) fileInputRef.current.value = '';
        return;
      }

      if (file.size > MAX_IMAGE_SIZE_BYTES) {
        toast.error('Ảnh không được vượt quá 3MB.');
        if (fileInputRef.current) fileInputRef.current.value = '';
        return;
      }

      uploadAvatar(file, {
        onSuccess: () => {
          toast.success('Avatar uploaded successfully');
          if (fileInputRef.current) fileInputRef.current.value = '';
        },
        onError: () => {
          toast.error('Failed to upload avatar, please try again');
          if (fileInputRef.current) fileInputRef.current.value = '';
        }
      });
    }
  };

  const handleVisibilityChange = (checked: boolean) => {
    if (!checked) {
      setIsTurnOffModalOpen(true);
    } else {
      updateVisibility({ isVisibleToRecruiters: true });
    }
  };

  const confirmTurnOff = () => {
    updateVisibility({ isVisibleToRecruiters: false }, {
      onSuccess: () => {
        setIsTurnOffModalOpen(false);
        toast.success('Profile is now hidden from recruiters');
      },
      onError: () => {
        setIsTurnOffModalOpen(false);
        toast.error('An error occurred, please try again');
      }
    });
  };

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!info) return;
    
    let basicSuccess = false;
    let presenceSuccess = false;

    updateBasicInfo({
      firstName,
      lastName,
      phone: phone || null,
      location: location || null,
    }, {
      onSuccess: () => {
        basicSuccess = true;
        checkAndClose();
      },
      onError: () => toast.error('Failed to update basic information.')
    });

    updateSocialLinks({
      portfolioUrl: portfolioUrl || null,
      linkedInUrl: linkedInUrl || null,
      githubUrl: githubUrl || null,
    }, {
      onSuccess: () => {
        presenceSuccess = true;
        checkAndClose();
      },
      onError: () => toast.error('Failed to update online presence.')
    });

    const checkAndClose = () => {
      if (basicSuccess && presenceSuccess) {
        toast.success('Profile updated successfully');
        setIsEditModalOpen(false);
      }
    };
  };

  return (
    <Card className="p-6 md:p-8 flex flex-col gap-6 relative shadow-md overflow-hidden bg-card/60 backdrop-blur-md">
      {/* Top Left: Visibility Toggle */}
      <div className="absolute top-4 left-4 z-20">
        <div className="flex items-center gap-2 bg-background/80 backdrop-blur px-2.5 py-1.5 rounded-lg border border-border/50 shadow-sm">
          <Label className="cursor-pointer flex items-center gap-2 text-xs text-muted-foreground font-medium">
            {isUpdatingVisibility ? (
              <Loader2 className="w-3.5 h-3.5 animate-spin" />
            ) : (
              <Switch
                checked={summary.isVisibleToRecruiters}
                onCheckedChange={handleVisibilityChange}
                className="scale-75 data-[state=checked]:bg-primary m-0"
              />
            )}
            Visible to recruiters
          </Label>
        </div>
      </div>

      {/* Top Right: Edit Action */}
      <div className="absolute top-4 right-4 z-20">
        <Button variant="ghost" size="icon" onClick={() => setIsEditModalOpen(true)} className="text-muted-foreground hover:text-primary w-8 h-8 rounded-full transition-colors">
          <Edit2 className="w-4 h-4" />
        </Button>
      </div>

      <div className="flex flex-col items-center mt-6">
        {/* Avatar Area */}
        <div className="relative shrink-0 mb-4 z-10">
          <div className="w-28 h-28 sm:w-32 sm:h-32 rounded-full overflow-hidden border-2 border-border relative bg-muted shadow-sm flex items-center justify-center">
            {summary.avatarUrl && !imageError ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={summary.avatarUrl}
                alt={summary.fullName}
                onError={() => setImageError(true)}
                className="w-full h-full object-cover text-transparent"
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-muted-foreground text-4xl font-bold uppercase">
                {(summary?.fullName || 'NA').slice(0, 2).toUpperCase()}
              </div>
            )}

            {isUploadingAvatar && (
              <div className="absolute inset-0 z-20 flex items-center justify-center bg-black/60">
                <Loader2 className="w-6 h-6 text-white animate-spin" />
              </div>
            )}
          </div>
          
          <input
            type="file"
            ref={fileInputRef}
            className="hidden"
            accept="image/jpeg,image/png,image/webp"
            onChange={handleFileChange}
            disabled={isUploadingAvatar}
          />
        </div>

        {/* Text Info */}
        <div className="w-full space-y-5 text-center">
          <div className="space-y-1.5">
            <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight text-foreground">
              {summary.fullName}
            </h1>
            
            <div className="flex flex-col items-center gap-2 text-sm text-muted-foreground">
              {info?.email && (
                <span className="flex items-center gap-1.5 font-medium">
                  <Mail className="w-4 h-4 text-primary/70" />
                  {info.email}
                </span>
              )}
              {info?.phone && (
                <span className="flex items-center gap-1.5 font-medium">
                  <Phone className="w-4 h-4 text-primary/70" />
                  {info.phone}
                </span>
              )}
              {summary.location && (
                <span className="flex items-center gap-1.5 font-medium">
                  <MapPin className="w-4 h-4 text-primary/70" />
                  {summary.location}
                </span>
              )}
            </div>
          </div>

          {/* Social Links */}
          <div className="flex flex-col items-center gap-3 pt-2 border-t border-border/40">
            {info?.linkedInUrl && (
              <a href={info.linkedInUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1.5 text-muted-foreground hover:text-blue-600 dark:hover:text-blue-400 text-sm font-medium transition-colors w-max">
                <LinkedinIcon className="w-4 h-4" /> <span className="hover:underline">LinkedIn</span>
              </a>
            )}
            {info?.githubUrl && (
              <a href={info.githubUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1.5 text-muted-foreground hover:text-foreground text-sm font-medium transition-colors w-max">
                <GithubIcon className="w-4 h-4" /> <span className="hover:underline">GitHub</span>
              </a>
            )}
            {info?.portfolioUrl && (
              <a href={info.portfolioUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1.5 text-muted-foreground hover:text-emerald-600 dark:hover:text-emerald-400 text-sm font-medium transition-colors w-max">
                <Globe className="w-4 h-4" /> <span className="hover:underline">Portfolio</span>
              </a>
            )}
            {!info?.linkedInUrl && !info?.githubUrl && !info?.portfolioUrl && (
              <p className="text-xs text-muted-foreground italic">No online presence added.</p>
            )}
          </div>
        </div>
      </div>

      {/* About Me Section integrated into Header Widget */}
      <div className="w-full mt-2 pt-6 border-t border-border/40">
        <AboutSection />
      </div>

      {/* Visibility Toggle Modal */}
      <Dialog open={isTurnOffModalOpen} onOpenChange={setIsTurnOffModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-destructive">
              <AlertCircle className="w-5 h-5" />
              Hide profile from recruiters?
            </DialogTitle>
            <DialogDescription className="pt-2">
              Recruiters will no longer be able to find your profile in the search system if you turn this off. Are you sure?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 sm:gap-0 mt-4">
            <Button variant="outline" onClick={() => setIsTurnOffModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={confirmTurnOff} disabled={isUpdatingVisibility}>
              {isUpdatingVisibility && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
              Confirm
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Profile Modal */}
      <Dialog disablePointerDismissal open={isEditModalOpen} onOpenChange={setIsEditModalOpen}>
        <DialogContent className="sm:max-w-[750px] p-0 overflow-hidden gap-0 bg-background border-none shadow-xl rounded-xl">
          <DialogHeader className="px-6 py-4 border-b border-border/40 bg-card/50 backdrop-blur-sm">
            <DialogTitle className="text-xl font-extrabold tracking-tight">Personal details</DialogTitle>
          </DialogHeader>
          
          <form onSubmit={handleSaveProfile} className="flex flex-col">
            <div className="grid grid-cols-1 sm:grid-cols-[220px_1fr] gap-8 p-6 max-h-[75vh] overflow-y-auto">
              
              {/* Left Column - Avatar */}
              <div className="flex flex-col items-center gap-4">
                <div className="relative">
                  <div className="w-36 h-36 rounded-full overflow-hidden border border-border/50 relative bg-primary/5 flex items-center justify-center shadow-sm">
                    {summary.avatarUrl && !imageError ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={summary.avatarUrl}
                        alt={summary.fullName}
                        onError={() => setImageError(true)}
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center text-primary/40 text-5xl font-bold uppercase">
                        {(summary?.fullName || 'NA').slice(0, 2).toUpperCase()}
                      </div>
                    )}
                    
                    {isUploadingAvatar && (
                      <div className="absolute inset-0 z-20 flex items-center justify-center bg-black/60">
                        <Loader2 className="w-6 h-6 text-white animate-spin" />
                      </div>
                    )}
                  </div>
                </div>
                <Button type="button" variant="ghost" size="sm" onClick={handleAvatarClick} disabled={isUploadingAvatar} className="text-primary font-bold hover:text-primary hover:bg-primary/10 gap-1.5 h-8">
                  <Camera className="w-4 h-4" /> {isUploadingAvatar ? 'Uploading...' : 'Edit Avatar'}
                </Button>
              </div>

              {/* Right Column - Inputs */}
              <div className="space-y-5">
                
                <div className="grid grid-cols-2 gap-4">
                  <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                    <Label htmlFor="firstName" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">First name <span className="text-destructive">*</span></Label>
                    <input id="firstName" value={firstName} onChange={(e) => setFirstName(e.target.value)} required className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50" placeholder="e.g. Tra" />
                  </div>
                  <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                    <Label htmlFor="lastName" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Last name <span className="text-destructive">*</span></Label>
                    <input id="lastName" value={lastName} onChange={(e) => setLastName(e.target.value)} required className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50" placeholder="e.g. Pham" />
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="border border-border/60 rounded-lg px-3 py-1.5 bg-muted/30 opacity-70 cursor-not-allowed shadow-sm">
                    <Label className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Email address</Label>
                    <input value={info?.email || ''} disabled className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground cursor-not-allowed truncate" />
                  </div>
                  <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                    <Label htmlFor="phone" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Phone number</Label>
                    <input id="phone" value={phone} onChange={(e) => setPhone(e.target.value)} className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50" placeholder="e.g. 0947852588" />
                  </div>
                </div>

                <div className="flex gap-4">
                  <div className="flex-1 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                    <Label className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Current Location <span className="text-destructive">*</span></Label>
                    <LocationCombobox
                      value={locationType}
                      onChange={(val) => {
                        setLocationType(val)
                        if (val !== "Other") setLocation(val)
                        else setLocation("")
                      }}
                      className="w-full h-auto min-h-[20px] p-0 border-none bg-transparent hover:bg-transparent shadow-none outline-none focus:ring-0 text-sm font-medium justify-between"
                    />
                  </div>
                  {locationType === "Other" && (
                    <div className="flex-1 border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                      <Label htmlFor="location" className="text-[11px] text-muted-foreground font-semibold block mb-0.5">Specific address</Label>
                      <input
                        id="location"
                        placeholder="e.g. 123 Main St..."
                        value={location}
                        onChange={(e) => setLocation(e.target.value)}
                        className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50"
                      />
                    </div>
                  )}
                </div>

                <div className="space-y-3 pt-4 border-t border-border/30">
                  <h4 className="text-[11px] text-muted-foreground font-bold uppercase tracking-wider ml-1">Personal Links</h4>
                  
                  <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                    <Label htmlFor="linkedInUrl" className="text-[11px] text-muted-foreground font-semibold flex items-center gap-1.5 mb-0.5">
                      <LinkedinIcon className="w-3 h-3" /> LinkedIn URL
                    </Label>
                    <input id="linkedInUrl" value={linkedInUrl} onChange={(e) => setLinkedInUrl(e.target.value)} className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50" placeholder="https://linkedin.com/in/username" />
                  </div>
                  
                  <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                    <Label htmlFor="portfolioUrl" className="text-[11px] text-muted-foreground font-semibold flex items-center gap-1.5 mb-0.5">
                      <Globe className="w-3 h-3" /> Portfolio URL
                    </Label>
                    <input id="portfolioUrl" value={portfolioUrl} onChange={(e) => setPortfolioUrl(e.target.value)} className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50" placeholder="https://yourportfolio.com" />
                  </div>
                  
                  <div className="border border-border/60 rounded-lg px-3 py-1.5 focus-within:border-primary focus-within:ring-1 focus-within:ring-primary/30 transition-all bg-card shadow-sm">
                    <Label htmlFor="githubUrl" className="text-[11px] text-muted-foreground font-semibold flex items-center gap-1.5 mb-0.5">
                      <GithubIcon className="w-3 h-3" /> GitHub URL
                    </Label>
                    <input id="githubUrl" value={githubUrl} onChange={(e) => setGithubUrl(e.target.value)} className="w-full bg-transparent border-none outline-none focus:!outline-none focus:!ring-0 focus:!border-transparent focus:!shadow-none p-0 text-sm font-medium text-foreground placeholder:text-muted-foreground/50" placeholder="https://github.com/username" />
                  </div>
                </div>

              </div>
            </div>

            <DialogFooter className="px-6 pt-4 pb-6 border-t border-border/40 bg-muted/10 sm:justify-end gap-2">
              <Button type="button" variant="ghost" onClick={() => setIsEditModalOpen(false)} className="h-10 px-6 font-semibold hover:bg-muted text-muted-foreground">
                Cancel
              </Button>
              <Button type="submit" disabled={isPending || !firstName || !lastName} className="h-10 px-8 font-bold shadow-sm">
                {isPending && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                Save
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
