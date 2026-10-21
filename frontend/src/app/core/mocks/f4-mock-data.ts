import {
  StudentBillingSummary,
  FinancialTransaction,
  FinancialKpis,
  StudentCharge
} from '../models/finance.models';
import {
  WhatsAppTemplate,
  WhatsAppMessageLog,
  GuardianConsentRecord
} from '../models/whatsapp.models';
import {
  QuestionItem,
  ExamItem,
  HomeworkItem,
  LibraryItem
} from '../models/lms.models';
import {
  WorkspaceStaffMember,
  WorkspaceSettings,
  ReadOnlySubscriptionView
} from '../models/team-settings.models';

export const MOCK_STUDENT_CHARGES: StudentCharge[] = [
  {
    id: 'ch-101',
    studentId: 'st-1042',
    workspaceId: 'ws-main',
    month: '2026-10',
    description: 'مصروفات شهر أكتوبر 2026 - فيزياء 3 ث',
    amount: 450.0,
    allocatedAmount: 450.0,
    dueAtUtc: '2026-10-01T00:00:00Z',
    status: 'Paid',
    createdAtUtc: '2026-10-01T08:00:00Z'
  },
  {
    id: 'ch-102',
    studentId: 'st-1043',
    workspaceId: 'ws-main',
    month: '2026-10',
    description: 'مصروفات شهر أكتوبر 2026 - فيزياء 3 ث',
    amount: 450.0,
    allocatedAmount: 200.0,
    dueAtUtc: '2026-10-01T00:00:00Z',
    status: 'PartiallyPaid',
    createdAtUtc: '2026-10-01T08:00:00Z'
  },
  {
    id: 'ch-103',
    studentId: 'st-1044',
    workspaceId: 'ws-main',
    month: '2026-10',
    description: 'مصروفات شهر أكتوبر 2026 - كيمياء 2 ث',
    amount: 450.0,
    allocatedAmount: 0,
    dueAtUtc: '2026-10-01T00:00:00Z',
    status: 'Open',
    createdAtUtc: '2026-10-01T08:00:00Z'
  }
];

