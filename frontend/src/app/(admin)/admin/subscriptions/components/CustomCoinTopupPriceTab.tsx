'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { useAdminCustomCoinTopupPrice, useUpdateAdminCustomCoinTopupPrice } from '@/hooks/useWallet';
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';

export function CustomCoinTopupPriceTab() {
  const t = useTranslations('AdminSubscriptions');
  const { data, isLoading } = useAdminCustomCoinTopupPrice();
  const updateMutation = useUpdateAdminCustomCoinTopupPrice();
  const [editedPricePerCoinVnd, setEditedPricePerCoinVnd] = useState<string | null>(null);
  const pricePerCoinVnd = editedPricePerCoinVnd ?? String(data?.data?.pricePerCoinVnd ?? '');

  const parsedPrice = Number(pricePerCoinVnd);
  const isValidPrice = Number.isInteger(parsedPrice) && parsedPrice > 0;

  const handleSave = () => {
    if (!isValidPrice) {
      toast.error(t('customCoinToastValid'));
      return;
    }

    updateMutation.mutate(
      { pricePerCoinVnd: parsedPrice },
      {
        onSuccess: (res) => {
          if (res.success) {
            toast.success(t('customCoinToastSuccess'));
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
        <CardTitle>{t('customCoinTitle')}</CardTitle>
        <CardDescription>
          {t('customCoinDesc')}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <p className="text-sm text-muted-foreground">{t('customCoinLoading')}</p>
        ) : (
          <label className="block space-y-2 text-sm font-medium">
            {t('customCoinLabel')}
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
          {updateMutation.isPending ? t('customCoinSavingBtn') : t('customCoinSaveBtn')}
        </Button>
      </CardFooter>
    </Card>
  );
}
