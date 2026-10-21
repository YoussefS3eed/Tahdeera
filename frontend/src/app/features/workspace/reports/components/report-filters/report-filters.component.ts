import { Component, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportsStore } from '../../../../../core/stores/reports.store';
import { ReportFilterCriteria } from '../../../../../core/models/reports.models';

@Component({
  selector: 'app-report-filters',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './report-filters.component.html',
  styleUrls: ['./report-filters.component.scss']
})
export class ReportFiltersComponent {
  readonly store = inject(ReportsStore);

  @Output() filterChanged = new EventEmitter<ReportFilterCriteria>();

  dateFrom = signal<string>('2026-10-01');
  dateTo = signal<string>('2026-10-21');
  selectedGrade = signal<string>('all');

  onSelectReport(id: string): void {
    this.store.selectReport(id);
  }

  applyFilters(): void {
    const criteria: ReportFilterCriteria = {
      reportId: this.store.activeReportId(),
      dateFrom: this.dateFrom(),
      dateTo: this.dateTo(),
      gradeName: this.selectedGrade() !== 'all' ? this.selectedGrade() : undefined
    };
    this.store.runReport(criteria);
  }
}
