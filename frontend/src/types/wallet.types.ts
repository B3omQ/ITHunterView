export type PaymentTargetType = 'SUBSCRIPTION' | 'COIN_PACKAGE';
export type PaymentGateway = 'PAYOS';
export type PaymentStatus = 'PENDING' | 'PAID' | 'CANCELLED' | 'FAILED';

export interface CreatePaymentRequest {
  targetId: string;
  targetType: PaymentTargetType;
  paymentGateway: PaymentGateway;
}

export interface CreatePaymentResponse {
  paymentId: string;
  orderCode: number;
  checkoutUrl: string;
  qrCode: string;
}

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
