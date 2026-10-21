import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FinancialTransaction } from '../../../../../core/models/finance.models';

@Component({
  selector: 'app-printable-receipt-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './printable-receipt-modal.component.html',
  styleUrls: ['./printable-receipt-modal.component.scss']
})
export class PrintableReceiptModalComponent {
  @Input() transaction: FinancialTransaction | null = null;
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  printReceipt(): void {
    window.print();
  }

  closeModal(): void {
    this.close.emit();
  }
}