export const MOCK_BILLING_ROSTER: StudentBillingSummary[] = [
  {
    studentId: 'st-1042',
    studentName: 'أحمد محمد علي',
    studentCode: 'ST-1042',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - أ',
    requiredFee: 450.0,
    paidAmount: 450.0,
    remainingFee: 0.0,
    paymentStatus: 'Paid',
    lastPaymentDate: '2026-10-15T14:30:00Z'
  },
  {
    studentId: 'st-1043',
    studentName: 'سارة محمود خليل',
    studentCode: 'ST-1043',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - أ',
    requiredFee: 450.0,
    paidAmount: 200.0,
    remainingFee: 250.0,
    paymentStatus: 'Partial',
    lastPaymentDate: '2026-10-10T12:00:00Z'
  },
  {
    studentId: 'st-1044',
    studentName: 'يوسف أحمد منصور',
    studentCode: 'ST-1044',
    gradeName: 'الثاني الثانوي',
    groupName: 'كيمياء 2 ث - ب',
    requiredFee: 450.0,
    paidAmount: 0.0,
    remainingFee: 450.0,
    paymentStatus: 'Unpaid'
  },
  {
    studentId: 'st-1045',
    studentName: 'مريم حسن عثمان',
    studentCode: 'ST-1045',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - ب',
    requiredFee: 450.0,
    paidAmount: 450.0,
    remainingFee: 0.0,
    paymentStatus: 'Paid',
    lastPaymentDate: '2026-10-14T09:15:00Z'
  },
  {
    studentId: 'st-1046',
    studentName: 'عمر خالد إبراهيم',
    studentCode: 'ST-1046',
    gradeName: 'الأول الثانوي',
    groupName: 'أحياء 1 ث - أ',
    requiredFee: 400.0,
    paidAmount: 0.0,
    remainingFee: 400.0,
    paymentStatus: 'Unpaid'
  },
  {
    studentId: 'st-1047',
    studentName: 'نور الدين مصطفى',
    studentCode: 'ST-1047',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - أ',
    requiredFee: 450.0,
    paidAmount: 450.0,
    remainingFee: 0.0,
    paymentStatus: 'Paid',
    lastPaymentDate: '2026-10-08T16:20:00Z'
  },
  {
    studentId: 'st-1048',
    studentName: 'فاطمة طارق الزيات',
    studentCode: 'ST-1048',
    gradeName: 'الثاني الثانوي',
    groupName: 'كيمياء 2 ث - أ',
    requiredFee: 450.0,
    paidAmount: 150.0,
    remainingFee: 300.0,
    paymentStatus: 'Partial',
    lastPaymentDate: '2026-10-12T11:45:00Z'
  },
  {
    studentId: 'st-1049',
    studentName: 'عبد الرحمن ياسر النجار',
    studentCode: 'ST-1049',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - ب',
    requiredFee: 450.0,
    paidAmount: 0.0,
    remainingFee: 450.0,
    paymentStatus: 'Unpaid'
  },
  {
    studentId: 'st-1050',
    studentName: 'شهد وائل عبد العزيز',
    studentCode: 'ST-1050',
    gradeName: 'الأول الثانوي',
    groupName: 'أحياء 1 ث - أ',
    requiredFee: 400.0,
    paidAmount: 400.0,
    remainingFee: 0.0,
    paymentStatus: 'Paid',
    lastPaymentDate: '2026-10-05T10:00:00Z'
  },
  {
    studentId: 'st-1051',
    studentName: 'كريم سامح الشناوي',
    studentCode: 'ST-1051',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - أ',
    requiredFee: 450.0,
    paidAmount: 450.0,
    remainingFee: 0.0,
    paymentStatus: 'Paid',
    lastPaymentDate: '2026-10-02T13:30:00Z'
  },
  {
    studentId: 'st-1052',
    studentName: 'هدى عصام الدين',
    studentCode: 'ST-1052',
    gradeName: 'الثاني الثانوي',
    groupName: 'كيمياء 2 ث - ب',
    requiredFee: 450.0,
    paidAmount: 250.0,
    remainingFee: 200.0,
    paymentStatus: 'Partial',
    lastPaymentDate: '2026-10-11T15:10:00Z'
  },
  {
    studentId: 'st-1053',
    studentName: 'حمزة أشرف رضوان',
    studentCode: 'ST-1053',
    gradeName: 'الأول الثانوي',
    groupName: 'أحياء 1 ث - ب',
    requiredFee: 400.0,
    paidAmount: 0.0,
    remainingFee: 400.0,
    paymentStatus: 'Unpaid'
  },
  {
    studentId: 'st-1054',
    studentName: 'رنا إيهاب القاضي',
    studentCode: 'ST-1054',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - ب',
    requiredFee: 450.0,
    paidAmount: 450.0,
    remainingFee: 0.0,
    paymentStatus: 'Paid',
    lastPaymentDate: '2026-10-06T17:00:00Z'
  },
  {
    studentId: 'st-1055',
    studentName: 'زياد وليد درويش',
    studentCode: 'ST-1055',
    gradeName: 'الثاني الثانوي',
    groupName: 'كيمياء 2 ث - أ',
    requiredFee: 450.0,
    paidAmount: 0.0,
    remainingFee: 450.0,
    paymentStatus: 'Unpaid'
  },
  {
    studentId: 'st-1056',
    studentName: 'ملك هاني فهمي',
    studentCode: 'ST-1056',
    gradeName: 'الثالث الثانوي',
    groupName: 'فيزياء 3 ث - أ',
    requiredFee: 450.0,
    paidAmount: 450.0,
    remainingFee: 0.0,
    paymentStatus: 'Paid',
    lastPaymentDate: '2026-10-09T14:00:00Z'
  }
];

export const MOCK_FINANCIAL_KPIS: FinancialKpis = {
  totalRequiredEgp: 112500.0,
  totalCollectedEgp: 84250.0,
  totalRemainingEgp: 28250.0,
  totalStudentsCount: 250,
  paidStudentsCount: 185,
  partialStudentsCount: 25,
  unpaidStudentsCount: 40
};

