import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AiAssistantStore } from '../../../core/stores/ai-assistant.store';
import { PromptChip } from '../../../core/models/ai-assistant.models';
import { AiQueryFormComponent } from './components/ai-query-form/ai-query-form.component';
import { AiResponseCardComponent } from './components/ai-response-card/ai-response-card.component';

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [
    CommonModule,
    AiQueryFormComponent,
    AiResponseCardComponent
  ],
  templateUrl: './ai-assistant.component.html',
  styleUrls: ['./ai-assistant.component.scss']
})
export class AiAssistantComponent implements OnInit {
  readonly store = inject(AiAssistantStore);

  ngOnInit(): void {
    this.store.loadPromptChips();
    this.store.loadQuota();
  }

  onQuerySubmitted(question: string): void {
    this.store.askAi(question);
  }

  onChipSelected(chip: PromptChip): void {
    this.store.askAi(chip.prompt || chip.promptText || '');
  }

  dismissCurrent(): void {
    this.store.clearResponse();
  }
}
