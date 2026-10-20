import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudyGroupItem } from '../../../../../core/models/academic.models';

export interface AssistantStaffItem {
  id: string;
  name: string;
  phone: string;
}

@Component({
  selector: 'app-assistant-delegation-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assistant-delegation-drawer.component.html',
  styleUrls: ['./assistant-delegation-drawer.component.scss']
})
export class AssistantDelegationDrawerComponent {
  readonly group = input.required<StudyGroupItem>();
  readonly assistants = input<AssistantStaffItem[]>([
    { id: 'staff-asst-1', name: 'أحمد نبيل (مساعد أول)', phone: '01012345678' },
    { id: 'staff-asst-2', name: 'سلمى مجدي (مساعد تدريس)', phone: '01198765432' },
    { id: 'staff-asst-3', name: 'كريم عثمان (مشرف حضور)', phone: '01234567891' }
  ]);

  readonly delegated = output<{ groupId: string; assistantId: string | null }>();
  readonly closed = output<void>();

  selectedAssistantId: string | null = null;

  ngOnInit(): void {
    this.selectedAssistantId = this.group()?.delegatedAssistantStaffId || null;
  }

  saveDelegation(): void {
    // Implements CONF-09: One active attendance delegate per group
    this.delegated.emit({
      groupId: this.group().id,
      assistantId: this.selectedAssistantId
    });
  }

  revokeDelegation(): void {
    this.selectedAssistantId = null;
    this.delegated.emit({
      groupId: this.group().id,
      assistantId: null
    });
  }

  close(): void {
    this.closed.emit();
  }
}