export const MOCK_TRANSACTIONS: FinancialTransaction[] = [
  {
    id: 'REC-2026-0104',
    workspaceId: 'ws-main',
    studentId: 'st-1042',
    studentName: 'أحمد محمد علي',
    studentCode: 'ST-1042',
    amount: 450.0,
    paymentMethod: 'Cash',
    recordedAtUtc: '2026-10-15T14:30:00Z',
    recordedByUserId: 'usr-acc-1',
    recordedByUserName: 'أحمد سمير (المحاسب)',
    status: 'Completed',
    allocations: [
      {
        id: 'alloc-1',
        paymentId: 'REC-2026-0104',
        chargeId: 'ch-101',
        amount: 450.0,
        isReversed: false,
        allocatedAtUtc: '2026-10-15T14:30:00Z'
      }
    ],
    notes: 'سداد كامل نقداً عن شهر أكتوبر'
  },
  {
    id: 'REC-2026-0103',
    workspaceId: 'ws-main',
    studentId: 'st-1043',
    studentName: 'سارة محمود خليل',
    studentCode: 'ST-1043',
    amount: 200.0,
    paymentMethod: 'VodafoneCash',
    transactionReference: 'VF-99214',
    recordedAtUtc: '2026-10-10T12:00:00Z',
    recordedByUserId: 'usr-acc-1',
    recordedByUserName: 'أحمد سمير (المحاسب)',
    status: 'Completed',
    allocations: [
      {
        id: 'alloc-2',
        paymentId: 'REC-2026-0103',
        chargeId: 'ch-102',
        amount: 200.0,
        isReversed: false,
        allocatedAtUtc: '2026-10-10T12:00:00Z'
      }
    ],
    notes: 'تحويل فودافون كاش جزء من المصروفات'
  },
  {
    id: 'REC-2026-0102',
    workspaceId: 'ws-main',
    studentId: 'st-1045',
    studentName: 'مريم حسن عثمان',
    studentCode: 'ST-1045',
    amount: 450.0,
    paymentMethod: 'InstaPay',
    transactionReference: 'IP-4412',
    recordedAtUtc: '2026-10-14T09:15:00Z',
    recordedByUserId: 'usr-acc-1',
    recordedByUserName: 'أحمد سمير (المحاسب)',
    status: 'Completed',
    allocations: [],
    notes: 'تحويل انستاباي'
  },
  {
    id: 'REC-2026-0101',
    workspaceId: 'ws-main',
    studentId: 'st-1047',
    studentName: 'نور الدين مصطفى',
    studentCode: 'ST-1047',
    amount: 450.0,
    paymentMethod: 'Cash',
    recordedAtUtc: '2026-10-08T16:20:00Z',
    recordedByUserId: 'usr-acc-1',
    recordedByUserName: 'أحمد سمير (المحاسب)',
    status: 'Reversed',
    reversalReason: 'خطأ إدخال كود الطالب - تم تحويل المبلغ لطالب آخر',
    reversedAtUtc: '2026-10-08T17:00:00Z',
    reversedByUserName: 'أحمد سمير (المحاسب)',
    allocations: [
      {
        id: 'alloc-3',
        paymentId: 'REC-2026-0101',
        chargeId: 'ch-104',
        amount: 450.0,
        isReversed: true,
        allocatedAtUtc: '2026-10-08T16:20:00Z'
      }
    ]
  }
];

