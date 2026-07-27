'use client';

import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { useAdminPayments } from '@/hooks/useAdminPayments';
import { useSignalR } from '@/hooks/useSignalR';
import type { PaymentDto } from '@/types/wallet.types';

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

export default function FinancePage() {
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useAdminPayments({ page, pageSize });
  const result = data?.data;
  const queryClient = useQueryClient();
  const connection = useSignalR('/hubs/notification');

  useEffect(() => {
    if (connection) {
      connection.on('ReceiveNewPayment', (payment: PaymentDto) => {
        // Có thể update trực tiếp vào cache để nhanh hơn, 
        // hoặc đơn giản là invalidate để gọi lại API
        queryClient.invalidateQueries({ queryKey: ['admin-payments'] });
      });
    }

    return () => {
      if (connection) {
        connection.off('ReceiveNewPayment');
      }
    };
  }, [connection, queryClient]);

  return (
    <div className="p-6">
      <Card>
        <CardHeader>
          <CardTitle>Quản lý tài chính & Giao dịch</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tên người dùng</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Số tiền</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead>Thời gian giao dịch</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center">Đang tải...</TableCell>
                  </TableRow>
                ) : !result?.items?.length ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-center">Chưa có giao dịch nào.</TableCell>
                  </TableRow>
                ) : (
                  result.items.map((payment) => (
                    <TableRow key={payment.id}>
                      <TableCell className="font-medium">
                        {payment.userName || 'Unknown'}
                      </TableCell>
                      <TableCell>{payment.userEmail || 'N/A'}</TableCell>
                      <TableCell className="text-green-600 font-semibold">
                        +{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(payment.amount)}
                      </TableCell>
                      <TableCell>
                        <Badge 
                          variant={payment.status === 'SUCCESS' ? 'default' : payment.status === 'FAILED' ? 'destructive' : 'secondary'}
                        >
                          {payment.status}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {format(new Date(payment.createdAt), 'dd/MM/yyyy HH:mm:ss')}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
          
          {result && result.total > pageSize && (
             <div className="flex justify-end gap-2 mt-4">
                <Button 
                  variant="outline" 
                  disabled={page === 1}
                  onClick={() => setPage(page - 1)}
                >
                  Trang trước
                </Button>
                <Button 
                  variant="outline"
                  disabled={page * pageSize >= result.total}
                  onClick={() => setPage(page + 1)}
                >
                  Trang sau
                </Button>
             </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
