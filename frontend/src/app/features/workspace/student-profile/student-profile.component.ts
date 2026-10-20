import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AcademicService } from '../../../core/services/academic.service';
import {
  Student360Profile,
  CardRevocationReason,
  AttendanceBadgeStatus
} from '../../../core/models/academic.models';
import { ProfileHeaderComponent } from './components/profile-header/profile-header.component';
import { CardManagementModalComponent } from './components/card-management-modal/card-management-modal.component';

export type ProfileTabType = 'attendance' | 'finances' | 'exams' | 'homework' | 'whatsapp' | 'report';

@Component({
  selector: 'app-student-profile',
  standalone: true,
  imports: [
    CommonModule,
    ProfileHeaderComponent,
    CardManagementModalComponent
  ],
  templateUrl: './student-profile.component.html',
  styleUrls: ['./student-profile.component.scss']
})
export class StudentProfileComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly academicService = inject(AcademicService);

  studentId = signal<string>('');
  profile = signal<Student360Profile | null>(null);
  isLoading = signal(true);
  activeTab = signal<ProfileTabType>('attendance');

  // Role simulation per CONF-09: Financial tab strictly hidden if Assistant or Receptionist
  currentUserRole = signal<'Owner' | 'Teacher' | 'Assistant' | 'Receptionist'>('Teacher');

  // Card modal state
  showCardModal = signal(false);

  ngOnInit(): void {
    const role = localStorage.getItem('tahdeera_user_role') as any;
    if (role) {
      this.currentUserRole.set(role);
    }

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.studentId.set(id);
        this.loadProfile(id);
      }
    });
  }

  loadProfile(id: string): void {
    this.isLoading.set(true);
    this.academicService.getStudent360(id).subscribe({
      next: p => {
        this.profile.set(p);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  // CONF-09: Check if finances tab can be viewed
  canViewFinances = computed(() => {
    const role = this.currentUserRole();
    return role !== 'Assistant' && role !== 'Receptionist';
  });

  setTab(tab: ProfileTabType): void {
    if (tab === 'finances' && !this.canViewFinances()) {
      return;
    }
    this.activeTab.set(tab);
  }

  onCardRevoked(event: { reason: CardRevocationReason }): void {
    const p = this.profile();
    if (!p?.activeNfcCardUid) return;

    this.academicService.revokeCard(p.activeNfcCardUid, event.reason).subscribe(() => {
      this.profile.update(curr => curr ? { ...curr, activeNfcCardUid: null } : null);
      this.showCardModal.set(false);
    });
  }

  onCardReplaced(event: { newUid: string }): void {
    const p = this.profile();
    if (!p) return;

    this.academicService.linkCard(event.newUid, p.id).subscribe(() => {
      this.profile.update(curr => curr ? { ...curr, activeNfcCardUid: event.newUid } : null);
      this.showCardModal.set(false);
    });
  }

  goBack(): void {
    this.router.navigate(['/workspace/students']);
  }
}
