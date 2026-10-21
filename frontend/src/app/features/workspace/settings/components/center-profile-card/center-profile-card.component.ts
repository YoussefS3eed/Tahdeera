import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SettingsStore } from '../../../../../core/stores/settings.store';

@Component({
  selector: 'app-center-profile-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './center-profile-card.component.html',
  styleUrls: ['./center-profile-card.component.scss']
})
export class CenterProfileCardComponent {
  readonly store = inject(SettingsStore);

  centerNameAr = signal<string>('');
  centerNameEn = signal<string>('');
  centerPhone = signal<string>('');
  address = signal<string>('');
  governorate = signal<string>('');
  taxId = signal<string>('');

  saveSuccess = signal<boolean>(false);

  ngOnInit(): void {
    const s = this.store.settings();
    const p = s?.profile;
    if (p) {
      this.centerNameAr.set(p.centerNameAr);
      this.centerNameEn.set(p.centerNameEn || '');
      this.centerPhone.set(p.centerPhone);
      this.address.set(p.address);
      this.governorate.set(p.governorate);
      this.taxId.set(p.taxId || '');
    } else if (s) {
      this.centerNameAr.set(s.centerName || '');
      this.centerPhone.set(s.phone || '');
      this.address.set(s.address || '');
      this.governorate.set('القاهرة');
    }
  }

  async saveProfile(): Promise<void> {
    await this.store.updateProfile({
      centerNameAr: this.centerNameAr(),
      centerNameEn: this.centerNameEn(),
      centerPhone: this.centerPhone(),
      address: this.address(),
      governorate: this.governorate(),
      taxId: this.taxId()
    });
    this.saveSuccess.set(true);
    setTimeout(() => this.saveSuccess.set(false), 3000);
  }
}
