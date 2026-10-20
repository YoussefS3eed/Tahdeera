import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-cross-group-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cross-group-drawer.component.html',
  styleUrls: ['./cross-group-drawer.component.scss']
})
export class CrossGroupDrawerComponent {
  readonly studentName = input<string>('');
  readonly studentCode = input<string>('');
  readonly studentId = input<string>('');
  readonly originalGroupName = input<string>('');
  readonly targetSessionTitle = input<string>('');

  readonly confirmed = output<string>();
  readonly cancelled = output<void>();

  overrideReason = 'حضور كبديل استثنائي للحصة المحددة بناءً على طلب الطالب وولي الأمر';

  confirm(): void {
    this.confirmed.emit(this.overrideReason);
  }

  close(): void {
    this.cancelled.emit();
  }
}
