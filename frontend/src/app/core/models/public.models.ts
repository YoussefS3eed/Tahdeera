/**
 * Authoritative 3-tier subscription package tiers matching Tahdeera pricing structure.
 */
export type PackageTier = 'Basic' | 'Pro' | 'Center';

/**
 * Student volume brackets for public subscription inquiry estimation.
 */
export type StudentCountBracket = '1-100' | '100-300' | '300-1000' | '+1000';

/**
 * Egyptian Governorate representation for the subscription inquiry dropdown.
 */
export interface GovernorateOption {
  id: string;
  nameAr: string;
  nameEn: string;
}

/**
 * Package Plan display entity for Landing Page comparison cards.
 */
export interface PackagePlan {
  id: PackageTier;
  titleKey: string;           // Translation key in LANDING bundle
  priceMonthly: number;       // In Egyptian Pounds (EGP / ج.م)
  currencyKey: string;        // E.g. 'LANDING.PRICE_PER_MONTH'
  maxStudents: number | null; // null represents unlimited students
  isFeatured: boolean;        // Pro package displays "الأكثر طلباً" featured styling
  features: string[];         // Checklist of features displayed per package
}

/**
 * Subscription inquiry request payload submitted from LandingComponent modal.
 */
export interface SubscriptionInquiryRequest {
  name: string;               // Required, min 3 characters
  phone: string;              // Required, strictly validated by regex ^01[0125][0-9]{8}$
  governorate: string;        // Required, valid Egyptian governorate code
  estimatedStudents: StudentCountBracket; // Required bracket selection
  selectedPackage: PackageTier; // Required package selection
}

/**
 * Success response returned by POST /api/v1/public/inquiries.
 */
export interface SubscriptionInquiryResponse {
  success: boolean;
  inquiryId: string;          // E.g. "inq-981"
  message: string;            // Localized confirmation message
}

/**
 * Navigation item representation for Desktop Sidebar and Mobile Bottom Bar.
 */
export interface NavigationItem {
  id: string;                 // Unique identifier (e.g. 'dashboard', 'nfc-station')
  labelKey: string;           // Translation key in NAV bundle
  icon: string;               // Material Symbols Outlined icon name
  route: string;              // Angular route path (e.g. '/workspace/dashboard')
  allowedRoles?: ('Teacher' | 'Assistant' | 'Receptionist' | 'SuperAdmin')[];
  badge?: number | string;    // Optional numerical badge count
  isStickyMobile?: boolean;   // True for the 4 core bottom mobile tabs
}

/**
 * Responsive shell UI interaction state managed by Shell/Header state.
 */
export interface ShellUiState {
  isMobileNavOpen: boolean;   // Controls bottom-sheet drawer for "المزيد" on mobile (<= 768px)
  isSearchModalOpen: boolean; // Controls GlobalSearchModalComponent visibility
  isReauthModalOpen: boolean; // Controls ReauthModalComponent visibility
}
