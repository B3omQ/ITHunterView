'use client';

import { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { toast } from 'sonner';
import api from '@/services/api-client';
import { useQueryClient } from '@tanstack/react-query';
import { Zap, Coins, Key } from 'lucide-react';

export function CheatMenu() {
  const [open, setOpen] = useState(false);
  const [amount, setAmount] = useState('10000');
  const [subId, setSubId] = useState('1'); 
  const [isLoading, setIsLoading] = useState(false);
  const queryClient = useQueryClient();

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl + Shift + F9 to open cheat menu
      if (e.ctrlKey && e.shiftKey && e.key === 'F9') {
        e.preventDefault();
        setOpen(true);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const handleAddCoin = async () => {
    try {
      setIsLoading(true);
      await api.post(`/api/v1/wallet/cheat/add-coin?amount=${amount}`);
      toast.success(`Đã buff thành công ${amount} Coins!`);
      // Cập nhật lại số dư ví trên UI
      queryClient.invalidateQueries({ queryKey: ['walletBalance'] });
      queryClient.invalidateQueries({ queryKey: ['walletTransactions'] });
    } catch (error: any) {
      toast.error('Hack coin thất bại: ' + (error.response?.data?.message || error.message));
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubscribe = async () => {
    try {
      setIsLoading(true);
      await api.post(`/api/v1/wallet/cheat/subscribe?subscriptionId=${subId}`);
      toast.success(`Đã hack thành công gói Subscription ID = ${subId}!`);
    } catch (error: any) {
      toast.error('Hack gói thất bại: ' + (error.response?.data?.message || error.message));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent className="sm:max-w-md border-red-500/50 shadow-[0_0_15px_rgba(239,68,68,0.2)]">
        <DialogHeader>
          <DialogTitle className="text-red-500 flex items-center gap-2">
            <Zap className="h-5 w-5 fill-current" />
            Bảng Điều Khiển Tà Đạo (Cheat)
          </DialogTitle>
          <DialogDescription>
            Bí kíp test nhanh. Nhấn <kbd className="px-1.5 py-0.5 bg-muted rounded border text-xs font-mono font-semibold">Ctrl + Shift + F9</kbd> để bật/tắt. 
            Để gỡ bỏ tính năng này, hãy xóa &lt;CheatMenu /&gt; trong AppShell.tsx.
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-6 py-4">
          {/* Hack Coin */}
          <div className="space-y-3 p-4 bg-amber-500/10 border border-amber-500/20 rounded-xl relative overflow-hidden">
            <div className="absolute top-0 right-0 p-4 opacity-10">
              <Coins className="w-16 h-16" />
            </div>
            <h4 className="font-semibold text-amber-600 dark:text-amber-500 flex items-center gap-2 relative z-10">
              <Coins className="h-4 w-4" /> Bơm Tiền Test
            </h4>
            <div className="flex gap-2 relative z-10">
              <Input 
                type="number" 
                value={amount} 
                onChange={(e) => setAmount(e.target.value)} 
                placeholder="Số Coin"
                className="flex-1 bg-background/50 backdrop-blur-sm focus-visible:ring-amber-500"
              />
              <Button onClick={handleAddCoin} disabled={isLoading} variant="default" className="bg-amber-500 hover:bg-amber-600 text-white border-none shadow-md shadow-amber-500/20">
                Bơm Ngay!
              </Button>
            </div>
          </div>

          {/* Hack Sub */}
          <div className="space-y-3 p-4 bg-indigo-500/10 border border-indigo-500/20 rounded-xl relative overflow-hidden">
            <div className="absolute top-0 right-0 p-4 opacity-10">
              <Key className="w-16 h-16" />
            </div>
            <h4 className="font-semibold text-indigo-600 dark:text-indigo-400 flex items-center gap-2 relative z-10">
              <Key className="h-4 w-4" /> Kích Hoạt Gói
            </h4>
            <div className="flex gap-2 relative z-10">
              <Input 
                type="number" 
                value={subId} 
                onChange={(e) => setSubId(e.target.value)} 
                placeholder="ID Gói (VD: 1, 2)"
                className="flex-1 bg-background/50 backdrop-blur-sm focus-visible:ring-indigo-500"
              />
              <Button onClick={handleSubscribe} disabled={isLoading} variant="default" className="bg-indigo-500 hover:bg-indigo-600 text-white border-none shadow-md shadow-indigo-500/20">
                Mở Khóa!
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