export const MOCK_WHATSAPP_TEMPLATES: Record<string, WhatsAppTemplate> = {
  AttendanceCheckIn: {
    type: 'AttendanceCheckIn',
    titleAr: 'إشعار تسجيل الحضور',
    titleEn: 'Attendance Check-in Alert',
    rawText: 'نحيطكم علماً بأن الطالب {{اسم_الطالب}} قد حضر بنجاح إلى حصة {{اسم_المجموعة}} في تمام الساعة {{وقت_الحضور}}. مع تحيات {{اسم_السنتر}}.',
    availableTokens: ['{{اسم_الطالب}}', '{{اسم_المجموعة}}', '{{وقت_الحضور}}', '{{اسم_السنتر}}'],
    isEnabled: true,
    lastUpdatedUtc: '2026-10-18T10:00:00Z'
  },
  AbsenceAlert: {
    type: 'AbsenceAlert',
    titleAr: 'تنبيه غياب طالب',
    titleEn: 'Student Absence Alert',
    rawText: 'نلفت انتباهكم لغياب الطالب {{اسم_الطالب}} عن حصة {{اسم_المجموعة}} المنعقدة اليوم. يرجى التواصل لإبداء العذر. مع تحيات {{اسم_السنتر}}.',
    availableTokens: ['{{اسم_الطالب}}', '{{اسم_المجموعة}}', '{{اسم_السنتر}}'],
    isEnabled: true,
    lastUpdatedUtc: '2026-10-18T10:00:00Z'
  },
  ExamResult: {
    type: 'ExamResult',
    titleAr: 'نتيجة امتحان أكاديمي',
    titleEn: 'Exam Result Scorecard',
    rawText: 'يسرنا إبلاغكم بأن الطالب {{اسم_الطالب}} قد حقق درجة {{الدرجة}} في {{عنوان_الامتحان}}. مع أطيب تمنياتنا بالتفوق.',
    availableTokens: ['{{اسم_الطالب}}', '{{الدرجة}}', '{{عنوان_الامتحان}}', '{{اسم_السنتر}}'],
    isEnabled: true,
    lastUpdatedUtc: '2026-10-18T10:00:00Z'
  },
  WeeklyParentReport: {
    type: 'WeeklyParentReport',
    titleAr: 'تقرير الأداء الأسبوعي',
    titleEn: 'Weekly Performance Report',
    rawText: 'تقرير أسبوعي للطالب {{اسم_الطالب}}: نسبة الحضور: {{نسبة_الحضور}}%، الواجبات: {{حالة_الواجبات}}. مع تحيات {{اسم_السنتر}}.',
    availableTokens: ['{{اسم_الطالب}}', '{{نسبة_الحضور}}', '{{حالة_الواجبات}}', '{{اسم_السنتر}}'],
    isEnabled: true,
    lastUpdatedUtc: '2026-10-18T10:00:00Z'
  },
  PaymentReceipt: {
    type: 'PaymentReceipt',
    titleAr: 'إيصال استلام المصروفات',
    titleEn: 'Tuition Payment Receipt',
    rawText: 'تم استلام مبلغ {{المبلغ_المسدد}} ج.م رسوم شهرية للطالب {{اسم_الطالب}}، إيصال رقم {{رقم_الإيصال}}. شكراً لكم.',
    availableTokens: ['{{اسم_الطالب}}', '{{المبلغ_المسدد}}', '{{رقم_الإيصال}}', '{{اسم_السنتر}}'],
    isEnabled: true,
    lastUpdatedUtc: '2026-10-18T10:00:00Z'
  },
  AttendanceCorrection: {
    type: 'AttendanceCorrection',
    titleAr: 'تعديل وتصحيح حالة حضور',
    titleEn: 'Attendance Correction Notice',
    rawText: 'تصحيح إداري: تم تعديل حالة تسجيل الطالب {{اسم_الطالب}} في حصة {{اسم_المجموعة}} إلى {{الحالة_الجديدة}}.',
    availableTokens: ['{{اسم_الطالب}}', '{{اسم_المجموعة}}', '{{الحالة_الجديدة}}', '{{اسم_السنتر}}'],
    isEnabled: true,
    lastUpdatedUtc: '2026-10-18T10:00:00Z'
  }
};

