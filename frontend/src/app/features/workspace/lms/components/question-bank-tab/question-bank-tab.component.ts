import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LmsStore } from '../../../../../core/stores/lms.store';
import { QuestionItem } from '../../../../../core/models/lms.models';

@Component({
  selector: 'app-question-bank-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './question-bank-tab.component.html',
  styleUrls: ['./question-bank-tab.component.scss']
})
export class QuestionBankTabComponent {
  readonly store = inject(LmsStore);

  readonly filterType = signal<string>('all');
  readonly filterDifficulty = signal<string>('all');
  readonly searchQuery = signal<string>('');

  readonly filteredQuestions = computed(() => {
    const t = this.filterType();
    const d = this.filterDifficulty();
    const q = this.searchQuery().trim().toLowerCase();

    return this.store.questionBank().filter(item => {
      if (t !== 'all' && item.type !== t) return false;
      if (d !== 'all' && item.difficulty !== d) return false;
      if (q && !(item.textAr || item.questionText || '').toLowerCase().includes(q)) return false;
      return true;
    });
  });

  async approveDraft(id: string): Promise<void> {
    await this.store.approveAiDraftQuestion(id);
  }
}
