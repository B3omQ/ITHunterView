'use client';

import { useState, useEffect } from 'react';
import { useCoinConfig, useUpdateCoinConfig } from '@/hooks/useSubscription';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';
import type { CoinPackageDto, UpdateCoinConfigDto } from '@/types/subscription.types';

interface PackageRowProps {
  pkg: CoinPackageDto;
  index: number;
  onUpdate: (index: number, field: keyof CoinPackageDto, value: any) => void;
  onRemove: (index: number) => void;
}

function PackageRow({ pkg, index, onUpdate, onRemove }: PackageRowProps) {
  const t = useTranslations('AdminSubscriptions');
  const [localName, setLocalName] = useState(pkg.name);
  const [localCoins, setLocalCoins] = useState(pkg.coins);
  const [localPrice, setLocalPrice] = useState(pkg.price);

  // Sync state if pkg changes from outside (e.g. initial load or add/remove package resets index)
  useEffect(() => {
    setLocalName(pkg.name);
    setLocalCoins(pkg.coins);
    setLocalPrice(pkg.price);
  }, [pkg.name, pkg.coins, pkg.price]);

  return (
    <TableRow className="hover:bg-neutral-50/30">
      <TableCell>
        <Input
          value={localName}
          onChange={(e) => setLocalName(e.target.value)}
          onBlur={() => onUpdate(index, 'name', localName)}
          placeholder={t('placeholderPkgName')}
          className="h-9"
        />
      </TableCell>
      <TableCell>
        <Input
          type="number"
          min="1"
          value={localCoins}
          onChange={(e) => setLocalCoins(Math.max(1, Number(e.target.value)))}
          onBlur={() => onUpdate(index, 'coins', Math.max(1, Number(localCoins)))}
          className="h-9 text-center"
        />
      </TableCell>
      <TableCell>
        <Input
          type="number"
          min="1000"
          step="1000"
          value={localPrice}
          onChange={(e) => setLocalPrice(Math.max(0, Number(e.target.value)))}
          onBlur={() => onUpdate(index, 'price', Math.max(0, Number(localPrice)))}
          className="h-9"
        />
      </TableCell>
      <TableCell>
        <div className="flex items-center gap-2">
          <Switch
            checked={pkg.isActive}
            onCheckedChange={(checked) => onUpdate(index, 'isActive', checked)}
          />
          <span className="text-xs text-neutral-500">
            {pkg.isActive ? t('statusActive') : t('statusInactive')}
          </span>
        </div>
      </TableCell>
      <TableCell className="text-right">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onRemove(index)}
          className="text-red-500 hover:text-red-700 hover:bg-red-50"
        >
          {t('btnDelete')}
        </Button>
      </TableCell>
    </TableRow>
  );
}