export const MOCK_OUTBOX_LOGS: WhatsAppMessageLog[] = [
  {
    id: 'MSG-2026-8821',
    workspaceId: 'ws-main',
    studentId: 'st-1042',
    studentName: 'أحمد محمد علي',
    guardianPhone: '01012345670',
    messageType: 'AttendanceCheckIn',
    groupName: 'فيزياء 3 ث - أ',
    dispatchedAtUtc: new Date().toISOString(),
    status: 'Pending',
    bufferSecondsRemaining: 24,
    retryCount: 0,
    idempotencyKey: 'att_rec_1042_Present'
  },
  {
    id: 'MSG-2026-8820',
    workspaceId: 'ws-main',
    studentId: 'st-1043',
    studentName: 'سارة محمود خليل',
    guardianPhone: '01123456789',
    messageType: 'AttendanceCheckIn',
    groupName: 'فيزياء 3 ث - أ',
    dispatchedAtUtc: new Date().toISOString(),
    status: 'Pending',
    bufferSecondsRemaining: 18,
    retryCount: 0,
    idempotencyKey: 'att_rec_1043_Present'
  },
  {
    id: 'MSG-2026-8819',
    workspaceId: 'ws-main',
    studentId: 'st-1044',
    studentName: 'يوسف أحمد منصور',
    guardianPhone: '01234567890',
    messageType: 'AbsenceAlert',
    groupName: 'كيمياء 2 ث - ب',
    dispatchedAtUtc: '2026-10-21T01:10:00Z',
    status: 'Delivered',
    providerMessageId: 'wamid.HBgLMjAxMDU1Njc4OTA',
    retryCount: 0,
    idempotencyKey: 'att_rec_1044_Absent'
  },
  {
    id: 'MSG-2026-8818',
    workspaceId: 'ws-main',
    studentId: 'st-1045',
    studentName: 'مريم حسن عثمان',
    guardianPhone: '01512345678',
    messageType: 'PaymentReceipt',
    groupName: 'فيزياء 3 ث - ب',
    dispatchedAtUtc: '2026-10-21T00:45:00Z',
    status: 'Sent',
    providerMessageId: 'wamid.HBgLMjAxNTEyMzQ1Njc4',
    retryCount: 0,
    idempotencyKey: 'pay_rec_0102'
  },
  {
    id: 'MSG-2026-8817',
    workspaceId: 'ws-main',
    studentId: 'st-1046',
    studentName: 'عمر خالد إبراهيم',
    guardianPhone: '01099887766',
    messageType: 'AttendanceCheckIn',
    groupName: 'أحياء 1 ث - أ',
    dispatchedAtUtc: '2026-10-20T18:00:00Z',
    status: 'FailedPermanent',
    failureReason: 'رقم الهاتف غير مسجل على تطبيق واتساب (131026)',
    retryCount: 1,
    idempotencyKey: 'att_rec_1046_Present'
  },
  {
    id: 'MSG-2026-8816',
    workspaceId: 'ws-main',
    studentId: 'st-1047',
    studentName: 'نور الدين مصطفى',
    guardianPhone: '01155443322',
    messageType: 'AttendanceCheckIn',
    groupName: 'فيزياء 3 ث - أ',
    dispatchedAtUtc: '2026-10-20T17:50:00Z',
    status: 'FailedTransient',
    failureReason: 'انقطاع مؤقت في الاتصال بمزود الخدمة (503 Service Unavailable)',
    retryCount: 2,
    idempotencyKey: 'att_rec_1047_Present'
  }
];

export const MOCK_GUARDIAN_CONSENTS: GuardianConsentRecord[] = [
  {
    studentId: 'st-1042',
    studentName: 'أحمد محمد علي',
    studentCode: 'ST-1042',
    guardianPhone: '01012345670',
    whatsappOptIn: true,
    lastMessageAtUtc: '2026-10-21T01:30:00Z'
  },
  {
    studentId: 'st-1043',
    studentName: 'سارة محمود خليل',
    studentCode: 'ST-1043',
    guardianPhone: '01123456789',
    whatsappOptIn: true,
    lastMessageAtUtc: '2026-10-21T01:30:00Z'
  },
  {
    studentId: 'st-1044',
    studentName: 'يوسف أحمد منصور',
    studentCode: 'ST-1044',
    guardianPhone: '01234567890',
    whatsappOptIn: false,
    lastMessageAtUtc: '2026-10-15T09:00:00Z'
  }
];

export const MOCK_LMS_QUESTIONS: QuestionItem[] = [
  {
    id: 'q-101',
    subject: 'فيزياء',
    lesson: 'الحركة الموجية',
    difficulty: 'Easy',
    questionText: 'ما هي وحدة قياس التردد في النظام الدولي للوحدات؟',
    options: ['الجول (J)', 'الهرتز (Hz)', 'الواط (W)', 'النيوتن (N)'],
    correctAnswerIndex: 1,
    sourceType: 'Manual',
    isApproved: true,
    createdAtUtc: '2026-10-10T09:00:00Z'
  },
  {
    id: 'q-102',
    subject: 'فيزياء',
    lesson: 'الحركة الموجية',
    difficulty: 'Medium',
    questionText: 'إذا زاد تردد الموجة للضعف مع ثبوت سرعتها، فإن طولها الموجي:',
    options: ['يزداد للضعف', 'يقل للنصف', 'يظل ثابتاً', 'يتضاعف أربع مرات'],
    correctAnswerIndex: 1,
    sourceType: 'Manual',
    isApproved: true,
    createdAtUtc: '2026-10-10T09:30:00Z'
  },
  {
    id: 'q-103',
    subject: 'فيزياء',
    lesson: 'الضوء والانكسار',
    difficulty: 'Hard',
    questionText: 'النسبة بين جيب زاوية السقوط إلى جيب زاوية الانكسار تسمى:',
    options: ['معامل الانكسار المطلق', 'معامل الانكسار النسبي', 'زاوية الانحراف', 'الزاوية الحرجة'],
    correctAnswerIndex: 1,
    sourceType: 'AiGeneratedDraft',
    isApproved: false,
    createdAtUtc: '2026-10-19T14:20:00Z'
  },
  {
    id: 'q-104',
    subject: 'فيزياء',
    lesson: 'الحركة الدائرية',
    difficulty: 'Medium',
    questionText: 'تكون القوة الجاذبة المركزية دائماً في اتجاه:',
    options: ['مماس المسار الدائري', 'عمودي على نصف القطر للخارج', 'مركز المسار الدائري', 'عكس اتجاه الحركة'],
    correctAnswerIndex: 2,
    sourceType: 'AiApproved',
    isApproved: true,
    createdAtUtc: '2026-10-18T11:00:00Z'
  }
];

