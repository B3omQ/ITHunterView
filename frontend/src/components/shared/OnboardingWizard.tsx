'use client';

import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useUpdateOnboardingProfile, usePersonalInfo } from '@/hooks/useCandidateProfile';
import { LocationCombobox } from './LocationCombobox';
import { toast } from 'sonner';
import { CheckCircle2, ChevronRight, User, Phone } from 'lucide-react';

interface OnboardingWizardProps {
  missingFields: string[];
}

export function OnboardingWizard({ missingFields }: OnboardingWizardProps) {
  const { data: info } = usePersonalInfo();
  const { mutate: updateProfile, isPending } = useUpdateOnboardingProfile();

  const [step, setStep] = useState(1);
  const [firstName, setFirstName] = useState(info?.firstName || '');
  const [lastName, setLastName] = useState(info?.lastName || '');
  const [phone, setPhone] = useState(info?.phone || '');
  
  const [locationType, setLocationType] = useState('Hồ Chí Minh');
  const [locationStr, setLocationStr] = useState(info?.location || '');

  const totalSteps = 2;

  // Sync initial data once loaded
  React.useEffect(() => {
    if (info) {
      if (info.firstName && !firstName) setFirstName(info.firstName);
      if (info.lastName && !lastName) setLastName(info.lastName);
      if (info.phone && !phone) setPhone(info.phone);
      if (info.location && !locationStr) {
        setLocationStr(info.location);
        const standardLocations = ["TP Hồ Chí Minh", "Hà Nội", "Đà Nẵng"];
        // In PersonalInfoTab we check against VIETNAM_PROVINCES, here we simplify slightly
        if (!standardLocations.includes(info.location) && info.location !== 'Hồ Chí Minh') {
          setLocationType("Other");
        } else {
          setLocationType(info.location);
        }
      }
    }
  }, [info]);

  const handleNext = () => {
    if (step === 1) {
      if (!firstName.trim() || !lastName.trim()) {
        toast.error('Please enter both first and last name');
        return;
      }
      // Partial save
      updateProfile(
        { firstName, lastName, phone: phone || '', location: locationStr || '' },
        {
          onSuccess: () => setStep(2),
        }
      );
    }
  };

  const handleSubmit = () => {
    if (!phone.trim()) {
      toast.error('Please enter a phone number');
      return;
    }
    if (!locationStr.trim()) {
      toast.error('Please select or enter a location');
      return;
    }

    updateProfile(
      { firstName, lastName, phone, location: locationStr },
      {
        onSuccess: () => {
          toast.success('Profile updated successfully!');
        },
        onError: () => {
          toast.error('An error occurred, please try again.');
        }
      }
    );
  };

  return (
    <Dialog open={true} onOpenChange={() => {}}>
      {/* Remove the default close button using CSS or by not rendering it inside DialogPrimitive.Close if possible, 
          but shadcn Dialog handles onOpenChange. Passing empty function prevents ESC or outside click to close. */}
      <DialogContent className="sm:max-w-[450px] p-0 overflow-hidden [&>button]:hidden">
        <div className="p-6 border-b">
          <DialogHeader>
            <DialogTitle className="text-xl font-bold">
              Complete your profile
            </DialogTitle>
            <DialogDescription className="text-sm mt-2">
              Help us personalize your job search and recommendations.
            </DialogDescription>
          </DialogHeader>
          
          {/* Progress bar */}
          <div className="mt-6 flex items-center gap-2">
            <div className={`h-1.5 flex-1 rounded-full ${step >= 1 ? 'bg-primary' : 'bg-secondary'}`} />
            <div className={`h-1.5 flex-1 rounded-full ${step >= 2 ? 'bg-primary' : 'bg-secondary'}`} />
          </div>
          <p className="text-xs font-medium text-muted-foreground mt-2 text-right">
            Step {step} of {totalSteps}
          </p>
        </div>

        <div className="p-6">
          {step === 1 && (
            <div className="space-y-4">
              <div className="flex items-center gap-2 mb-4 text-foreground">
                <User className="w-5 h-5 text-muted-foreground" />
                <h3 className="font-semibold text-sm uppercase tracking-wider">Personal Information</h3>
              </div>
              <div className="space-y-2">
                <Label htmlFor="firstName">First Name <span className="text-destructive">*</span></Label>
                <Input
                  id="firstName"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Enter first name..."
                  autoFocus
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Last Name <span className="text-destructive">*</span></Label>
                <Input
                  id="lastName"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Enter last name..."
                />
              </div>
            </div>
          )}

          {step === 2 && (
            <div className="space-y-4">
              <div className="flex items-center gap-2 mb-4 text-foreground">
                <Phone className="w-5 h-5 text-muted-foreground" />
                <h3 className="font-semibold text-sm uppercase tracking-wider">Contact Information</h3>
              </div>
              <div className="space-y-2">
                <Label htmlFor="phone">Phone Number <span className="text-destructive">*</span></Label>
                <Input
                  id="phone"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  placeholder="09xx xxx xxx"
                  autoFocus
                />
              </div>
              <div className="space-y-2">
                <Label>Location <span className="text-destructive">*</span></Label>
                <div className="flex flex-col gap-2">
                  <LocationCombobox
                    value={locationType}
                    onChange={(val) => {
                      setLocationType(val);
                      if (val !== 'Other') {
                        setLocationStr(val);
                      } else {
                        setLocationStr('');
                      }
                    }}
                    className="w-full"
                  />
                  {locationType === 'Other' && (
                    <Input
                      placeholder="Enter city/province..."
                      value={locationStr}
                      onChange={(e) => setLocationStr(e.target.value)}
                    />
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="p-6 pt-0 flex justify-end gap-3">
          {step === 1 ? (
            <Button onClick={handleNext} disabled={isPending || !firstName.trim() || !lastName.trim()} className="w-full sm:w-auto">
              Continue <ChevronRight className="w-4 h-4 ml-1.5" />
            </Button>
          ) : (
            <Button onClick={handleSubmit} disabled={isPending || !phone.trim() || !locationStr.trim()} className="w-full sm:w-auto">
              {isPending ? 'Saving...' : 'Complete'} <CheckCircle2 className="w-4 h-4 ml-1.5" />
            </Button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
