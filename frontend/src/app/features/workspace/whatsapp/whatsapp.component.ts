import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WhatsAppStore } from '../../../core/stores/whatsapp.store';
import { TemplateEditorTabComponent } from './components/template-editor-tab/template-editor-tab.component';
import { OutboxLogsTabComponent } from './components/outbox-logs-tab/outbox-logs-tab.component';
import { GuardianConsentTabComponent } from './components/guardian-consent-tab/guardian-consent-tab.component';

@Component({
  selector: 'app-whatsapp',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TemplateEditorTabComponent,
    OutboxLogsTabComponent,
    GuardianConsentTabComponent
  ],
  templateUrl: './whatsapp.component.html',
  styleUrls: ['./whatsapp.component.scss']
})
export class WhatsAppComponent implements OnInit {
  readonly store = inject(WhatsAppStore);

  ngOnInit(): void {
    this.store.loadTemplates();
    this.store.loadOutboxLogs();
    this.store.loadConsents();
  }

  setTab(tab: 'template' | 'logs' | 'parents'): void {
    this.store.activeSubTab.set(tab);
  }
}