export const MOCK_LMS_EXAMS: ExamItem[] = [
  {
    id: 'ex-101',
    workspaceId: 'ws-main',
    title: 'امتحان الفيزياء الأسبوعي - الحركة الموجية',
    groupId: 'grp-1',
    groupName: 'فيزياء 3 ث - أ',
    durationMinutes: 45,
    maxAttempts: 1,
    totalMarks: 20,
    questionsCount: 4,
    questionIds: ['q-101', 'q-102', 'q-104'],
    submissionsCount: 28,
    averageScorePercent: 82.5,
    status: 'Published',
    autoGradingEnabled: true,
    autoSendWhatsappResults: true,
    publishDateUtc: '2026-10-20T08:00:00Z'
  },
  {
    id: 'ex-102',
    workspaceId: 'ws-main',
    title: 'امتحان منتصف الفصل - الكيمياء العضوية',
    groupId: 'grp-2',
    groupName: 'كيمياء 2 ث - ب',
    durationMinutes: 60,
    maxAttempts: 1,
    totalMarks: 30,
    questionsCount: 15,
    questionIds: [],
    submissionsCount: 0,
    averageScorePercent: 0,
    status: 'Draft',
    autoGradingEnabled: true,
    autoSendWhatsappResults: true,
    publishDateUtc: '2026-10-25T10:00:00Z'
  }
];

export const MOCK_LMS_HOMEWORK: HomeworkItem[] = [
  {
    id: 'hw-101',
    workspaceId: 'ws-main',
    title: 'واجب المسائل الرياضية على التردد والسرعة',
    groupId: 'grp-1',
    groupName: 'فيزياء 3 ث - أ',
    dueDateUtc: '2026-10-24T23:59:00Z',
    maxScore: 10,
    submittedCount: 22,
    lateCount: 4,
    missingCount: 6,
    status: 'Active'
  }
];

export const MOCK_LMS_LIBRARY: LibraryItem[] = [
  {
    id: 'lib-101',
    workspaceId: 'ws-main',
    title: 'مذكرة القوانين والمسائل المحلولة - الفصل الأول',
    groupId: 'grp-1',
    groupName: 'فيزياء 3 ث - أ',
    fileType: 'PDF',
    fileSizeMb: 4.8,
    downloadUrl: '/assets/sample.pdf',
    uploadedAtUtc: '2026-10-15T09:00:00Z'
  },
  {
    id: 'lib-102',
    workspaceId: 'ws-main',
    title: 'فيديو توضيحي لتجربة الشق المزدوج ليونج',
    groupId: 'grp-1',
    groupName: 'فيزياء 3 ث - أ',
    fileType: 'Video',
    fileSizeMb: 24.5,
    downloadUrl: '/assets/sample.mp4',
    uploadedAtUtc: '2026-10-16T11:30:00Z'
  }
];

