import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AcademicService } from '../../../core/services/academic.service';
import { StudyGroupItem } from '../../../core/models/academic.models';
import { CreateGroupModalComponent } from './components/create-group-modal/create-group-modal.component';
import { AssistantDelegationDrawerComponent } from './components/assistant-delegation-drawer/assistant-delegation-drawer.component';

@Component({
  selector: 'app-groups',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CreateGroupModalComponent,
    AssistantDelegationDrawerComponent
  ],
  templateUrl: './groups.component.html',
  styleUrls: ['./groups.component.scss']
})
export class GroupsComponent implements OnInit {
  private readonly academicService = inject(AcademicService);

  groups = signal<StudyGroupItem[]>([]);
  isLoading = signal(false);

  // Filters
  selectedGrade = signal('all');

  // Modals
  showCreateModal = signal(false);
  activeDelegationGroup = signal<StudyGroupItem | null>(null);

  ngOnInit(): void {
    this.loadGroups();
  }

  loadGroups(): void {
    this.isLoading.set(true);
    this.academicService.getStudyGroups().subscribe({
      next: groups => {
        this.groups.set(groups);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  filteredGroups = computed(() => {
    const grade = this.selectedGrade();
    if (grade === 'all') return this.groups();
    return this.groups().filter(g => g.grade === grade);
  });

  onGroupCreated(payload: Partial<StudyGroupItem>): void {
    this.academicService.createStudyGroup(payload).subscribe(newGroup => {
      this.groups.update(prev => [newGroup, ...prev]);
      this.showCreateModal.set(false);
    });
  }

  openDelegation(group: StudyGroupItem): void {
    this.activeDelegationGroup.set(group);
  }

  onAssistantDelegated(event: { groupId: string; assistantId: string | null }): void {
    this.academicService.delegateGroupAssistant(event.groupId, event.assistantId).subscribe(updated => {
      this.groups.update(prev => prev.map(g => g.id === updated.id ? updated : g));
      this.activeDelegationGroup.set(null);
    });
  }
}
