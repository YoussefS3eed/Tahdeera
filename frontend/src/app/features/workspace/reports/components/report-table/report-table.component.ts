import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportResult, ReportCatalogItem } from '../../../../../core/models/reports.models';
import { ReportExporterService } from '../../../../../core/services/report-exporter.service';

@Component({
  selector: 'app-report-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-table.component.html',
  styleUrls: ['./report-table.component.scss']
})
export class ReportTableComponent {
  private exporter = inject(ReportExporterService);

  @Input() result: ReportResult | null = null;
  @Input() reportMeta: ReportCatalogItem | null = null;

  get tableHeaders(): string[] {
    if (this.result?.headers && this.result.headers.length > 0) return this.result.headers;
    if (this.result?.columns && this.result.columns.length > 0) return this.result.columns.map(c => c.headerAr);
    return [];
  }

  get tableRows(): (string | number)[][] {
    if (!this.result?.rows || this.result.rows.length === 0) return [];
    if (Array.isArray(this.result.rows[0])) return this.result.rows;
    const cols = this.result.columns || [];
    return this.result.rows.map(row => cols.map(c => row[c.key] ?? ''));
  }

  exportCsv(): void {
    if (!this.result) return;
    const headers = this.tableHeaders;
    const rows = this.tableRows;
    const filename = `${this.reportMeta?.titleAr || this.reportMeta?.nameAr || 'report'}_${new Date().toISOString().slice(0, 10)}`;
    this.exporter.exportToCsv(filename, headers, rows);
  }

  printReport(): void {
    if (!this.result) return;
    const headers = this.tableHeaders;
    const rows = this.tableRows;
    const title = this.reportMeta?.titleAr || this.reportMeta?.nameAr || 'تقرير تحضيرة';
    const totalCount = this.result.totalRows ?? rows.length;
    const subtitle = `تم الاستخراج بتاريخ: ${new Date().toLocaleDateString('ar-EG')} - إجمالي السجلات: ${totalCount}`;
    this.exporter.printReport(title, headers, rows, subtitle);
  }
}
