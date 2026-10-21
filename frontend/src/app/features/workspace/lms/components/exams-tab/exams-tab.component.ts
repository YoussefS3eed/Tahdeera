import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LmsStore } from '../../../../../core/stores/lms.store';
import { CreateExamModalComponent } from '../create-exam-modal/create-exam-modal.component';

@Component({
  selector: 'app-exams-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, CreateExamModalComponent],
  templateUrl: './exams-tab.component.html',
  styleUrls: ['./exams-tab.component.scss']
})
export class ExamsTabComponent {
  readonly store = inject(LmsStore);
  private router = inject(Router);

  readonly filterStatus = signal<string>('all');
  readonly searchQuery = signal<string>('');
  readonly showCreateModal = signal<boolean>(false);

  readonly filteredExams = computed(() => {
    const st = this.filterStatus();
    const q = this.searchQuery().trim().toLowerCase();
    return this.store.exams().filter(e => {
      if (st !== 'all' && e.status !== st) return false;
      if (q && !e.title.toLowerCase().includes(q) && !(e.subject || '').toLowerCase().includes(q)) return false;
      return true;
    });
  });

  openStudentPortal(examId: string): void {
    this.router.navigate(['/portal/exam', examId]);
  }
}
