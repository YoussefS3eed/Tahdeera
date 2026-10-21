import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LmsStore } from '../../../../../core/stores/lms.store';

@Component({
  selector: 'app-library-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './library-tab.component.html',
  styleUrls: ['./library-tab.component.scss']
})
export class LibraryTabComponent {
  readonly store = inject(LmsStore);
}