export function CoinConfigTab() {
  const t = useTranslations('AdminSubscriptions');
  const { data, isLoading } = useCoinConfig();
  const updateMutation = useUpdateCoinConfig();

  // State chi phí AI có kiểm soát (Controlled state)
  const [cvJdMatching, setCvJdMatching] = useState<number>(1000);
  const [mockInterview, setMockInterview] = useState<number>(2000);
  const [learningPath, setLearningPath] = useState<number>(500);
  const [unlockCv, setUnlockCv] = useState<number>(3000);
  const [postJob, setPostJob] = useState<number>(20000);
  const [extendJob, setExtendJob] = useState<number>(10000);
  const [pushTop, setPushTop] = useState<number>(5000);
  const [cvOptimize, setCvOptimize] = useState<number>(500);

  // State danh sách các gói nạp coin
  const [packages, setPackages] = useState<CoinPackageDto[]>([]);

  // Đồng bộ dữ liệu khi load xong
  useEffect(() => {
    if (data?.data) {
      const config = data.data;
      setCvJdMatching(config.featureCosts?.cvJdMatching ?? 1000);
      setMockInterview(config.featureCosts?.mockInterview ?? 2000);
      setLearningPath(config.featureCosts?.learningPath ?? 500);
      setUnlockCv(config.featureCosts?.unlockCv ?? 3000);
      setPostJob(config.featureCosts?.postJob ?? 20000);
      setExtendJob(config.featureCosts?.extendJob ?? 10000);
      setPushTop(config.featureCosts?.pushTop ?? 5000);
      setCvOptimize(config.featureCosts?.cvOptimize ?? 500);
      setPackages(config.packages || []);
    }
  }, [data]);

  // Thêm một gói nạp coin trống
  const handleAddPackage = () => {
    const newPkg: CoinPackageDto = {
      id: Math.random().toString(36).substring(2, 9), // ID tạm thời
      name: `New Top-up Package`,
      coins: 50,
      price: 99000,
      isActive: true,
    };
    setPackages(prev => [...prev, newPkg]);
  };

  // Cập nhật giá trị một gói nạp
  const handleUpdatePackageField = (index: number, field: keyof CoinPackageDto, value: any) => {
    setPackages(prev => {
      const updated = [...prev];
      updated[index] = {
        ...updated[index],
        [field]: value,
      };
      return updated;
    });
  };

  // Xóa một gói nạp
  const handleRemovePackage = (index: number) => {
    setPackages(prev => prev.filter((_, i) => i !== index));
  };

  // Xử lý submit lưu cấu hình lên API
  const handleSave = () => {
    // Validate cơ bản
    if (cvJdMatching < 0 || mockInterview < 0 || learningPath < 0 || unlockCv < 0 || postJob < 0 || extendJob < 0 || pushTop < 0 || cvOptimize < 0) {
      toast.error('AI feature cost cannot be negative');
      return;
    }

    if (packages.length === 0) {
      toast.error('At least 1 coin package must be configured');
      return;
    }

    for (const pkg of packages) {
      if (!pkg.name.trim()) {
        toast.error('Coin package name is required');
        return;
      }
      if (pkg.coins <= 0) {
        toast.error(`Package "${pkg.name}" must have more than 0 coins`);
        return;
      }
      if (pkg.price <= 0) {
        toast.error(`Package "${pkg.name}" must have a price greater than 0`);
        return;
      }
    }

    const payload: UpdateCoinConfigDto = {
      featureCosts: {
        cvJdMatching: cvJdMatching,
        mockInterview: mockInterview,
        learningPath: learningPath,
        unlockCv: unlockCv,
        postJob: postJob,
        extendJob: extendJob,
        pushTop: pushTop,
        cvOptimize: cvOptimize,
      },
      packages: packages.map(p => ({
        id: p.id,
        name: p.name,
        coins: Number(p.coins),
        price: Number(p.price),
        isActive: p.isActive,
      })),
    };

    updateMutation.mutate(payload, {
      onSuccess: (res) => {
        if (res.success) {
          toast.success(t('toastUpdateSuccess'));
        } else {
          toast.error(res.message || 'Update failed');
        }
      },
      onError: (err: any) => {
        toast.error(err.response?.data?.message || 'Error sending update request');
      },
    });
  };

  if (isLoading) {
    return <div className="p-8 text-center text-muted-foreground text-sm">{t('coinConfigLoading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        
        {/* Card 1: AI Cost Configuration */}
        <Card className="md:col-span-1 border-neutral-200/80 shadow-sm">
          <CardHeader className="border-b bg-neutral-50/50">
            <CardTitle className="text-lg font-bold text-neutral-800">{t('aiCostsTitle')}</CardTitle>
            <CardDescription>{t('aiCostsDesc')}</CardDescription>
          </CardHeader>
          <CardContent className="pt-6 space-y-4">
            
            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">{t('cvJdMatching')}</label>
              <div className="relative flex items-center">
                <Input
                  type="number"
                  min="0"
                  value={cvJdMatching}
                  onChange={(e) => setCvJdMatching(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">{t('coinLabel')}</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">{t('mockInterview')}</label>
              <div className="relative flex items-center">
                <Input
                  type="number"
                  min="0"
                  value={mockInterview}
                  onChange={(e) => setMockInterview(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">{t('coinLabel')}</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">{t('learningPath')}</label>
              <div className="relative flex items-center">
                <Input
                  type="number" min="0" value={learningPath}
                  onChange={(e) => setLearningPath(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">{t('coinLabel')}</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">{t('unlockCv')}</label>
              <div className="relative flex items-center">
                <Input
                  type="number" min="0" value={unlockCv}
                  onChange={(e) => setUnlockCv(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">{t('coinLabel')}</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">{t('postJob')}</label>
              <div className="relative flex items-center">
                <Input
                  type="number" min="0" value={postJob}
                  onChange={(e) => setPostJob(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">{t('coinLabel')}</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">{t('extendJob')}</label>
              <div className="relative flex items-center">
                <Input
                  type="number" min="0" value={extendJob}
                  onChange={(e) => setExtendJob(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">{t('coinLabel')}</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">{t('pushTop')}</label>
              <div className="relative flex items-center">
                <Input
                  type="number" min="0" value={pushTop}
                  onChange={(e) => setPushTop(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">{t('coinLabel')}</span>
              </div>
            </div>

            <div className="pt-2 text-xs text-amber-600 bg-amber-50 border border-amber-200/50 rounded-lg p-3" dangerouslySetInnerHTML={{ __html: t.raw('coinRateInfo') }} />
          </CardContent>
        </Card>

        {/* Card 2: Coin Packages Configuration */}
        <Card className="md:col-span-2 border-neutral-200/80 shadow-sm">
          <CardHeader className="border-b bg-neutral-50/50 flex flex-row items-center justify-between space-y-0">
            <div>
              <CardTitle className="text-lg font-bold text-neutral-800">{t('coinPackagesTitle')}</CardTitle>
              <CardDescription>{t('coinPackagesDesc')}</CardDescription>
            </div>
            <Button size="sm" onClick={handleAddPackage} className="bg-neutral-900 text-white hover:bg-neutral-800">
              {t('addPackageBtn')}
            </Button>
          </CardHeader>
          <CardContent className="pt-6">
            <div className="border rounded-lg overflow-hidden bg-white">
              <Table>
                <TableHeader className="bg-neutral-50">
                  <TableRow>
                    <TableHead className="w-[40%]">{t('colPkgName')}</TableHead>
                    <TableHead className="w-[15%]">{t('colCoinsAmount')}</TableHead>
                    <TableHead className="w-[20%]">{t('colPriceVnd')}</TableHead>
                    <TableHead className="w-[15%]">{t('colStatus')}</TableHead>
                    <TableHead className="w-[10%] text-right"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {packages.map((pkg, index) => (
                    <PackageRow
                      key={pkg.id}
                      pkg={pkg}
                      index={index}
                      onUpdate={handleUpdatePackageField}
                      onRemove={handleRemovePackage}
                    />
                  ))}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Action Footer */}
      <div className="flex justify-end pt-4 border-t gap-3">
        <Button
          onClick={handleSave}
          disabled={updateMutation.isPending}
          className="bg-neutral-900 text-white hover:bg-neutral-800 px-6"
        >
          {updateMutation.isPending ? t('savingChangesBtn') : t('saveAllConfigBtn')}
        </Button>
      </div>
    </div>
  );
}
