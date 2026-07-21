export interface WalletBalanceDto {
  userId: string;
  balance: number;
}

export interface WalletTransactionDto {
  id: string;
  amount: number;
  transactionType: string;
  referenceId: string;
  description: string;
  createdAt: string;
}

export type PaymentGatewayType = 'VNPAY' | 'MOMO' | 'STRIPE' | 'PAYPAL' | 'BANK_TRANSFER';

export interface CreatePaymentDto {
  paymentGateway: PaymentGatewayType;
  targetType: 'WALLET_TOPUP' | 'SUBSCRIPTION';
  targetId: string;
}

export interface PaymentDto {
  id: string;
  userId: string;
  amount: number;
  currency: string;
  creditsGranted?: number | null;
  paymentGateway: string;
  gatewayTransactionId?: string | null;
  targetType: string;
  targetId?: string | null;
  status: 'PENDING' | 'SUCCESS' | 'FAILED';
  createdAt: string;
  updatedAt: string;
}

export interface PaymentSimulationDto {
  paymentId: string;
  success: boolean;
  gatewayTransactionId: string;
}
