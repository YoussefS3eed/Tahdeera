import { HttpEvent, HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of, throwError } from 'rxjs';
import {
  INITIAL_ADMIN_AUDIT_LOGS,
  INITIAL_ADMIN_KPIS,
  INITIAL_ADMIN_WORKSPACES,
  INITIAL_GLOBAL_SETTINGS,
  INITIAL_PACKAGE_TEMPLATES,
  INITIAL_PAYMENTS_LEDGER,
  INITIAL_WHATSAPP_CONFIG,
  INITIAL_WHATSAPP_STATS
} from './admin-mock-data';
import {
  AdminAuditLogEntry,
  AdminWorkspaceSummary,
  PackageTemplate,
  PlatformGlobalSettings,
  PlatformSubscriptionPayment,
  WhatsAppProviderConfig,
  WhatsAppTestPingResult
} from '../models/admin.models';

let workspaces: AdminWorkspaceSummary[] = [...INITIAL_ADMIN_WORKSPACES];
let templates: PackageTemplate[] = [...INITIAL_PACKAGE_TEMPLATES];
let payments: PlatformSubscriptionPayment[] = [...INITIAL_PAYMENTS_LEDGER];
let whatsappConfig: WhatsAppProviderConfig = { ...INITIAL_WHATSAPP_CONFIG };
let globalSettings: PlatformGlobalSettings = { ...INITIAL_GLOBAL_SETTINGS };
let auditLogs: AdminAuditLogEntry[] = [...INITIAL_ADMIN_AUDIT_LOGS];

