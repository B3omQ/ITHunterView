'use client';

import React, { useRef } from 'react';
import { Camera, MapPin, Loader2 } from 'lucide-react';
import { useUpdateVisibility, useUploadAvatar } from '@/hooks/useCandidateProfile';
import type { ProfileSummary } from '@/types/candidate.types';
import { Card } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';

interface ProfileHeaderProps {
  summary: ProfileSummary;
}

export function ProfileHeader({ summary }: ProfileHeaderProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { mutate: updateVisibility, isPending: isUpdatingVisibility } = useUpdateVisibility();
  const { mutate: uploadAvatar, isPending: isUploadingAvatar } = useUploadAvatar();

  const handleAvatarClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      uploadAvatar(file);
    }
  };

  const handleVisibilityChange = (checked: boolean) => {
    updateVisibility({ isVisibleToRecruiters: checked });
  };

  // Định dạng thời gian lưu gần nhất
  const formatLastSaved = (dateStr: string | null) => {
    if (!dateStr) return 'Not saved yet';
    try {
      const date = new Date(dateStr);
      return `Last saved: ${date.toLocaleDateString()} ${date.toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit',
      })}`;
    } catch {
      return 'Last saved recently';
    }
  };

  return (
    <Card className="relative overflow-hidden border border-border/40 bg-card/60 backdrop-blur-md p-6 md:p-8 rounded-2xl shadow-xl flex flex-col md:flex-row gap-6 md:gap-8 items-start md:items-center justify-between transition-all duration-300 hover:shadow-2xl">
      {/* Background Glow */}
      <div className="absolute top-0 right-0 w-80 h-80 bg-primary/5 rounded-full blur-3xl -z-10 pointer-events-none" />
      <div className="absolute bottom-0 left-0 w-60 h-60 bg-blue-500/5 rounded-full blur-3xl -z-10 pointer-events-none" />

      {/* Info Left */}
      <div className="flex flex-col sm:flex-row gap-5 items-center sm:items-start md:items-center w-full md:w-auto">
        {/* Avatar Area */}
        <div className="relative group cursor-pointer" onClick={handleAvatarClick}>
          <div className="w-24 h-24 sm:w-28 sm:h-28 rounded-full overflow-hidden border-4 border-background shadow-lg ring-2 ring-primary/20 transition-all duration-300 group-hover:ring-primary/40 relative">
            {summary.avatarUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={summary.avatarUrl}
                alt={summary.fullName}
                className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
              />
            ) : (
              <div className="w-full h-full bg-gradient-to-br from-primary/10 to-primary/20 flex items-center justify-center text-primary text-3xl font-bold uppercase">
                {summary.fullName.slice(0, 2)}
              </div>
            )}

            {/* Hover Mask */}
            <div className="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-300">
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
            accept="image/*"
            onChange={handleFileChange}
            disabled={isUploadingAvatar}
          />
        </div>

        {/* Text Info */}
        <div className="text-center sm:text-left space-y-1.5">
          <div className="flex flex-col sm:flex-row sm:items-center gap-2">
            <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight text-foreground">
              {summary.fullName}
            </h1>
          </div>

          <div className="flex flex-wrap items-center justify-center sm:justify-start gap-3 text-xs sm:text-sm text-muted-foreground/80">
            {summary.location && (
              <span className="flex items-center gap-1">
                <MapPin className="w-3.5 h-3.5 text-primary" />
                {summary.location}
              </span>
            )}
            <span className="inline-block w-1.5 h-1.5 rounded-full bg-muted-foreground/30" />
            <span className="font-mono text-muted-foreground/75">
              {formatLastSaved(summary.lastSavedAt)}
            </span>
          </div>
        </div>
      </div>

      {/* Stats Right */}
      <div className="flex flex-col sm:flex-row md:flex-col gap-6 items-stretch sm:items-center md:items-end justify-between w-full md:w-72 border-t md:border-t-0 pt-6 md:pt-0 border-border/20">
        {/* Visibility Toggle */}
        <div className="flex items-center justify-between sm:justify-start md:justify-end gap-3 w-full sm:w-auto md:w-full bg-muted/40 p-3 rounded-xl border border-border/10 backdrop-blur-sm">
          <div className="space-y-0.5">
            <Label className="text-xs sm:text-sm font-bold block">Visible to Recruiters</Label>
            <span className="text-[10px] sm:text-xs text-muted-foreground">
              Allow search by recruiters
            </span>
          </div>
          <div className="flex items-center">
            {isUpdatingVisibility && <Loader2 className="w-4 h-4 mr-2 animate-spin text-muted-foreground" />}
            <Switch
              checked={summary.isVisibleToRecruiters}
              onCheckedChange={handleVisibilityChange}
              disabled={isUpdatingVisibility}
            />
          </div>
        </div>

      </div>
    </Card>
  );
}
