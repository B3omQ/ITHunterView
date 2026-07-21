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

export interface CreatePaymentDto {
  targetType: 'WALLET_TOPUP' | 'SUBSCRIPTION';
  targetId: string;
  paymentGateway: 'PAYOS' | 'MOMO' | 'VNPAY';
}

export interface CreatePaymentResponseDto {
  paymentId: string;
  orderCode: number;
  checkoutUrl: string;
  qrCode?: string;
}

export interface PaymentSimulationDto {
  paymentId: string;
  gatewayTransactionId: string;
  success: boolean;
}

