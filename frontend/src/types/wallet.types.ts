export interface WalletBalanceDto {
  userId: string;
  balance: number;
  activeSubscriptionName?: string | null;
  subscriptionEndDate?: string | null;
  mockInterviewLimit?: number | null;
  mockInterviewUsed?: number | null;
  cvMatchLimit?: number | null;
  cvMatchUsed?: number | null;
  learningPathLimit?: number | null;
  learningPathUsed?: number | null;
  learningPathSlotLimit?: number | null;
  learningPathSlotUsed?: number | null;
  jobSlotsLimit?: number | null;
  jobSlotsUsed?: number | null;
  unlockCvLimit?: number | null;
  unlockCvUsed?: number | null;
  jobExtendLimit?: number | null;
  jobExtendUsed?: number | null;
  pushTopLimit?: number | null;
  pushTopUsed?: number | null;
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

