import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AcademicService } from '../../../core/services/academic.service';
import { NfcCardItem, StudentListItem } from '../../../core/models/academic.models';
import { LinkCardDialogComponent } from './components/link-card-dialog/link-card-dialog.component';
import { RevokeCardModalComponent } from './components/revoke-card-modal/revoke-card-modal.component';

@Component({
  selector: 'app-cards',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LinkCardDialogComponent,
    RevokeCardModalComponent
  ],
  templateUrl: './cards.component.html',
  styleUrls: ['./cards.component.scss']
})
export class CardsComponent implements OnInit {
  private readonly academicService = inject(AcademicService);

  cards = signal<NfcCardItem[]>([]);
  students = signal<StudentListItem[]>([]);
  isLoading = signal(false);

  // Filters
  searchQuery = signal('');
  selectedStatus = signal('all');

  // Modals
  showLinkDialog = signal(false);
  activeRevokeCard = signal<NfcCardItem | null>(null);

  ngOnInit(): void {
    this.loadCards();
    this.loadStudents();
  }

  loadCards(): void {
    this.isLoading.set(true);
    this.academicService.getNfcCards().subscribe({
      next: cards => {
        this.cards.set(cards);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  loadStudents(): void {
    this.academicService.getStudents().subscribe(students => {
      this.students.set(students);
    });
  }

  filteredCards = computed(() => {
    let list = this.cards();
    const query = this.searchQuery().trim().toLowerCase();
    const status = this.selectedStatus();

    if (query) {
      list = list.filter(c =>
        c.cardUid.toLowerCase().includes(query) ||
        (c.assignedStudentName && c.assignedStudentName.toLowerCase().includes(query)) ||
        (c.label && c.label.toLowerCase().includes(query))
      );
    }

    if (status !== 'all') {
      list = list.filter(c => c.status === status);
    }

    return list;
  });

  onCardLinked(event: { cardUid: string; studentId: string }): void {
    this.academicService.linkCard(event.cardUid, event.studentId).subscribe(updated => {
      this.cards.update(prev => {
        const idx = prev.findIndex(c => c.cardUid === updated.cardUid);
        if (idx >= 0) {
          const copy = [...prev];
          copy[idx] = updated;
          return copy;
        }
        return [updated, ...prev];
      });
      this.showLinkDialog.set(false);
    });
  }

  openRevoke(card: NfcCardItem): void {
    this.activeRevokeCard.set(card);
  }

  onCardRevoked(event: { cardUid: string; reason: string }): void {
    this.academicService.revokeCard(event.cardUid, event.reason).subscribe(revoked => {
      this.cards.update(prev => prev.map(c => c.cardUid === revoked.cardUid ? revoked : c));
      this.activeRevokeCard.set(null);
    });
  }
}
