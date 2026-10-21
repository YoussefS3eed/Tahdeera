import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiQueryResponse, PromptChip } from '../../../../../core/models/ai-assistant.models';

@Component({
  selector: 'app-ai-response-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ai-response-card.component.html',
  styleUrls: ['./ai-response-card.component.scss']
})
export class AiResponseCardComponent {
  @Input() response: AiQueryResponse | null = null;
  @Output() chipClicked = new EventEmitter<PromptChip>();
  @Output() dismiss = new EventEmitter<void>();

  showSql = signal<boolean>(false);

  toggleSql(): void {
    this.showSql.update(v => !v);
  }

  onSelectChip(chip: PromptChip): void {
    this.chipClicked.emit(chip);
  }
}
