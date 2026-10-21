import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportsStore } from '../../../core/stores/reports.store';
import { ReportFiltersComponent } from './components/report-filters/report-filters.component';
import { ReportTableComponent } from './components/report-table/report-table.component';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    CommonModule,
    ReportFiltersComponent,
    ReportTableComponent
  ],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss']
})
export class ReportsComponent implements OnInit {
  readonly store = inject(ReportsStore);

  async ngOnInit(): Promise<void> {
    await this.store.loadCatalog();
    if (this.store.activeReportId()) {
      this.store.runReport({ reportId: this.store.activeReportId() });
    }
  }
}
