export type PaymentStatus = 'Paid' | 'Partial' | 'Unpaid' | 'Overdue';
export type PaymentMethod = 'Cash' | 'VodafoneCash' | 'InstaPay' | 'BankTransfer' | 'POS';
export type TransactionStatus = 'Completed' | 'Reversed' | 'Pending';

export interface StudentCharge {
  id: string;                         // UUID v4
  titleAr?: string;
  studentId: string;                  // FK to students.id
  workspaceId?: string;               // Multi-tenant isolation
  month?: string;                     // E.g. "2026-10"
  description?: string;               // E.g. "مصروفات شهر أكتوبر 2026"
  amount: number;                     // E.g. 450.00
  allocatedAmount: number;            // Total paid towards this charge
  dueAt?: string;
  dueAtUtc?: string;                  // ISO 8601
  status?: 'Open' | 'Paid' | 'PartiallyPaid';
  createdAtUtc?: string;
}

export interface PaymentAllocation {
  id: string;                         // UUID v4
  paymentId: string;                  // FK to student_payments.id
  chargeId: string;                   // FK to student_charges.id
  amount: number;                     // Amount applied to this charge
  isReversed: boolean;                // Marked true on reversal
  allocatedAtUtc?: string;
}

export interface StudentBillingSummary {
  studentId: string;                  // UUID v4
  studentName: string;                // Arabic display name
  studentCode: string;                // E.g. "ST-1042" (wrapped in .ltr-num)
  gradeName: string;                  // E.g. "الثالث الثانوي"
  groupName: string;                  // E.g. "فيزياء 3 ث - أ"
  requiredFee: number;                // Aggregate monthly charge
  paidAmount: number;                 // Total collected
  remainingFee: number;               // requiredFee - paidAmount
  paymentStatus: PaymentStatus;
  lastPaymentDate?: string;
  openCharges?: StudentCharge[];
}

export interface FinancialTransaction {
  id: string;                         // E.g. "REC-2026-0104"
  receiptNumber?: string;
  type?: 'Payment' | 'Charge' | 'Reversal';
  workspaceId?: string;
  studentId: string;
  studentName: string;
  studentCode: string;
  amount: number;                     // E.g. 450.00
  paymentMethod: PaymentMethod;
  referenceNumber?: string;
  transactionReference?: string;      // E.g. Vodafone Cash ref "VF-8912"
  createdAt?: string;
  recordedAtUtc?: string;
  recordedByUserId?: string;
  recordedByUserName?: string;
  status: TransactionStatus;          // 'Completed' | 'Reversed'
  reversalReason?: string;            // Min 5 chars if status === 'Reversed'
  reversedAtUtc?: string;
  reversedByUserName?: string;
  allocations?: PaymentAllocation[];
  notes?: string;
}

export interface FinancialKpis {
  totalRequiredEgp: number;
  totalCollectedEgp: number;
  totalRemainingEgp: number;
  totalStudentsCount: number;
  paidStudentsCount: number;
  partialStudentsCount: number;
  unpaidStudentsCount: number;
  collectionRatePercent?: number;
}
