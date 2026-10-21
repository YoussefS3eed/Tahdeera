import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeamStore } from '../../../core/stores/team.store';
import { WorkspaceStaffMember } from '../../../core/models/team-settings.models';
import { AddStaffModalComponent } from './components/add-staff-modal/add-staff-modal.component';
import { StaffCredentialCardComponent } from './components/staff-credential-card/staff-credential-card.component';
import { DeactivateStaffModalComponent } from './components/deactivate-staff-modal/deactivate-staff-modal.component';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AddStaffModalComponent,
    StaffCredentialCardComponent,
    DeactivateStaffModalComponent
  ],
  templateUrl: './team.component.html',
  styleUrls: ['./team.component.scss']
})
export class TeamComponent implements OnInit {
  readonly teamStore = inject(TeamStore);

  showAddModal = signal<boolean>(false);
  showCredentialCard = signal<boolean>(false);
  showDeactivateModal = signal<boolean>(false);

  selectedMemberForCard = signal<WorkspaceStaffMember | null>(null);
  selectedMemberForDeactivation = signal<WorkspaceStaffMember | null>(null);

  ngOnInit(): void {
    this.teamStore.loadMembers();
  }

  onStaffCreated(member: WorkspaceStaffMember): void {
    this.selectedMemberForCard.set(member);
    this.showCredentialCard.set(true);
  }

  openCredentialCard(member: WorkspaceStaffMember): void {
    this.selectedMemberForCard.set(member);
    this.showCredentialCard.set(true);
  }

  openDeactivateModal(member: WorkspaceStaffMember): void {
    this.selectedMemberForDeactivation.set(member);
    this.showDeactivateModal.set(true);
  }
}
