export type StandardReportId =
  | 'daily-attendance'
  | 'daily-absence'
  | 'student-period'
  | 'group-attendance'
  | 'unpaid-students'
  | 'partial-students'
  | 'payments-ledger'
  | 'whatsapp-status'
  | 'new-students'
  | 'monthly-term'
  | string;

export interface ReportColumn {
  key: string;
  headerAr: string;
}

export interface ReportCatalogItem {
  id: StandardReportId;
  nameAr?: string;
  titleAr: string;
  titleEn: string;
  descriptionAr?: string;
  descAr: string;
  descEn: string;
  icon: string;
  category: 'Attendance' | 'Finance' | 'WhatsApp' | 'Academic' | string;
}

export interface ReportFilterCriteria {
  reportId: StandardReportId;
  dateFrom?: string;
  dateTo?: string;
  startDate?: string;
  endDate?: string;
  groupId?: string;
  gradeLevel?: string;
  gradeName?: string;
  searchQuery?: string;
}

export interface ReportResult {
  reportId: StandardReportId;
  title: string;
  generatedAt?: string;
  generatedAtUtc?: string;
  headers?: string[];
  columns: ReportColumn[];
  rows: any[];
  totalRows: number;
  totalsSummary?: Record<string, string | number>;
}
