'use client';

import { useState, useEffect, useRef } from 'react';
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
          placeholder="Ví dụ: Gói nạp 20 Coin"
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
            {pkg.isActive ? 'Bật' : 'Tắt'}
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
          Xóa
        </Button>
      </TableCell>
    </TableRow>
  );
}

export function CoinConfigTab() {
  const { data, isLoading } = useCoinConfig();
  const updateMutation = useUpdateCoinConfig();

  // Refs cho form chi phí AI nhằm tránh re-render khi gõ phím
  const cvJdMatchingRef = useRef<HTMLInputElement>(null);
  const mockInterviewRef = useRef<HTMLInputElement>(null);
  const cvOptimizeRef = useRef<HTMLInputElement>(null);

  // State danh sách các gói nạp coin
  const [packages, setPackages] = useState<CoinPackageDto[]>([]);

  // Đồng bộ dữ liệu khi load xong
  useEffect(() => {
    if (data?.data) {
      const config = data.data;
      if (cvJdMatchingRef.current) {
        cvJdMatchingRef.current.value = String(config.featureCosts?.cvJdMatching ?? 2);
      }
      if (mockInterviewRef.current) {
        mockInterviewRef.current.value = String(config.featureCosts?.mockInterview ?? 10);
      }
      if (cvOptimizeRef.current) {
        cvOptimizeRef.current.value = String(config.featureCosts?.cvOptimize ?? 3);
      }
      setPackages(config.packages || []);
    }
  }, [data]);

  // Thêm một gói nạp coin trống
  const handleAddPackage = () => {
    const newPkg: CoinPackageDto = {
      id: Math.random().toString(36).substring(2, 9), // ID tạm thời
      name: `Gói nạp mới`,
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
    const cvJdMatchingCost = Number(cvJdMatchingRef.current?.value ?? 0);
    const mockInterviewCost = Number(mockInterviewRef.current?.value ?? 0);
    const cvOptimizeCost = Number(cvOptimizeRef.current?.value ?? 0);

    // Validate cơ bản
    if (cvJdMatchingCost < 0 || mockInterviewCost < 0 || cvOptimizeCost < 0) {
      toast.error('Chi phí tính năng AI không được nhỏ hơn 0');
      return;
    }

    if (packages.length === 0) {
      toast.error('Phải cấu hình tối thiểu 1 gói nạp Coin');
      return;
    }

    for (const pkg of packages) {
      if (!pkg.name.trim()) {
        toast.error('Tên gói nạp Coin không được để trống');
        return;
      }
      if (pkg.coins <= 0) {
        toast.error(`Gói "${pkg.name}" phải có số Coin lớn hơn 0`);
        return;
      }
      if (pkg.price <= 0) {
        toast.error(`Gói "${pkg.name}" phải có giá tiền lớn hơn 0`);
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
          toast.success('Cập nhật cấu hình Coin thành công');
        } else {
          toast.error(res.message || 'Cập nhật thất bại');
        }
      },
      onError: (err: any) => {
        toast.error(err.response?.data?.message || 'Lỗi khi gửi yêu cầu cập nhật');
      },
    });
  };

  if (isLoading) {
    return <div className="p-8 text-center text-muted-foreground text-sm">Đang tải cấu hình Coin...</div>;
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        
        {/* Card 1: AI Cost Configuration */}
        <Card className="md:col-span-1 border-neutral-200/80 shadow-sm">
          <CardHeader className="border-b bg-neutral-50/50">
            <CardTitle className="text-lg font-bold text-neutral-800">Chi phí AI (Coin)</CardTitle>
            <CardDescription>Cấu hình số coin tiêu tốn cho mỗi lần sử dụng dịch vụ AI.</CardDescription>
          </CardHeader>
          <CardContent className="pt-6 space-y-4">
            
            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">So khớp CV - JD</label>
              <div className="relative flex items-center">
                <Input
                  type="number"
                  min="0"
                  ref={cvJdMatchingRef}
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
                  ref={mockInterviewRef}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">Coin</span>
              </div>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-semibold text-neutral-700">Tối ưu hóa CV</label>
              <div className="relative flex items-center">
                <Input
                  type="number"
                  min="0"
                  ref={cvOptimizeRef}
                  className="pr-12"
                />
                <span className="absolute right-3 text-xs font-semibold text-neutral-400">Coin</span>
              </div>
            </div>

            <div className="pt-2 text-xs text-amber-600 bg-amber-50 border border-amber-200/50 rounded-lg p-3">
              Tỷ giá nạp mặc định được quy ước là <strong>1 Coin = 2,000 VND</strong> khi thiết lập các gói ví nạp.
            </div>
          </CardContent>
        </Card>

        {/* Card 2: Coin Packages Configuration */}
        <Card className="md:col-span-2 border-neutral-200/80 shadow-sm">
          <CardHeader className="border-b bg-neutral-50/50 flex flex-row items-center justify-between space-y-0">
            <div>
              <CardTitle className="text-lg font-bold text-neutral-800">Các gói nạp Coin</CardTitle>
              <CardDescription>Quản lý danh sách các mệnh giá nạp coin hiển thị cho ứng viên.</CardDescription>
            </div>
            <Button size="sm" onClick={handleAddPackage} className="bg-neutral-900 text-white hover:bg-neutral-800">
              Thêm gói nạp
            </Button>
          </CardHeader>
          <CardContent className="pt-6">
            <div className="border rounded-lg overflow-hidden bg-white">
              <Table>
                <TableHeader className="bg-neutral-50">
                  <TableRow>
                    <TableHead className="w-[40%]">Tên gói nạp</TableHead>
                    <TableHead className="w-[15%]">Số Coin</TableHead>
                    <TableHead className="w-[20%]">Mệnh giá (VND)</TableHead>
                    <TableHead className="w-[15%]">Trạng thái</TableHead>
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
          {updateMutation.isPending ? 'Đang lưu thay đổi...' : 'Lưu tất cả cấu hình'}
        </Button>
      </div>
    </div>
  );
}
