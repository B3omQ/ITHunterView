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
      {/* Top Actions: Visibility Toggle & Edit */}
      <div className="absolute top-4 right-4 z-20 flex flex-row items-center gap-2">
        <Button variant="outline" size="sm" onClick={() => setIsEditModalOpen(true)} className="flex items-center gap-1.5 bg-background/80 backdrop-blur shadow-sm hover:bg-muted text-xs h-8">
          <Edit2 className="w-3.5 h-3.5" /> Edit Profile
        </Button>
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
            Visible
          </Label>
        </div>
      </div>

      <div className="flex flex-col items-center mt-6">
        {/* Avatar Area */}
        <div className="relative group cursor-pointer shrink-0 mb-4 z-10" onClick={handleAvatarClick}>
          <div className="w-28 h-28 sm:w-32 sm:h-32 rounded-full overflow-hidden border-2 border-border relative bg-muted shadow-sm flex items-center justify-center">
            {summary.avatarUrl && !imageError ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={summary.avatarUrl}
                alt={summary.fullName}
                onError={() => setImageError(true)}
                className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110 text-transparent"
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-muted-foreground text-4xl font-bold uppercase">
                {(summary?.fullName || 'NA').slice(0, 2).toUpperCase()}
              </div>
            )}

            <div
              className={`absolute inset-0 z-20 flex items-center justify-center transition-opacity duration-300 ${
                isUploadingAvatar ? 'opacity-100 bg-black/60' : 'opacity-0 group-hover:opacity-100 bg-black/40'
              }`}
            >
              {isUploadingAvatar ? (
                <Loader2 className="w-6 h-6 text-white animate-spin" />
              ) : (
                <Camera className="w-6 h-6 text-white" />
              )}
            </div>
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
      <Dialog open={isEditModalOpen} onOpenChange={setIsEditModalOpen}>
        <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit Profile Information</DialogTitle>
            <DialogDescription>
              Update your contact details and online presence.
            </DialogDescription>
          </DialogHeader>
          
          <form onSubmit={handleSaveProfile} className="space-y-6 pt-4">
            <div className="space-y-4">
              <h3 className="text-sm font-semibold text-foreground uppercase tracking-wider">Contact Info</h3>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="firstName">First Name</Label>
                  <Input id="firstName" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="lastName">Last Name</Label>
                  <Input id="lastName" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
                </div>
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="phone">Phone Number</Label>
                <Input id="phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
              </div>

              <div className="space-y-2">
                <Label htmlFor="locationType">Location</Label>
                <div className="flex gap-2">
                  <LocationCombobox
                    value={locationType}
                    onChange={(val) => {
                      setLocationType(val)
                      if (val !== "Other") setLocation(val)
                      else setLocation("")
                    }}
                    className={locationType === "Other" ? "w-1/3" : "w-full"}
                  />
                  {locationType === "Other" && (
                    <Input
                      id="location"
                      placeholder="e.g. Can Tho"
                      value={location}
                      onChange={(e) => setLocation(e.target.value)}
                      className="flex-1"
                    />
                  )}
                </div>
              </div>
            </div>

            <div className="space-y-4">
              <h3 className="text-sm font-semibold text-foreground uppercase tracking-wider">Online Presence</h3>
              
              <div className="space-y-2">
                <Label htmlFor="linkedInUrl">LinkedIn URL</Label>
                <div className="relative">
                  <LinkedinIcon className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input id="linkedInUrl" placeholder="linkedin.com/in/username" value={linkedInUrl} onChange={(e) => setLinkedInUrl(e.target.value)} className="pl-9" />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="githubUrl">GitHub URL</Label>
                <div className="relative">
                  <GithubIcon className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input id="githubUrl" placeholder="github.com/username" value={githubUrl} onChange={(e) => setGithubUrl(e.target.value)} className="pl-9" />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="portfolioUrl">Portfolio URL</Label>
                <div className="relative">
                  <Globe className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input id="portfolioUrl" placeholder="yourportfolio.com" value={portfolioUrl} onChange={(e) => setPortfolioUrl(e.target.value)} className="pl-9" />
                </div>
              </div>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsEditModalOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={isPending || !firstName || !lastName}>
                {isPending && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                Save Changes
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