export const MOCK_STAFF_MEMBERS: WorkspaceStaffMember[] = [
  {
    id: 'usr-1',
    name: 'أ. محمد عبد الرحمن',
    username: 'mohamed_abdelrahman',
    email: 'mohamed@tahdeera.com',
    phone: '01019482687',
    role: 'Teacher',
    isActive: true,
    lastActiveUtc: '2026-10-21T01:30:00Z',
    createdAtUtc: '2026-09-01T08:00:00Z'
  },
  {
    id: 'usr-2',
    name: 'محمود السيد (مساعد أول)',
    username: 'mahmoud_sayed',
    email: 'mahmoud@tahdeera.com',
    phone: '01123456789',
    role: 'Assistant',
    isActive: true,
    lastActiveUtc: '2026-10-21T01:25:00Z',
    createdAtUtc: '2026-09-05T09:00:00Z'
  },
  {
    id: 'usr-3',
    name: 'سارة إبراهيم (مساعد حضور)',
    username: 'sara_ibrahim',
    email: 'sara@tahdeera.com',
    phone: '01234567890',
    role: 'Assistant',
    isActive: true,
    lastActiveUtc: '2026-10-20T16:00:00Z',
    createdAtUtc: '2026-09-10T10:00:00Z'
  },
  {
    id: 'usr-4',
    name: 'مروة كمال (الاستقبال)',
    username: 'marwa_kamal',
    email: 'marwa@tahdeera.com',
    phone: '01512345678',
    role: 'Reception',
    isActive: true,
    lastActiveUtc: '2026-10-21T00:50:00Z',
    createdAtUtc: '2026-09-12T08:30:00Z'
  },
  {
    id: 'usr-5',
    name: 'أحمد سمير (المحاسب)',
    username: 'ahmed_samir',
    email: 'ahmed_samir@tahdeera.com',
    phone: '01099887766',
    role: 'Accountant',
    isActive: true,
    lastActiveUtc: '2026-10-21T01:15:00Z',
    createdAtUtc: '2026-09-15T09:30:00Z'
  }
];

export const MOCK_WORKSPACE_SETTINGS: WorkspaceSettings = {
  workspaceId: 'ws-main',
  centerName: 'سنتر الأوائل التعليمي',
  teacherName: 'أ. محمد عبد الرحمن',
  phone: '01019482687',
  address: 'شارع جمال عبد الناصر، عمارة الأطباء، الدور الثاني - القاهرة',
  defaultLateGraceMinutes: 15,
  weeklyHolidays: [5, 6], // Friday and Saturday
  notifyOnSubscriptionWarning: true,
  notifyOnWhatsappFailure: true,
  profile: {
    centerNameAr: 'سنتر الأوائل التعليمي',
    centerNameEn: 'Al-Awael Educational Center',
    centerPhone: '01019482687',
    address: 'شارع جمال عبد الناصر، عمارة الأطباء، الدور الثاني',
    governorate: 'القاهرة',
    taxId: '984-210-441'
  },
  attendancePolicies: {
    lateThresholdMinutes: 15,
    autoAbsentThresholdMinutes: 45,
    autoSendAbsenceWhatsApp: true,
    autoSendCheckInWhatsApp: true
  }
};

export const MOCK_SUBSCRIPTION_VIEW: ReadOnlySubscriptionView = {
  planName: 'باقة المحترفين - Pro',
  packageNameAr: 'الباقة الاحترافية',
  packagePlan: 'Pro',
  status: 'Active',
  startDate: '2026-09-01',
  endDate: '2026-12-08',
  expiresAt: '2026-12-08T23:59:59Z',
  remainingDays: 48,
  supportHotline1: '01272785618',
  supportHotline2: '01019482687',
  activeStudentsCount: 248,
  maxStudentsLimit: 300,
  currentStaffCount: 4,
  maxStaffLimit: 8,
  activeFeatures: [
    'تسجيل الحضور الذكي NFC',
    'إشعارات واتساب غير المحدودة',
    'الامتحانات والتصحيح التلقائي LMS',
    'مساعد الذكاء الاصطناعي الأكاديمي',
    'تصدير تقارير Excel المتقدمة'
  ]
};


// Aliases for convenience in specs
export const MOCK_EXAMS = MOCK_LMS_EXAMS;
export const MOCK_QUESTIONS = MOCK_LMS_QUESTIONS;
export const MOCK_HOMEWORK = MOCK_LMS_HOMEWORK;
export const MOCK_LIBRARY = MOCK_LMS_LIBRARY;
export const MOCK_CONSENTS = MOCK_GUARDIAN_CONSENTS;

