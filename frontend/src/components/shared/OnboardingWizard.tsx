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
        toast.error('Vui lòng nhập đầy đủ Họ và Tên');
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
      toast.error('Vui lòng nhập số điện thoại');
      return;
    }
    if (!locationStr.trim()) {
      toast.error('Vui lòng chọn hoặc nhập địa điểm');
      return;
    }

    updateProfile(
      { firstName, lastName, phone, location: locationStr },
      {
        onSuccess: () => {
          toast.success('Cập nhật hồ sơ thành công!');
        },
        onError: () => {
          toast.error('Đã xảy ra lỗi, vui lòng thử lại.');
        }
      }
    );
  };

  return (
    <Dialog open={true} onOpenChange={() => {}}>
      {/* Remove the default close button using CSS or by not rendering it inside DialogPrimitive.Close if possible, 
          but shadcn Dialog handles onOpenChange. Passing empty function prevents ESC or outside click to close. */}
      <DialogContent className="sm:max-w-[450px] p-0 overflow-hidden [&>button]:hidden">
        <div className="bg-primary/5 p-6 border-b border-border/10">
          <DialogHeader>
            <DialogTitle className="text-xl font-bold text-foreground">
              Hoàn thiện hồ sơ
            </DialogTitle>
            <DialogDescription className="text-sm mt-2">
              Giúp chúng tôi cá nhân hóa trải nghiệm tìm việc và gợi ý phù hợp nhất cho bạn.
            </DialogDescription>
          </DialogHeader>
          
          {/* Progress bar */}
          <div className="mt-6 flex items-center gap-2">
            <div className={`h-1.5 flex-1 rounded-full ${step >= 1 ? 'bg-primary' : 'bg-primary/20'}`} />
            <div className={`h-1.5 flex-1 rounded-full ${step >= 2 ? 'bg-primary' : 'bg-primary/20'}`} />
          </div>
          <p className="text-xs font-medium text-muted-foreground mt-2 text-right">
            Bước {step} / {totalSteps}
          </p>
        </div>

        <div className="p-6">
          {step === 1 && (
            <div className="space-y-4 animate-in fade-in slide-in-from-right-4 duration-300">
              <div className="flex items-center gap-2 mb-4 text-primary">
                <User className="w-5 h-5" />
                <h3 className="font-semibold text-sm uppercase tracking-wider">Thông tin cá nhân</h3>
              </div>
              <div className="space-y-2">
                <Label htmlFor="firstName">Tên <span className="text-destructive">*</span></Label>
                <Input
                  id="firstName"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Nhập tên..."
                  autoFocus
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Họ và tên đệm <span className="text-destructive">*</span></Label>
                <Input
                  id="lastName"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Nhập họ..."
                />
              </div>
            </div>
          )}

          {step === 2 && (
            <div className="space-y-4 animate-in fade-in slide-in-from-right-4 duration-300">
              <div className="flex items-center gap-2 mb-4 text-primary">
                <Phone className="w-5 h-5" />
                <h3 className="font-semibold text-sm uppercase tracking-wider">Thông tin liên lạc</h3>
              </div>
              <div className="space-y-2">
                <Label htmlFor="phone">Số điện thoại <span className="text-destructive">*</span></Label>
                <Input
                  id="phone"
                  value={phone}
                  onChange={(e) => setPhone(e.target.value)}
                  placeholder="09xx xxx xxx"
                  autoFocus
                />
              </div>
              <div className="space-y-2">
                <Label>Địa điểm làm việc <span className="text-destructive">*</span></Label>
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
                      placeholder="Nhập tỉnh/thành phố..."
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
              Tiếp tục <ChevronRight className="w-4 h-4 ml-1.5" />
            </Button>
          ) : (
            <Button onClick={handleSubmit} disabled={isPending || !phone.trim() || !locationStr.trim()} className="w-full sm:w-auto">
              {isPending ? 'Đang lưu...' : 'Hoàn thành'} <CheckCircle2 className="w-4 h-4 ml-1.5" />
            </Button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
