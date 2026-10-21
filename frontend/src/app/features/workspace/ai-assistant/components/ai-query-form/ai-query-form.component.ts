import { Component, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiAssistantStore } from '../../../../../core/stores/ai-assistant.store';
import { PromptChip } from '../../../../../core/models/ai-assistant.models';

@Component({
  selector: 'app-ai-query-form',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-query-form.component.html',
  styleUrls: ['./ai-query-form.component.scss']
})
export class AiQueryFormComponent {
  readonly store = inject(AiAssistantStore);

  @Output() querySubmitted = new EventEmitter<string>();

  questionText = signal<string>('');

  submitQuery(): void {
    const q = this.questionText().trim();
    if (!q || this.store.isLoading()) return;
    this.querySubmitted.emit(q);
    this.questionText.set('');
  }

  useChip(chip: PromptChip): void {
    const text = chip.prompt || chip.promptText || '';
    if (!text) return;
    this.questionText.set(text);
    this.submitQuery();
  }
}
