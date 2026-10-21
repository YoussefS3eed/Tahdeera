/**
 * Milestone F4 Mock HTTP Interceptor & Handlers
 * Simulates REST APIs for Financial Ledger, WhatsApp Outbox, LMS, AI BI, Reports, Team & Settings
 * Strictly follows contracts/ in specs/012-f4-finance-whatsapp-lms-governance
 */

import { HttpRequest, HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import {
  MOCK_BILLING_ROSTER,
  MOCK_TRANSACTIONS,
  MOCK_FINANCIAL_KPIS,
  MOCK_STUDENT_CHARGES,
  MOCK_WHATSAPP_TEMPLATES,
  MOCK_OUTBOX_LOGS,
  MOCK_GUARDIAN_CONSENTS,
  MOCK_LMS_QUESTIONS,
  MOCK_LMS_EXAMS,
  MOCK_LMS_HOMEWORK,
  MOCK_LMS_LIBRARY,
  MOCK_STAFF_MEMBERS,
  MOCK_WORKSPACE_SETTINGS,
  MOCK_SUBSCRIPTION_VIEW
} from './f4-mock-data';
import { StudentBillingSummary, FinancialTransaction, StudentCharge } from '../models/finance.models';
import { WhatsAppTemplate, WhatsAppMessageLog, GuardianConsentRecord } from '../models/whatsapp.models';
import { QuestionItem, ExamItem, HomeworkItem, LibraryItem, ExamSubmission } from '../models/lms.models';
import { WorkspaceStaffMember, WorkspaceSettings } from '../models/team-settings.models';

// In-Memory state stores
let billingRoster = [...MOCK_BILLING_ROSTER];
let transactionsLedger = [...MOCK_TRANSACTIONS];
let studentCharges = [...MOCK_STUDENT_CHARGES];
let templatesStore = { ...MOCK_WHATSAPP_TEMPLATES };
let outboxStore = [...MOCK_OUTBOX_LOGS];
let consentsStore = [...MOCK_GUARDIAN_CONSENTS];
let questionsStore = [...MOCK_LMS_QUESTIONS];
let examsStore = [...MOCK_LMS_EXAMS];
let homeworkStore = [...MOCK_LMS_HOMEWORK];
let libraryStore = [...MOCK_LMS_LIBRARY];
let staffStore = [...MOCK_STAFF_MEMBERS];
let settingsStore = { ...MOCK_WORKSPACE_SETTINGS };

function createProblem(status: number, title: string, detail: string, errorCode: string, url: string): HttpErrorResponse {
  return new HttpErrorResponse({
    status,
    statusText: title,
    error: {
      type: `https://errors.tahdeera.com/f4/${errorCode.toLowerCase()}`,
      title,
      status,
      detail,
      instance: url,
      errorCode,
      traceId: `00-${Math.random().toString(16).substring(2, 18)}-01`
    }
  });
}

export function handleF4MockRequest(req: HttpRequest<any>): Observable<any> | null {
  const url = req.url;
  const method = req.method;

  // ----------------------------------------------------
  // 1. Finance Endpoints (SCR-12)
  // ----------------------------------------------------
  if (url.includes('/payments/summary') && method === 'GET') {
    return of(new HttpResponse({
      status: 200,
      body: {
        kpis: MOCK_FINANCIAL_KPIS,
        students: billingRoster
      }
    })).pipe(delay(50));
  }

  if (url.includes('/payments/ledger') && method === 'GET') {
    return of(new HttpResponse({
      status: 200,
      body: {
        transactions: transactionsLedger
      }
    })).pipe(delay(50));
  }

  if (url.match(/\/payments$/) && method === 'POST') {
    const body = req.body || {};
    const amount = Number(body.amount);
    if (!amount || amount <= 0 || amount > 100000) {
      return throwError(() => createProblem(422, 'Invalid Payment Amount', 'Amount must be between 1 and 100,000 EGP', 'INVALID_PAYMENT_AMOUNT', url));
    }
    if ((body.paymentMethod === 'VodafoneCash' || body.paymentMethod === 'InstaPay') && (!body.transactionReference || body.transactionReference.length < 4)) {
      return throwError(() => createProblem(422, 'Missing Reference', 'VodafoneCash and InstaPay require a reference of at least 4 characters', 'MISSING_TRANSACTION_REFERENCE', url));
    }

    const student = billingRoster.find(s => s.studentId === body.studentId);
    const recId = `REC-2026-${String(transactionsLedger.length + 105).padStart(4, '0')}`;
    const newTx: FinancialTransaction = {
      id: recId,
      workspaceId: 'ws-main',
      studentId: body.studentId,
      studentName: student ? student.studentName : (body.studentName || 'طالب تجريبي'),
      studentCode: student ? student.studentCode : (body.studentCode || 'ST-9999'),
      amount: amount,
      paymentMethod: body.paymentMethod,
      transactionReference: body.transactionReference,
      recordedAtUtc: new Date().toISOString(),
      recordedByUserId: 'usr-current',
      recordedByUserName: 'المحاسب المالي',
      status: 'Completed',
      allocations: [
        {
          id: `alloc-${Date.now()}`,
          paymentId: recId,
          chargeId: 'ch-auto',
          amount: amount,
          isReversed: false,
          allocatedAtUtc: new Date().toISOString()
        }
      ],
      notes: body.notes
    };
    transactionsLedger.unshift(newTx);

    // Update billing summary if student exists
    if (student) {
      student.paidAmount += amount;
      student.remainingFee = Math.max(0, student.requiredFee - student.paidAmount);
      student.paymentStatus = student.remainingFee === 0 ? 'Paid' : (student.paidAmount > 0 ? 'Partial' : 'Unpaid');
      student.lastPaymentDate = newTx.recordedAtUtc;
    }

    return of(new HttpResponse({ status: 201, body: newTx })).pipe(delay(50));
  }

  const reverseMatch = url.match(/\/payments\/([^/]+)\/reverse$/);
  if (reverseMatch && method === 'POST') {
    const paymentId = reverseMatch[1];
    const body = req.body || {};
    const reason = (body.reversalReason || '').trim();
    if (reason.length < 5) {
      return throwError(() => createProblem(422, 'Invalid Reversal Reason', 'Reversal reason must be at least 5 characters', 'INVALID_REVERSAL_REASON', url));
    }

    const tx = transactionsLedger.find(t => t.id === paymentId);
    if (!tx) {
      return throwError(() => createProblem(404, 'Transaction Not Found', 'Payment transaction was not found', 'TRANSACTION_NOT_FOUND', url));
    }
    if (tx.status === 'Reversed') {
      return throwError(() => createProblem(409, 'Already Reversed', 'Transaction has already been reversed', 'TRANSACTION_ALREADY_REVERSED', url));
    }

    tx.status = 'Reversed';
    tx.reversalReason = reason;
    tx.reversedAtUtc = new Date().toISOString();
    tx.reversedByUserName = 'المحاسب المالي';
    tx.allocations?.forEach(a => a.isReversed = true);

    const student = billingRoster.find(s => s.studentId === tx.studentId);
    if (student) {
      student.paidAmount = Math.max(0, student.paidAmount - tx.amount);
      student.remainingFee = Math.max(0, student.requiredFee - student.paidAmount);
      student.paymentStatus = student.remainingFee === 0 ? 'Paid' : (student.paidAmount > 0 ? 'Partial' : 'Unpaid');
    }

    return of(new HttpResponse({ status: 200, body: tx })).pipe(delay(50));
  }

  // ----------------------------------------------------
  // 2. WhatsApp Endpoints (SCR-13)
  // ----------------------------------------------------
  if (url.includes('/whatsapp/templates') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: templatesStore })).pipe(delay(50));
  }

  const tplUpdateMatch = url.match(/\/whatsapp\/templates\/([^/]+)$/);
  if (tplUpdateMatch && method === 'PUT') {
    const type = tplUpdateMatch[1];
    if (templatesStore[type]) {
      templatesStore[type] = {
        ...templatesStore[type],
        rawText: req.body.rawText,
        lastUpdatedUtc: new Date().toISOString()
      };
      return of(new HttpResponse({ status: 200, body: templatesStore[type] })).pipe(delay(50));
    }
    return throwError(() => createProblem(404, 'Template Not Found', 'Template type not recognized', 'TEMPLATE_NOT_FOUND', url));
  }

  if (url.includes('/whatsapp/outbox') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: outboxStore })).pipe(delay(50));
  }

  const cancelOutboxMatch = url.match(/\/whatsapp\/outbox\/([^/]+)\/cancel$/);
  if (cancelOutboxMatch && method === 'POST') {
    const msgId = cancelOutboxMatch[1];
    const item = outboxStore.find(m => m.id === msgId);
    if (!item) {
      return throwError(() => createProblem(404, 'Message Not Found', 'Message log entry not found', 'MESSAGE_NOT_FOUND', url));
    }
    if (item.status !== 'Pending') {
      return throwError(() => createProblem(409, 'Message Already Dispatched', 'Only Pending messages can be cancelled', 'MESSAGE_ALREADY_DISPATCHED', url));
    }
    item.status = 'Cancelled';
    return of(new HttpResponse({ status: 200, body: item })).pipe(delay(50));
  }

  const retryOutboxMatch = url.match(/\/whatsapp\/outbox\/([^/]+)\/retry$/);
  if (retryOutboxMatch && method === 'POST') {
    const msgId = retryOutboxMatch[1];
    const item = outboxStore.find(m => m.id === msgId);
    if (!item) {
      return throwError(() => createProblem(404, 'Message Not Found', 'Message log entry not found', 'MESSAGE_NOT_FOUND', url));
    }
    item.status = 'Pending';
    item.bufferSecondsRemaining = 30;
    item.retryCount += 1;
    return of(new HttpResponse({ status: 200, body: item })).pipe(delay(50));
  }

  if (url.includes('/whatsapp/consents') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: consentsStore })).pipe(delay(50));
  }

  const consentToggleMatch = url.match(/\/whatsapp\/consents\/([^/]+)$/);
  if (consentToggleMatch && method === 'PUT') {
    const studentId = consentToggleMatch[1];
    const consent = consentsStore.find(c => c.studentId === studentId);
    if (consent) {
      consent.whatsappOptIn = req.body.whatsappOptIn;
      return of(new HttpResponse({ status: 200, body: consent })).pipe(delay(50));
    }
    return throwError(() => createProblem(404, 'Student Not Found', 'Student consent record not found', 'STUDENT_NOT_FOUND', url));
  }

  // ----------------------------------------------------
  // 3. LMS & Student Portal Endpoints (SCR-14)
  // ----------------------------------------------------
  if (url.includes('/lms/exams') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: examsStore })).pipe(delay(50));
  }

  if (url.includes('/lms/exams') && method === 'POST') {
    const body = req.body;
    const newExam: ExamItem = {
      id: `ex-${Date.now()}`,
      workspaceId: 'ws-main',
      title: body.title,
      groupId: body.groupId,
      groupName: body.groupName || 'مجموعة أ',
      durationMinutes: body.durationMinutes || 45,
      maxAttempts: body.maxAttempts || 1,
      totalMarks: body.totalMarks || 20,
      questionsCount: body.questionIds?.length || 0,
      questionIds: body.questionIds || [],
      submissionsCount: 0,
      averageScorePercent: 0,
      status: body.status || 'Draft',
      autoGradingEnabled: body.autoGradingEnabled ?? true,
      autoSendWhatsappResults: body.autoSendWhatsappResults ?? true,
      publishDateUtc: new Date().toISOString()
    };
    examsStore.unshift(newExam);
    return of(new HttpResponse({ status: 201, body: newExam })).pipe(delay(50));
  }

  if (url.includes('/lms/questions') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: questionsStore })).pipe(delay(50));
  }

  if (url.includes('/lms/questions') && method === 'POST') {
    const body = req.body;
    const newQ: QuestionItem = {
      id: `q-${Date.now()}`,
      subject: body.subject || 'فيزياء',
      lesson: body.lesson || 'درس عام',
      difficulty: body.difficulty || 'Medium',
      questionText: body.questionText,
      options: body.options,
      correctAnswerIndex: body.correctAnswerIndex,
      sourceType: body.sourceType || 'Manual',
      isApproved: body.sourceType === 'Manual' ? true : false,
      createdAtUtc: new Date().toISOString()
    };
    questionsStore.unshift(newQ);
    return of(new HttpResponse({ status: 201, body: newQ })).pipe(delay(50));
  }

  const approveQMatch = url.match(/\/lms\/questions\/([^/]+)\/approve$/);
  if (approveQMatch && method === 'POST') {
    const qId = approveQMatch[1];
    const q = questionsStore.find(item => item.id === qId);
    if (!q) {
      return throwError(() => createProblem(404, 'Question Not Found', 'Question not found in bank', 'QUESTION_NOT_FOUND', url));
    }
    q.sourceType = 'AiApproved';
    q.isApproved = true;
    return of(new HttpResponse({ status: 200, body: q })).pipe(delay(50));
  }

  if (url.includes('/lms/homework') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: homeworkStore })).pipe(delay(50));
  }

  if (url.includes('/lms/library') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: libraryStore })).pipe(delay(50));
  }

  // Public Student Exam Portal
  const portalAuthMatch = url.match(/\/portal\/exams\/([^/]+)\/authenticate$/);
  if (portalAuthMatch && method === 'POST') {
    const examId = portalAuthMatch[1];
    const { studentCode, verificationPin } = req.body || {};
    if (!studentCode || !verificationPin || verificationPin.length !== 4) {
      return throwError(() => createProblem(401, 'Invalid Credentials', 'Invalid student code or 4-digit PIN', 'INVALID_STUDENT_CREDENTIALS', url));
    }
    const exam = examsStore.find(e => e.id === examId) || examsStore[0];
    return of(new HttpResponse({
      status: 200,
      body: {
        token: 'portal-student-jwt-token',
        student: {
          studentId: 'st-1042',
          studentName: 'أحمد محمد علي',
          studentCode: studentCode
        },
        exam
      }
    })).pipe(delay(50));
  }

  const portalExamMatch = url.match(/\/portal\/exams\/([^/]+)$/);
  if (portalExamMatch && method === 'GET') {
    const examId = portalExamMatch[1];
    const exam = examsStore.find(e => e.id === examId) || examsStore[0];
    const questions = questionsStore
      .filter(q => q.isApproved)
      .map(q => ({
        id: q.id,
        subject: q.subject,
        lesson: q.lesson,
        difficulty: q.difficulty,
        questionText: q.questionText,
        options: q.options
      }));
    return of(new HttpResponse({
      status: 200,
      body: {
        exam,
        questions
      }
    })).pipe(delay(50));
  }

  const portalSubmitMatch = url.match(/\/portal\/exams\/([^/]+)\/submit$/);
  if (portalSubmitMatch && method === 'POST') {
    const examId = portalSubmitMatch[1];
    const answers = req.body.answers || [];
    let correctCount = 0;
    answers.forEach((ans: any) => {
      const q = questionsStore.find(item => item.id === ans.questionId);
      if (q && q.correctAnswerIndex === ans.selectedOptionIndex) {
        correctCount++;
        ans.isCorrect = true;
        ans.marksAwarded = 5;
      } else {
        ans.isCorrect = false;
        ans.marksAwarded = 0;
      }
    });

    const totalMarks = Math.max(20, answers.length * 5);
    const score = correctCount * 5;
    const submission: ExamSubmission = {
      id: `sub-${Date.now()}`,
      examId,
      studentId: req.body.studentId || 'st-1042',
      studentCode: req.body.studentCode || 'ST-1042',
      attemptNumber: 1,
      score,
      totalMarks,
      percentage: Math.round((score / totalMarks) * 100),
      submittedAtUtc: new Date().toISOString(),
      answers
    };

    return of(new HttpResponse({ status: 201, body: submission })).pipe(delay(50));
  }

  // ----------------------------------------------------
  // 4. AI Assistant Endpoints (SCR-15)
  // ----------------------------------------------------
  if (url.includes('/ai/query') && method === 'POST') {
    const queryText = (req.body.queryText || '').trim();
    const isOut = queryText.includes('فرنسا') || queryText.includes('عاصمة') || queryText.length < 5;

    if (isOut) {
      return of(new HttpResponse({
        status: 200,
        body: {
          queryId: `ai-${Date.now()}`,
          isOutOfScope: true,
          analysisNarrativeAr: 'عذراً، المساعد الذكي مخصص فقط لتحليل بيانات سنترك الأكاديمية والتشغيلية المباشرة.',
          analysisNarrativeEn: 'Sorry, the AI Assistant is dedicated to operational inquiries within your educational center.',
          suggestedPromptChips: [
            'الطلاب الأكثر تخلفاً عن سداد المصروفات ونسبة غيابهم مرتفعة',
            'مقارنة نسب الحضور بين مجموعات الصف الثالث الثانوي',
            'اقتراح 5 أسئلة اختيار من متعدد فيزياء عن الحركة الموجية'
          ],
          queriesUsedToday: 15,
          dailyLimit: 50
        }
      })).pipe(delay(100));
    }

    return of(new HttpResponse({
      status: 200,
      body: {
        queryId: `ai-${Date.now()}`,
        isOutOfScope: false,
        analysisNarrativeAr: `بناءً على سجلات الحضور والمصروفات لشهر أكتوبر 2026: تم رصد 3 مجموعات تحتاج إلى متابعة إدارية نظراً لتجاوز نسبة الغياب 18% وتأخر سداد الرسوم الشهرية.`,
        analysisNarrativeEn: `Based on October 2026 logs, 3 groups require administrative follow-up due to high absence rates (>18%) and delayed tuition.`,
        resultTable: {
          headers: ['المجموعة', 'عدد الطلاب', 'نسبة الحضور', 'إجمالي المتأخرات'],
          rows: [
            ['فيزياء 3 ث - أ', 35, '82%', '3,150 ج.م'],
            ['كيمياء 2 ث - ب', 28, '79%', '2,700 ج.م'],
            ['أحياء 1 ث - أ', 40, '74%', '4,000 ج.م']
          ]
        },
        queriesUsedToday: 15,
        dailyLimit: 50
      }
    })).pipe(delay(150));
  }

  // ----------------------------------------------------
  // 5. Reports Endpoints (SCR-16)
  // ----------------------------------------------------
  const reportGenerateMatch = url.match(/\/reports\/([^/]+)\/generate$/);
  if (reportGenerateMatch && method === 'POST') {
    const reportId = reportGenerateMatch[1];
    let headers: string[] = [];
    let rows: (string | number)[][] = [];

    if (reportId === 'payments-ledger') {
      headers = ['رقم الإيصال', 'كود الطالب', 'اسم الطالب', 'المبلغ (ج.م)', 'طريقة الدفع', 'المسؤول', 'الحالة'];
      rows = transactionsLedger.map(t => [
        t.id ?? '',
        t.studentCode ?? '',
        t.studentName ?? '',
        t.amount ?? 0,
        t.paymentMethod ?? '',
        t.recordedByUserName ?? '',
        t.status === 'Completed' ? 'مكتملة' : 'ملغاة'
      ]);
    } else {
      headers = ['كود الطالب', 'اسم الطالب', 'المجموعة', 'الحالة', 'الرسوم المسددة'];
      rows = billingRoster.map(s => [
        s.studentCode,
        s.studentName,
        s.groupName,
        s.paymentStatus === 'Paid' ? 'مسدد' : (s.paymentStatus === 'Partial' ? 'جزئي' : 'غير مسدد'),
        s.paidAmount
      ]);
    }

    return of(new HttpResponse({
      status: 200,
      body: {
        reportId,
        title: 'تقرير تشغيلي معتمد',
        generatedAtUtc: new Date().toISOString(),
        headers,
        rows,
        totalsSummary: {
          'إجمالي السجلات': rows.length
        }
      }
    })).pipe(delay(50));
  }

  // ----------------------------------------------------
  // 6. Local Team & Settings Endpoints (SCR-17 & SCR-18)
  // ----------------------------------------------------
  if (url.includes('/members') && method === 'GET') {
    return of(new HttpResponse({
      status: 200,
      body: {
        members: staffStore,
        maxStaff: 8
      }
    })).pipe(delay(50));
  }

  if (url.includes('/members') && method === 'POST') {
    if (staffStore.filter(s => s.isActive).length >= 8) {
      return throwError(() => createProblem(422, 'Staff Quota Reached', 'Your workspace package limit of 8 staff members has been reached', 'QUOTA_MAX_STAFF_REACHED', url));
    }
    const body = req.body;
    const newMember: WorkspaceStaffMember = {
      id: `usr-${Date.now()}`,
      name: body.displayName,
      username: body.username,
      email: body.email,
      phone: body.phone,
      role: body.role,
      isActive: true,
      lastActiveUtc: undefined,
      createdAtUtc: new Date().toISOString()
    };
    staffStore.push(newMember);
    return of(new HttpResponse({ status: 201, body: newMember })).pipe(delay(50));
  }

  const deactivateMemberMatch = url.match(/\/members\/([^/]+)$/);
  if (deactivateMemberMatch && method === 'DELETE') {
    const memberId = deactivateMemberMatch[1];
    const m = staffStore.find(item => item.id === memberId);
    if (m) {
      m.isActive = false;
      return of(new HttpResponse({ status: 200, body: m })).pipe(delay(50));
    }
    return throwError(() => createProblem(404, 'Member Not Found', 'Staff member not found', 'MEMBER_NOT_FOUND', url));
  }

  if (url.includes('/settings') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: settingsStore })).pipe(delay(50));
  }

  if (url.includes('/settings') && method === 'PUT') {
    settingsStore = { ...settingsStore, ...req.body };
    return of(new HttpResponse({ status: 200, body: settingsStore })).pipe(delay(50));
  }

  if (url.includes('/subscription') && method === 'GET') {
    return of(new HttpResponse({ status: 200, body: MOCK_SUBSCRIPTION_VIEW })).pipe(delay(50));
  }

  return null;
}
