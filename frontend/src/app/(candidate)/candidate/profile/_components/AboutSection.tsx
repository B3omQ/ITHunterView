'use client';

import React, { useState, useEffect } from 'react';
import { usePersonalInfo, useUpdateAboutMe } from '@/hooks/useCandidateProfile';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { AlignLeft, Edit2, X, Check, Loader2 } from 'lucide-react';
import { toast } from 'sonner';

export function AboutSection() {
  const { data: info, isLoading } = usePersonalInfo();
  const { mutate: updateAboutMe, isPending } = useUpdateAboutMe();

  const [isEditing, setIsEditing] = useState(false);
  const [aboutMe, setAboutMe] = useState('');

  useEffect(() => {
    if (info && !isEditing) {
      setAboutMe(info.aboutMe || '');
    }
  }, [info, isEditing]);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    if (!info) return;
    
    updateAboutMe({
      aboutMe: aboutMe || null,
    }, {
      onSuccess: () => {
        toast.success('Bio updated successfully');
        setIsEditing(false);
      },
      onError: () => toast.error('Failed to update bio. Please try again.')
    });
  };

  const handleCancel = () => {
    setAboutMe(info?.aboutMe || '');
    setIsEditing(false);
  };

  if (isLoading) {
    return (
      <Card className="animate-pulse">
        <CardHeader className="border-b pb-4">
          <div className="h-6 w-32 bg-muted rounded"></div>
        </CardHeader>
        <CardContent className="p-6">
          <div className="space-y-2">
            <div className="h-4 w-full bg-muted rounded"></div>
            <div className="h-4 w-5/6 bg-muted rounded"></div>
            <div className="h-4 w-4/6 bg-muted rounded"></div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="w-full">
      <div className="flex flex-row items-center justify-between pb-3">
        <div className="flex items-center gap-3">
          <div>
            <h3 className="text-sm font-bold">About Me</h3>
          </div>
        </div>
        {!isEditing && (
          <Button variant="ghost" size="sm" onClick={() => setIsEditing(true)} className="text-muted-foreground hover:text-primary h-8 px-2 text-xs">
            <Edit2 className="w-3.5 h-3.5 mr-1.5" />
            Edit
          </Button>
        )}
      </div>
      
      {isEditing ? (
        <form onSubmit={handleSave} className="mt-2 border rounded-xl overflow-hidden bg-background/50">
          <div className="p-4 space-y-2">
            <div className="flex justify-between items-center">
              <Label htmlFor="aboutMe" className="text-[10px] font-bold uppercase tracking-wider text-muted-foreground/80">Bio</Label>
              <span className={`text-[10px] font-semibold ${aboutMe.length > 500 ? 'text-destructive font-bold' : 'text-muted-foreground/80'}`}>
                {aboutMe.length}/500
              </span>
            </div>
            <Textarea
              id="aboutMe"
              placeholder="Write a brief introduction about your career background, skills, and goals..."
              value={aboutMe}
              onChange={(e) => setAboutMe(e.target.value.slice(0, 500))}
              rows={4}
              className="bg-transparent border-none focus-visible:ring-0 resize-y p-0 text-sm shadow-none"
            />
          </div>
          <div className="px-4 py-3 flex justify-end gap-2 bg-muted/20 border-t border-border/10">
            <Button type="button" variant="outline" size="sm" onClick={handleCancel} disabled={isPending} className="h-8 text-xs">
              <X className="w-3.5 h-3.5 mr-1.5" /> Cancel
            </Button>
            <Button type="submit" size="sm" disabled={isPending || aboutMe === (info?.aboutMe || '')} className="h-8 text-xs">
              {isPending ? <Loader2 className="w-3.5 h-3.5 mr-1.5 animate-spin" /> : <Check className="w-3.5 h-3.5 mr-1.5" />} 
              {isPending ? 'Saving...' : 'Save'}
            </Button>
          </div>
        </form>
      ) : (
        <div className="pt-1">
          {info?.aboutMe ? (
            <p className="text-sm text-foreground/90 whitespace-pre-wrap leading-relaxed text-left">{info.aboutMe}</p>
          ) : (
            <div className="text-center py-4 border-2 border-dashed border-border rounded-xl bg-muted/10">
              <p className="text-xs text-muted-foreground mb-2">No introduction provided yet.</p>
              <Button onClick={() => setIsEditing(true)} variant="outline" size="sm" className="font-semibold text-xs h-8">
                <Edit2 className="w-3.5 h-3.5 mr-2" /> Add Bio
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
