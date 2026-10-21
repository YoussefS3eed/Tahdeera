import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LmsStore } from '../../../../../core/stores/lms.store';

@Component({
  selector: 'app-homework-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './homework-tab.component.html',
  styleUrls: ['./homework-tab.component.scss']
})
export class HomeworkTabComponent {
  readonly store = inject(LmsStore);
}
