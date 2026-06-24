'use client';

import { useState, useEffect } from 'react';
import { useCoinConfig, useUpdateCoinConfig } from '@/hooks/useSubscription';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { toast } from 'sonner';
import type { CoinPackageDto, UpdateCoinConfigDto } from '@/types/subscription.types';

interface PackageRowProps {
  pkg: CoinPackageDto;
  index: number;
  onUpdate: (index: number, field: keyof CoinPackageDto, value: any) => void;
  onRemove: (index: number) => void;
}

function PackageRow({ pkg, index, onUpdate, onRemove }: PackageRowProps) {
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
          placeholder="Example: Top-up 20 Coins"
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
            {pkg.isActive ? 'Active' : 'Inactive'}
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
          Delete
        </Button>
      </TableCell>
    </TableRow>
  );
}

export function CoinConfigTab() {
  const { data, isLoading } = useCoinConfig();
  const updateMutation = useUpdateCoinConfig();

  // State chi phí AI có kiểm soát (Controlled state)
  const [cvJdMatching, setCvJdMatching] = useState<number>(2);
  const [mockInterview, setMockInterview] = useState<number>(10);
  const [cvOptimize, setCvOptimize] = useState<number>(3);

  // State danh sách các gói nạp coin
  const [packages, setPackages] = useState<CoinPackageDto[]>([]);

  // Đồng bộ dữ liệu khi load xong
  useEffect(() => {
    if (data?.data) {
      const config = data.data;
      setCvJdMatching(config.featureCosts?.cvJdMatching ?? 2);
      setMockInterview(config.featureCosts?.mockInterview ?? 10);
      setCvOptimize(config.featureCosts?.cvOptimize ?? 3);
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
    const cvJdMatchingCost = cvJdMatching;
    const mockInterviewCost = mockInterview;
    const cvOptimizeCost = cvOptimize;

    // Validate cơ bản
    if (cvJdMatchingCost < 0 || mockInterviewCost < 0 || cvOptimizeCost < 0) {
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
        cvJdMatching: cvJdMatchingCost,
        mockInterview: mockInterviewCost,
        cvOptimize: cvOptimizeCost,
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
          toast.success('Coin configuration updated successfully');
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
    return <div className="p-8 text-center text-muted-foreground text-sm">Loading coin configuration...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        
        {/* Card 1: AI Cost Configuration */}
        <Card className="md:col-span-1 border-neutral-200/80 shadow-sm">
          <CardHeader className="border-b bg-neutral-50/50">
            <CardTitle className="text-lg font-bold text-neutral-800">AI Costs (Coins)</CardTitle>
            <CardDescription>Configure the number of coins consumed per AI service usage.</CardDescription>
          </CardHeader>
          <CardContent className="pt-6 space-y-4">
            
            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">CV-JD Matching</label>
              <div className="relative flex items-center">
                <Input
                  type="number"
                  min="0"
                  value={cvJdMatching}
                  onChange={(e) => setCvJdMatching(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">Coin</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">Mock Interview</label>
              <div className="relative flex items-center">
                <Input
                  type="number"
                  min="0"
                  value={mockInterview}
                  onChange={(e) => setMockInterview(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">Coin</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">CV Optimization</label>
              <div className="relative flex items-center">
                <Input
                  type="number"
                  min="0"
                  value={cvOptimize}
                  onChange={(e) => setCvOptimize(e.target.value === '' ? 0 : Number(e.target.value))}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">Coin</span>
              </div>
            </div>

            <div className="pt-2 text-xs text-amber-600 bg-amber-50 border border-amber-200/50 rounded-lg p-3">
              The default top-up rate is set at <strong>1 Coin = 2,000 VND</strong> when configuring wallet top-up packages.
            </div>
          </CardContent>
        </Card>

        {/* Card 2: Coin Packages Configuration */}
        <Card className="md:col-span-2 border-neutral-200/80 shadow-sm">
          <CardHeader className="border-b bg-neutral-50/50 flex flex-row items-center justify-between space-y-0">
            <div>
              <CardTitle className="text-lg font-bold text-neutral-800">Coin Top-up Packages</CardTitle>
              <CardDescription>Manage the list of coin top-up amounts displayed to candidates.</CardDescription>
            </div>
            <Button size="sm" onClick={handleAddPackage} className="bg-neutral-900 text-white hover:bg-neutral-800">
              Add Package
            </Button>
          </CardHeader>
          <CardContent className="pt-6">
            <div className="border rounded-lg overflow-hidden bg-white">
              <Table>
                <TableHeader className="bg-neutral-50">
                  <TableRow>
                    <TableHead className="w-[40%]">Package Name</TableHead>
                    <TableHead className="w-[15%]">Coins Amount</TableHead>
                    <TableHead className="w-[20%]">Price (VND)</TableHead>
                    <TableHead className="w-[15%]">Status</TableHead>
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
          {updateMutation.isPending ? 'Saving changes...' : 'Save All Configuration'}
        </Button>
      </div>
    </div>
  );
}
