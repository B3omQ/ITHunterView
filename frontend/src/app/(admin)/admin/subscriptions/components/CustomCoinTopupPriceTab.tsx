'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { useAdminCustomCoinTopupPrice, useUpdateAdminCustomCoinTopupPrice } from '@/hooks/useWallet';
import { toast } from 'sonner';

export function CustomCoinTopupPriceTab() {
  const { data, isLoading } = useAdminCustomCoinTopupPrice();
  const updateMutation = useUpdateAdminCustomCoinTopupPrice();
  const [editedPricePerCoinVnd, setEditedPricePerCoinVnd] = useState<string | null>(null);
  const pricePerCoinVnd = editedPricePerCoinVnd ?? String(data?.data?.pricePerCoinVnd ?? '');

  const parsedPrice = Number(pricePerCoinVnd);
  const isValidPrice = Number.isInteger(parsedPrice) && parsedPrice > 0;

  const handleSave = () => {
    if (!isValidPrice) {
      toast.error('Enter a positive whole-number price in VND.');
      return;
    }

    updateMutation.mutate(
      { pricePerCoinVnd: parsedPrice },
      {
        onSuccess: (res) => {
          if (res.success) {
            toast.success('Custom Coin price updated successfully');
          } else {
            toast.error(res.message || 'Unable to update custom Coin price');
          }
        },
        onError: (error) => {
          toast.error(error.message || 'Unable to update custom Coin price');
        },
      }
    );
  };

  return (
    <Card className="max-w-xl">
      <CardHeader>
        <CardTitle>Custom Coin Top-up Price</CardTitle>
        <CardDescription>
          This is the price Candidate pays for one individually purchased Coin. It does not affect Coin packages or feature costs.
        </CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <p className="text-sm text-muted-foreground">Loading custom Coin price...</p>
        ) : (
          <label className="block space-y-2 text-sm font-medium">
            Price per Coin (VND)
            <Input
              type="number"
              min="1"
              step="1"
              inputMode="numeric"
              value={pricePerCoinVnd}
              onChange={(event) => setEditedPricePerCoinVnd(event.target.value)}
            />
          </label>
        )}
      </CardContent>
      <CardFooter>
        <Button onClick={handleSave} disabled={isLoading || !isValidPrice || updateMutation.isPending}>
          {updateMutation.isPending ? 'Saving...' : 'Save custom Coin price'}
        </Button>
      </CardFooter>
    </Card>
  );
}
