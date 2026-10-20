import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AcademicService } from '../../../core/services/academic.service';
import { StudentListItem, StudyGroupItem } from '../../../core/models/academic.models';
import { AddStudentModalComponent } from './components/add-student-modal/add-student-modal.component';
import { LinkGlobalStudentModalComponent } from './components/link-global-student-modal/link-global-student-modal.component';

@Component({
  selector: 'app-students',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AddStudentModalComponent,
    LinkGlobalStudentModalComponent
  ],
  templateUrl: './students.component.html',
  styleUrls: ['./students.component.scss']
})
export class StudentsComponent implements OnInit {
  private readonly academicService = inject(AcademicService);
  private readonly router = inject(Router);

  // Filters & State
  searchQuery = signal('');
  selectedGrade = signal('all');
  selectedGroupId = signal('all');
  selectedStatus = signal('all');
  viewMode = signal<'grid' | 'table'>('grid');
  isLoading = signal(false);

  // Data
  students = signal<StudentListItem[]>([]);
  groups = signal<StudyGroupItem[]>([]);

  // Modals
  showAddModal = signal(false);
  showLinkModal = signal(false);

  ngOnInit(): void {
    const savedMode = localStorage.getItem('tahdeera_students_view_mode');
    if (savedMode === 'table' || savedMode === 'grid') {
      this.viewMode.set(savedMode);
    }

    this.loadGroups();
    this.loadStudents();
  }

  loadGroups(): void {
    this.academicService.getStudyGroups().subscribe(groups => {
      this.groups.set(groups);
    });
  }

  loadStudents(): void {
    this.isLoading.set(true);
    this.academicService.getStudents().subscribe({
      next: students => {
        this.students.set(students);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  filteredStudents = computed(() => {
    let list = this.students();
    const query = this.searchQuery().trim().toLowerCase();
    const grade = this.selectedGrade();
    const grpId = this.selectedGroupId();
    const status = this.selectedStatus();

    if (query) {
      list = list.filter(s =>
        s.fullName.toLowerCase().includes(query) ||
        s.studentCode.toLowerCase().includes(query) ||
        s.guardianPhone.includes(query)
      );
    }

    if (grade !== 'all') {
      list = list.filter(s => s.grade === grade);
    }

    if (grpId !== 'all') {
      list = list.filter(s => s.enrolledGroupIds?.includes(grpId));
    }

    if (status !== 'all') {
      list = list.filter(s => s.status === status);
    }

    return list;
  });

  setViewMode(mode: 'grid' | 'table'): void {
    this.viewMode.set(mode);
    localStorage.setItem('tahdeera_students_view_mode', mode);
  }

  navigateToProfile(studentId: string): void {
    this.router.navigate(['/workspace/students', studentId]);
  }

  onStudentCreated(payload: any): void {
    this.academicService.createStudent(payload).subscribe(newStudent => {
      this.students.update(prev => [newStudent, ...prev]);
      this.showAddModal.set(false);
    });
  }

  onStudentLinked(payload: { studentCode: string; secretPin: string }): void {
    this.academicService.linkExistingStudent(payload).subscribe({
      next: linkedStudent => {
        this.students.update(prev => [linkedStudent, ...prev]);
        this.showLinkModal.set(false);
      },
      error: err => {
        alert(err.error?.detail || 'فشل ربط الطالب بالمركز');
      }
    });
  }
}
