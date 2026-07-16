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
        <Button 
          variant="ghost" 
          size="sm" 
          onClick={() => setIsEditing(true)} 
          className={`text-muted-foreground hover:text-primary h-8 px-2 text-xs ${isEditing ? 'invisible' : ''}`}
        >
          <Edit2 className="w-3.5 h-3.5 mr-1.5" />
          Edit
        </Button>
      </div>
      
      {isEditing ? (
        <form onSubmit={handleSave} className="-mt-[9px] -mx-3">
          <div className="relative border border-border/40 rounded-xl overflow-hidden bg-muted/10 focus-within:bg-muted/20 focus-within:border-primary/40 transition-colors">
            <Textarea
              id="aboutMe"
              placeholder="Write a brief introduction about your career background, skills, and goals..."
              value={aboutMe}
              onChange={(e) => setAboutMe(e.target.value.slice(0, 500))}
              rows={Math.max(3, aboutMe.split('\n').length)}
              autoFocus
              className="bg-transparent border-none focus-visible:ring-0 resize-none p-3 text-sm shadow-none font-medium leading-relaxed min-h-[80px]"
            />
            <div className="flex justify-between items-center p-2 pt-0 bg-transparent">
              <span className={`text-[10px] font-medium px-1 ${aboutMe.length > 500 ? 'text-destructive font-bold' : 'text-muted-foreground/60'}`}>
                {aboutMe.length}/500
              </span>
              <div className="flex gap-1.5">
                <Button type="button" variant="ghost" size="sm" onClick={handleCancel} disabled={isPending} className="h-7 text-xs hover:bg-muted font-medium">
                  Cancel
                </Button>
                <Button type="submit" size="sm" disabled={isPending || aboutMe === (info?.aboutMe || '')} className="h-7 text-xs font-medium px-3">
                  {isPending && <Loader2 className="w-3 h-3 mr-1.5 animate-spin" />}
                  Save
                </Button>
              </div>
            </div>
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
