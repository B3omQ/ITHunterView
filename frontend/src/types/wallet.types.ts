export interface WalletBalanceDto {
  userId: string;
  balance: number;
}

export interface WalletTransactionDto {
  id: string;
  amount: number;
  transactionType: string;
  referenceId: string | null;
  description: string;
  createdAt: string;
}

export interface PaymentDto {
  id: string;
  userId: string;
  orderCode: number | null;
  amount: number;
  currency: string;
  creditsGranted: number | null;
  paymentGateway: string;
  gatewayTransactionId: string;
  targetType: string;
  targetId: string | null;
  subscriptionName: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
}
