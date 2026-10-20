import { Component, input, output, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StudentListItem } from '../../../../../core/models/academic.models';
import { NfcWedgeAdapterService } from '../../../../../core/services/nfc-wedge-adapter.service';

@Component({
  selector: 'app-link-card-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './link-card-dialog.component.html',
  styleUrls: ['./link-card-dialog.component.scss']
})
export class LinkCardDialogComponent implements OnInit {
  private readonly nfcWedge = inject(NfcWedgeAdapterService);

  readonly students = input<StudentListItem[]>([]);

  readonly linked = output<{ cardUid: string; studentId: string }>();
  readonly closed = output<void>();

  cardUid = '';
  selectedStudentId = '';
  validationError = signal<string | null>(null);

  ngOnInit(): void {
    this.nfcWedge.uidScanned$.subscribe(uid => {
      this.cardUid = uid;
    });
  }

  submit(): void {
    const uid = this.cardUid.trim();
    if (!uid || uid.length < 4) {
      this.validationError.set('يرجى تمرير كارت NFC على القارئ أو إدخال معرف الكارت');
      return;
    }

    if (!this.selectedStudentId) {
      this.validationError.set('يرجى اختيار الطالب المراد ربط الكارت به');
      return;
    }

    this.validationError.set(null);
    this.linked.emit({
      cardUid: uid,
      studentId: this.selectedStudentId
    });
  }

  close(): void {
    this.closed.emit();
  }
}
