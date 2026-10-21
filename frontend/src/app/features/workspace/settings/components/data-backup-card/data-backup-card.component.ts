import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SettingsStore } from '../../../../../core/stores/settings.store';

@Component({
  selector: 'app-data-backup-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './data-backup-card.component.html',
  styleUrls: ['./data-backup-card.component.scss']
})
export class DataBackupCardComponent {
  readonly store = inject(SettingsStore);

  async downloadBackup(): Promise<void> {
    const blob = await this.store.triggerBackupDownload();
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `tahdeera_backup_ws-main_${new Date().toISOString().slice(0, 10)}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }
}
