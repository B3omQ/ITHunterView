'use client';

import { useState } from 'react';
import { useSubscriptions, useCreateSubscription, useUpdateSubscription, useDuplicateSubscription, useUpdateSubscriptionStatus } from '@/hooks/useSubscription';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { toast } from 'sonner';
import { SubscriptionForm } from './components/SubscriptionForm';
import { CoinConfigTab } from './components/CoinConfigTab';
import type { SubscriptionDto, SubscriptionStatus } from '@/types/subscription.types';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';

export default function SubscriptionsAdminPage() {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [roleFilter, setRoleFilter] = useState<string>('ALL');
  const [statusFilter, setStatusFilter] = useState<string>('ALL');
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [editingSub, setEditingSub] = useState<SubscriptionDto | null>(null);

  const { data, isLoading, isFetching } = useSubscriptions({
    page,
    pageSize,
    role: roleFilter === 'ALL' ? undefined : roleFilter,
    status: statusFilter === 'ALL' ? undefined : (statusFilter as SubscriptionStatus),
  });

  const createMutation = useCreateSubscription();
  const updateMutation = useUpdateSubscription();
  const duplicateMutation = useDuplicateSubscription();
  const updateStatusMutation = useUpdateSubscriptionStatus();

  const handleCreateOrUpdate = async (formData: any) => {
    if (editingSub) {
      updateMutation.mutate(
        { id: editingSub.id, data: formData },
        {
          onSuccess: (res) => {
            if (res.success) {
              toast.success('Cập nhật gói thành công');
              setIsDialogOpen(false);
              setEditingSub(null);
            } else {
              toast.error(res.message || 'Cập nhật thất bại');
            }
          },
          onError: (err: any) => {
            toast.error(err.response?.data?.message || 'Có lỗi xảy ra');
          },
        }
      );
    } else {
      createMutation.mutate(formData, {
        onSuccess: (res) => {
          if (res.success) {
            toast.success('Tạo gói thành công ở trạng thái INACTIVE');
            setIsDialogOpen(false);
          } else {
            toast.error(res.message || 'Tạo thất bại');
          }
        },
        onError: (err: any) => {
          toast.error(err.response?.data?.message || 'Có lỗi xảy ra');
        },
      });
    }
  };

  const handleDuplicate = (id: number) => {
    duplicateMutation.mutate(id, {
      onSuccess: (res) => {
        if (res.success) {
          toast.success('Nhân bản gói thành công (Bản sao mặc định ở trạng thái INACTIVE)');
        } else {
          toast.error(res.message || 'Nhân bản thất bại');
        }
      },
    });
  };

  const handleToggleStatus = (id: number, currentStatus: SubscriptionStatus) => {
    const nextStatus: SubscriptionStatus = currentStatus === 'ACTIVE' ? 'INACTIVE' : 'ACTIVE';
    updateStatusMutation.mutate(
      { id, status: nextStatus },
      {
        onSuccess: (res) => {
          if (res.success) {
            toast.success(`Đã đổi trạng thái gói sang ${nextStatus}`);
          } else {
            toast.error(res.message || 'Thay đổi trạng thái thất bại');
          }
        },
      }
    );
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Cấu hình Subscription &amp; Coin</h1>
          <p className="text-sm text-muted-foreground">
            Quản lý gói cước subscription, hạn mức AI và cấu hình ví coin cho Ứng viên.
          </p>
        </div>
      </div>

      <Tabs defaultValue="subscriptions" className="space-y-6">
        <TabsList className="border-b rounded-none w-full justify-start bg-transparent h-auto p-0 gap-6">
          <TabsTrigger
            value="subscriptions"
            className="rounded-none border-b-2 border-transparent data-[active]:border-neutral-950 data-[active]:bg-transparent px-4 py-2 font-semibold text-sm transition-all"
          >
            Gói Subscription
          </TabsTrigger>
          <TabsTrigger
            value="coin-config"
            className="rounded-none border-b-2 border-transparent data-[active]:border-neutral-950 data-[active]:bg-transparent px-4 py-2 font-semibold text-sm transition-all"
          >
            Cấu hình Coin
          </TabsTrigger>
        </TabsList>

        <TabsContent value="subscriptions" className="space-y-6">
          <div className="flex justify-between items-center">
            <div>
              <h2 className="text-lg font-bold text-neutral-800">Danh sách gói dịch vụ</h2>
              <p className="text-xs text-muted-foreground">Cấu hình hạn mức tính năng AI cho từng đối tượng sử dụng.</p>
            </div>

            <Dialog open={isDialogOpen} onOpenChange={(open) => {
              setIsDialogOpen(open);
              if (!open) setEditingSub(null);
            }}>
              <DialogTrigger render={<Button onClick={() => setEditingSub(null)}>Thêm gói mới</Button>} />
              <DialogContent className="max-w-lg overflow-y-auto max-h-[90vh]">
                <DialogHeader>
                  <DialogTitle>{editingSub ? 'Chỉnh sửa gói dịch vụ' : 'Tạo gói dịch vụ mới'}</DialogTitle>
                </DialogHeader>
                <SubscriptionForm
                  initialData={editingSub}
                  onSubmit={handleCreateOrUpdate}
                  isLoading={createMutation.isPending || updateMutation.isPending}
                />
              </DialogContent>
            </Dialog>
          </div>

          {/* Filters */}
          <div className="flex gap-4 items-center bg-neutral-50 p-4 rounded-xl border">
            <div className="flex flex-col space-y-1">
              <span className="text-xs font-semibold text-neutral-500">Đối tượng (Role)</span>
              <Select value={roleFilter} onValueChange={(val) => setRoleFilter(val ?? 'ALL')}>
                <SelectTrigger className="w-[180px] bg-white">
                  <SelectValue placeholder="Chọn vai trò" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">Tất cả</SelectItem>
                  <SelectItem value="CANDIDATE">Ứng viên</SelectItem>
                  <SelectItem value="RECRUITER">Nhà tuyển dụng</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col space-y-1">
              <span className="text-xs font-semibold text-neutral-500">Trạng thái</span>
              <Select value={statusFilter} onValueChange={(val) => setStatusFilter(val ?? 'ALL')}>
                <SelectTrigger className="w-[180px] bg-white">
                  <SelectValue placeholder="Chọn trạng thái" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">Tất cả</SelectItem>
                  <SelectItem value="ACTIVE">Hoạt động</SelectItem>
                  <SelectItem value="INACTIVE">Vô hiệu hóa</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Data Table */}
          <div className="border rounded-xl bg-white shadow-sm overflow-hidden relative">
            {isLoading ? (
              <div className="p-8 text-center text-muted-foreground text-sm">Đang tải danh sách...</div>
            ) : !data?.data?.items?.length ? (
              <div className="p-8 text-center text-muted-foreground text-sm">Không tìm thấy gói dịch vụ nào.</div>
            ) : (
              <div className={isFetching ? "opacity-60 pointer-events-none transition-opacity duration-200" : "transition-opacity duration-200"}>
                <Table>
                  <TableHeader className="bg-neutral-50">
                    <TableRow>
                      <TableHead>Tên gói</TableHead>
                      <TableHead>Đối tượng</TableHead>
                      <TableHead>Giá bán</TableHead>
                      <TableHead>Thời hạn</TableHead>
                      <TableHead>Trạng thái</TableHead>
                      <TableHead>Giao dịch</TableHead>
                      <TableHead className="text-right">Hành động</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.data.items.map((sub) => (
                      <TableRow key={sub.id} className="hover:bg-neutral-50/50">
                        <TableCell className="font-semibold text-neutral-900">{sub.name}</TableCell>
                        <TableCell>
                          <Badge variant={sub.featuresConfig?.role === 'CANDIDATE' ? 'default' : 'secondary'}>
                            {sub.featuresConfig?.role === 'CANDIDATE' ? 'Candidate' : 'Recruiter'}
                          </Badge>
                        </TableCell>
                        <TableCell className="font-medium">{formatPrice(sub.price)}</TableCell>
                        <TableCell>{sub.durationDays} ngày</TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Switch
                              checked={sub.status === 'ACTIVE'}
                              onCheckedChange={() => handleToggleStatus(sub.id, sub.status)}
                            />
                            <span className="text-xs text-neutral-500">
                              {sub.status === 'ACTIVE' ? 'Active' : 'Inactive'}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant={sub.isUsed ? 'outline' : 'secondary'} className={sub.isUsed ? 'border-amber-200 text-amber-700 bg-amber-50' : 'bg-neutral-100 text-neutral-600'}>
                            {sub.isUsed ? 'Đã có gd' : 'Chưa bán'}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right space-x-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => {
                              setEditingSub(sub);
                              setIsDialogOpen(true);
                            }}
                          >
                            Sửa
                          </Button>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleDuplicate(sub.id)}
                          >
                            Nhân bản
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </div>

          {/* Pagination */}
          {data?.data && data.data.totalItems > pageSize && (
            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page === 1}
                onClick={() => setPage((p) => p - 1)}
              >
                Trang trước
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={page * pageSize >= data.data.totalItems}
                onClick={() => setPage((p) => p + 1)}
              >
                Trang sau
              </Button>
            </div>
          )}
        </TabsContent>

        <TabsContent value="coin-config">
          <CoinConfigTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