export const MOCK_PROMPT_CHIPS = [
  { id: 'chip-1', titleAr: 'الطلاب المتأخرين', labelAr: 'الطلاب المتأخرين', labelEn: 'At Risk Students', prompt: 'الطلاب الأكثر تخلفاً عن سداد المصروفات ونسبة غيابهم مرتفعة', promptText: 'الطلاب الأكثر تخلفاً عن سداد المصروفات ونسبة غيابهم مرتفعة', category: 'Risk' },
  { id: 'chip-2', titleAr: 'مقارنة الحضور', labelAr: 'مقارنة الحضور', labelEn: 'Compare Attendance', prompt: 'مقارنة نسب الحضور بين مجموعات الصف الثالث الثانوي', promptText: 'مقارنة نسب الحضور بين مجموعات الصف الثالث الثانوي', category: 'Academics' },
  { id: 'chip-3', titleAr: 'توليد أسئلة', labelAr: 'توليد أسئلة', labelEn: 'Generate Questions', prompt: 'اقتراح 5 أسئلة اختيار من متعدد فيزياء عن الحركة الموجية', promptText: 'اقتراح 5 أسئلة اختيار من متعدد فيزياء عن الحركة الموجية', category: 'Academics' }
];

export const MOCK_AI_RESPONSES: Record<string, any> = {
  'من هم الطلاب الأكثر غياباً؟': {
    queryId: 'ai-mock-1',
    isOutOfScope: false,
    analysisNarrativeAr: 'بناءً على سجلات الحضور: تم رصد 3 طلاب تجاوزت نسبة غيابهم 25% هذا الشهر.',
    analysisNarrativeEn: 'Based on attendance logs: 3 students have absence rates over 25% this month.',
    resultTable: {
      headers: ['اسم الطالب', 'كود الطالب', 'المجموعة', 'نسبة الغياب'],
      rows: [
        ['أحمد محمد علي', 'ST-1042', 'فيزياء 3 ث - أ', '28%'],
        ['سارة محمود خليل', 'ST-1043', 'فيزياء 3 ث - أ', '25%']
      ]
    },
    queriesUsedToday: 15,
    dailyLimit: 50
  }
};

export const MOCK_REPORTS_CATALOG = [
  { id: 'payments-ledger' as const, titleAr: 'دفتر يومية المقبوضات المالية', titleEn: 'Payments Ledger', descAr: 'سجل حركات السداد والإيصالات المستخرجة', descEn: 'All payments ledger', icon: 'account_balance_wallet', category: 'Finance' as const },
  { id: 'daily-attendance' as const, titleAr: 'سجل الحضور اليومي للمجموعات', titleEn: 'Daily Attendance', descAr: 'تفاصيل الحضور والغياب والتأخير', descEn: 'Daily attendance roster', icon: 'fact_check', category: 'Attendance' as const },
  { id: 'unpaid-students' as const, titleAr: 'تقرير الطلاب المتأخرين عن السداد', titleEn: 'Overdue Students', descAr: 'قائمة الطلاب غير المسددين للمصروفات', descEn: 'Students with unpaid charges', icon: 'money_off', category: 'Finance' as const },
  { id: 'whatsapp-status' as const, titleAr: 'تقرير تسليم رسائل الواتساب', titleEn: 'WhatsApp Delivery', descAr: 'حالة تسليم الرسائل ومعدلات الفشل', descEn: 'WhatsApp outbox logs', icon: 'chat', category: 'WhatsApp' as const }
];

export const MOCK_REPORT_RESULTS: Record<string, any> = {
  'payments-ledger': {
    reportId: 'payments-ledger' as const,
    title: 'دفتر يومية المقبوضات المالية',
    generatedAtUtc: '2026-10-21T10:00:00Z',
    headers: ['رقم الإيصال', 'كود الطالب', 'اسم الطالب', 'المبلغ (ج.م)', 'طريقة الدفع', 'المسؤول', 'الحالة'],
    rows: [
      ['REC-2026-0042', 'ST-1042', 'أحمد محمد علي', 450, 'Cash', 'محاسب السنتر', 'مكتملة']
    ]
  },
  'daily-attendance': {
    reportId: 'daily-attendance' as const,
    title: 'سجل الحضور اليومي للمجموعات',
    generatedAtUtc: '2026-10-21T10:00:00Z',
    headers: ['كود الطالب', 'اسم الطالب', 'المجموعة', 'الحالة', 'وقت التسجيل'],
    rows: [
      ['ST-1042', 'أحمد محمد علي', 'فيزياء 3 ث - أ', 'حاضر', '04:30 م']
    ]
  }
};
