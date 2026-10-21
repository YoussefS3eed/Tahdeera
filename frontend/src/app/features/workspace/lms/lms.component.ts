import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LmsStore, LmsTab } from '../../../core/stores/lms.store';
import { ExamsTabComponent } from './components/exams-tab/exams-tab.component';
import { QuestionBankTabComponent } from './components/question-bank-tab/question-bank-tab.component';
import { HomeworkTabComponent } from './components/homework-tab/homework-tab.component';
import { LibraryTabComponent } from './components/library-tab/library-tab.component';

@Component({
  selector: 'app-lms',
  standalone: true,
  imports: [
    CommonModule,
    ExamsTabComponent,
    QuestionBankTabComponent,
    HomeworkTabComponent,
    LibraryTabComponent
  ],
  templateUrl: './lms.component.html',
  styleUrls: ['./lms.component.scss']
})
export class LmsComponent implements OnInit {
  readonly store = inject(LmsStore);

  ngOnInit(): void {
    this.store.loadAll();
  }

  setTab(tab: LmsTab): void {
    this.store.activeTab.set(tab);
  }
}
