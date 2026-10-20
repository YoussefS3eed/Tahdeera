/**
 * Skeleton Loader Component Configuration.
 * Fixed dimensions guarantee Cumulative Layout Shift (CLS) strictly < 0.05.
 */
export type SkeletonVariant = 'kpi-card' | 'table-row' | 'text-line' | 'avatar';

export interface SkeletonLoaderConfig {
  variant: SkeletonVariant;   // Default: 'kpi-card'
  count: number;              // Number of skeleton elements (default: 1)
  height?: string;            // Optional custom CSS height; KPI card defaults to exactly 124px
}

/**
 * Dual-Mode Empty State Configuration.
 */
export type EmptyStateMode = 'zero-data' | 'zero-match';

export interface EmptyStateConfig {
  mode: EmptyStateMode;       // 'zero-data' (onboarding) | 'zero-match' (filter reset)
  title: string;              // Main translated title
  description: string;        // Secondary explanatory text
  iconName?: string;          // Material icon name (default: 'inbox' for zero-data, 'search_off' for zero-match)
  actionLabel?: string;       // Action button text
}

/**
 * Destructive Confirmation Modal Configuration.
 * Enforces Principle 7 (mandatory reason input for destructive operations).
 */
export interface ConfirmationModalConfig {
  title: string;              // Modal heading
  message: string;            // Explanation of action and consequences
  requireReason: boolean;     // If true, requires user to enter reason text before enabling confirm button
  reasonMinLength: number;    // Minimum non-whitespace characters required (default: 5)
  confirmButtonText: string;  // Confirm CTA label (default: 'تأكيد')
  cancelButtonText: string;   // Cancel CTA label (default: 'إلغاء')
  isDestructive: boolean;     // If true, renders danger styling (red theme)
}

/**
 * In-Place Re-Authentication Modal Configuration.
 * Enforces Principle 7 & Principle 9 (preserves active forms without page reload).
 */
export interface ReauthModalConfig {
  isOpen: boolean;            // Controls visibility
  userEmail: string;          // Displays prefilled, disabled email
}

/**
 * Global Search Modal Stub Configuration (Phases 1–3).
 * Enforces DEC-F2-107: accessible search stub with F3 deferral notice badge.
 */
export interface GlobalSearchModalConfig {
  isOpen: boolean;
  noticeAr: string;           // "البحث الشامل — سيتم ربطه بسجل الطلاب والمجموعات في المرحلة F3"
  noticeEn: string;           // "Global Search — Integration with the Students Directory and Groups will be available in Milestone F3."
}

/**
 * Reusable Workspace Placeholder Configuration (Phases 1–3).
 * Enforces DEC-F2-106: prevents broken navigation on unimplemented child routes.
 */
export interface WorkspacePlaceholderConfig {
  titleAr: string;            // "مساحة العمل — سيتم تفعيل لوحة التحكم في المرحلة 4 وباقي الأقسام في المراحل اللاحقة"
  returnRoute: string;        // "/"
  returnButtonTextAr: string; // "العودة لبوابة الزوار"
}

/**
 * Temporary Login Stub Configuration (Phases 1–3).
 * Enforces Clarification Q1: stable route target for /login.
 */
export interface LoginStubConfig {
  titleAr: string;            // "صفحة تسجيل الدخول — سيتم تفعيلها بالكامل في المرحلة 4"
  returnRoute: string;        // "/"
  returnButtonTextAr: string; // "العودة للرئيسية"
}