export const adminMockInterceptor: HttpHandlerFn = (req: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  const url = req.url;
  const method = req.method;

  if (!url.includes('/api/v1/admin')) {
    return throwError(() => new Error('Pass through'));
  }

  // GET /api/v1/admin/overview
  if (method === 'GET' && url.endsWith('/api/v1/admin/overview')) {
    const watchlist = workspaces
      .filter(w => (w.daysRemaining ?? 0) <= 7 && w.subscriptionStatus !== 'Expired')
      .map(w => ({
        workspaceId: w.id,
        workspaceName: w.name,
        workspaceCode: w.code,
        type: w.type,
        expiryDate: w.subscriptionExpiresAt,
        daysRemaining: w.daysRemaining,
        packageTier: w.packageTier,
        agreedPrice: w.contractedPrice
      }));

    return of(new HttpResponse({
      status: 200,
      body: {
        kpis: INITIAL_ADMIN_KPIS,
        watchlist,
        recentAudits: auditLogs.slice(0, 5)
      }
    })).pipe(delay(150));
  }

  // GET /api/v1/admin/workspaces
  if (method === 'GET' && url.includes('/api/v1/admin/workspaces') && !url.includes('/overrides')) {
    return of(new HttpResponse({
      status: 200,
      body: workspaces
    })).pipe(delay(150));
  }

  // POST /api/v1/admin/workspaces
  if (method === 'POST' && url.endsWith('/api/v1/admin/workspaces')) {
    const body = req.body as any;
    const code = body.name.split(' ')[0].toUpperCase().slice(0, 6) || 'NEWWS';
    const newWs: AdminWorkspaceSummary = {
      id: 'ws-' + Date.now(),
      code,
      name: body.name,
      type: body.type || 'Center',
      ownerName: body.ownerName,
      ownerPhone: body.ownerPhone,
      ownerEmail: body.ownerEmail,
      packageTier: body.packageTier || 'center',
      studentCount: 0,
      activeGroupCount: 0,
      subscriptionStatus: 'Active',
      accountStatus: 'Active',
      subscriptionExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      daysRemaining: 30,
      contractedPrice: body.agreedPrice || 12000,
      paidAmount: 0
    };
    workspaces.unshift(newWs);
    return of(new HttpResponse({ status: 201, body: newWs })).pipe(delay(200));
  }

  // POST /api/v1/admin/workspaces/:id/suspend
  if (method === 'POST' && url.includes('/suspend')) {
    const id = url.split('/workspaces/')[1]?.split('/')[0];
    const ws = workspaces.find(w => w.id === id);
    if (ws) {
      ws.accountStatus = 'Suspended';
      ws.suspensionReason = (req.body as any)?.suspensionReason || 'موقوف إدارياً';
      ws.manualSuspendedAt = new Date().toISOString();
    }
    return of(new HttpResponse({ status: 200, body: ws })).pipe(delay(150));
  }

  // POST /api/v1/admin/workspaces/:id/reactivate
  if (method === 'POST' && url.includes('/reactivate')) {
    const id = url.split('/workspaces/')[1]?.split('/')[0];
    const ws = workspaces.find(w => w.id === id);
    if (ws) {
      ws.accountStatus = 'Active';
      ws.suspensionReason = null;
      ws.manualSuspendedAt = null;
    }
    return of(new HttpResponse({ status: 200, body: ws })).pipe(delay(150));
  }

  // POST /api/v1/admin/subscriptions/:id/renew
  if (method === 'POST' && url.includes('/subscriptions') && url.includes('/renew')) {
    const id = url.split('/subscriptions/')[1]?.split('/')[0];
    const ws = workspaces.find(w => w.id === id);
    const body = req.body as any;
    if (ws) {
      const days = body.durationDays || 30;
      let base = new Date();
      if (ws.subscriptionExpiresAt && new Date(ws.subscriptionExpiresAt) > base) {
        base = new Date(ws.subscriptionExpiresAt);
      }
      ws.subscriptionExpiresAt = new Date(base.getTime() + days * 24 * 60 * 60 * 1000).toISOString();
      ws.subscriptionStatus = 'Active';
      ws.daysRemaining += days;
    }
    return of(new HttpResponse({ status: 200, body: ws })).pipe(delay(150));
  }

  // GET /api/v1/admin/packages
  if (method === 'GET' && url.endsWith('/api/v1/admin/packages')) {
    return of(new HttpResponse({ status: 200, body: templates })).pipe(delay(100));
  }

  // PUT /api/v1/admin/workspaces/:id/overrides
  if (method === 'PUT' && url.includes('/overrides')) {
    const id = url.split('/workspaces/')[1]?.split('/')[0];
    const ws = workspaces.find(w => w.id === id);
    if (ws) {
      ws.featureOverrides = req.body as any;
    }
    return of(new HttpResponse({ status: 200, body: ws })).pipe(delay(150));
  }

  // POST /api/v1/admin/workspaces/:id/overrides/reset
  if (method === 'POST' && url.includes('/overrides/reset')) {
    const id = url.split('/workspaces/')[1]?.split('/')[0];
    const ws = workspaces.find(w => w.id === id);
    if (ws) {
      ws.featureOverrides = null;
    }
    return of(new HttpResponse({ status: 200, body: ws })).pipe(delay(150));
  }

  // GET /api/v1/admin/revenue/summary
  if (method === 'GET' && url.endsWith('/api/v1/admin/revenue/summary')) {
    return of(new HttpResponse({ status: 200, body: INITIAL_ADMIN_KPIS })).pipe(delay(100));
  }

  // GET /api/v1/admin/revenue/payments
  if (method === 'GET' && url.endsWith('/api/v1/admin/revenue/payments')) {
    return of(new HttpResponse({ status: 200, body: payments })).pipe(delay(150));
  }

  // POST /api/v1/admin/revenue/payments
  if (method === 'POST' && url.endsWith('/api/v1/admin/revenue/payments')) {
    const body = req.body as any;
    const ws = workspaces.find(w => w.id === body.workspaceId);
    const newPayment: PlatformSubscriptionPayment = {
      id: 'tx-' + Date.now(),
      transactionRef: 'TX-2026-00' + (payments.length + 1),
      workspaceId: body.workspaceId,
      workspaceName: ws?.name || 'سنتر تعليمي',
      packageTier: ws?.packageTier || 'center',
      amount: body.amount,
      method: body.method,
      referenceNumber: body.referenceNumber,
      status: 'Completed',
      createdAt: new Date().toISOString()
    };
    payments.unshift(newPayment);
    if (ws) {
      ws.paidAmount += body.amount;
    }
    return of(new HttpResponse({ status: 201, body: newPayment })).pipe(delay(150));
  }

  // POST /api/v1/admin/revenue/payments/:id/reverse
  if (method === 'POST' && url.includes('/revenue/payments/') && url.endsWith('/reverse')) {
    const id = url.split('/payments/')[1]?.split('/')[0];
    const pay = payments.find(p => p.id === id);
    if (pay) {
      pay.status = 'Reversed';
      pay.reversalReason = (req.body as any)?.reversalReason || 'عكس قيد مالي محاسبي';
      pay.reversedAt = new Date().toISOString();
      const ws = workspaces.find(w => w.id === pay.workspaceId);
      if (ws) {
        ws.paidAmount = Math.max(0, (ws.paidAmount ?? 0) - pay.amount);
      }
    }
    return of(new HttpResponse({ status: 200, body: pay })).pipe(delay(150));
  }

  // GET /api/v1/admin/whatsapp/config
  if (method === 'GET' && url.endsWith('/api/v1/admin/whatsapp/config')) {
    return of(new HttpResponse({ status: 200, body: whatsappConfig })).pipe(delay(100));
  }

  // PUT /api/v1/admin/whatsapp/config
  if (method === 'PUT' && url.endsWith('/api/v1/admin/whatsapp/config')) {
    whatsappConfig = { ...whatsappConfig, ...(req.body as any) };
    return of(new HttpResponse({ status: 200, body: whatsappConfig })).pipe(delay(150));
  }

  // POST /api/v1/admin/whatsapp/test-ping
  if (method === 'POST' && url.endsWith('/api/v1/admin/whatsapp/test-ping')) {
    const res: WhatsAppTestPingResult = {
      status: 'Connected',
      verifiedName: 'Tahdeera Platform Central',
      qualityRating: 'Green',
      messagingTier: 'Tier 250 msgs/day',
      pingDurationMs: 295
    };
    return of(new HttpResponse({ status: 200, body: res })).pipe(delay(300));
  }

  // GET /api/v1/admin/whatsapp/stats
  if (method === 'GET' && url.endsWith('/api/v1/admin/whatsapp/stats')) {
    return of(new HttpResponse({ status: 200, body: INITIAL_WHATSAPP_STATS })).pipe(delay(100));
  }

  // GET /api/v1/admin/settings
  if (method === 'GET' && url.endsWith('/api/v1/admin/settings')) {
    return of(new HttpResponse({ status: 200, body: globalSettings })).pipe(delay(100));
  }

  // PUT /api/v1/admin/settings
  if (method === 'PUT' && url.endsWith('/api/v1/admin/settings')) {
    globalSettings = { ...globalSettings, ...(req.body as any) };
    return of(new HttpResponse({ status: 200, body: globalSettings })).pipe(delay(150));
  }

  // GET /api/v1/admin/audit-logs
  if (method === 'GET' && url.endsWith('/api/v1/admin/audit-logs')) {
    return of(new HttpResponse({ status: 200, body: auditLogs })).pipe(delay(150));
  }

  return of(new HttpResponse({ status: 200, body: {} }));
};

export function handleAdminMockRequest(req: HttpRequest<unknown>): Observable<HttpEvent<unknown>> | null {
  if (!req.url.includes('/api/v1/admin')) {
    return null;
  }
  return adminMockInterceptor(req);
}
