export type ExamStatus = 'Draft' | 'Published' | 'Closed';
export type QuestionDifficulty = 'Easy' | 'Medium' | 'Hard';
export type QuestionSourceType = 'Manual' | 'AiGeneratedDraft' | 'AiApproved';

export interface QuestionOption {
  id: string;
  textAr: string;
  isCorrect: boolean;
}

export interface QuestionItem {
  id: string;                         // UUID v4
  subject: string;                    // E.g. "فيزياء"
  lesson?: string;                    // E.g. "الحركة الموجية"
  difficulty: QuestionDifficulty;
  questionText?: string;
  textAr?: string;
  options?: any[];                    // string[] or QuestionOption[]
  correctAnswerIndex?: number;        // 0..3
  sourceType: QuestionSourceType;
  isApproved: boolean;
  points?: number;
  type?: string;
  createdAtUtc?: string;
}

export interface ExamItem {
  id: string;                         // UUID v4
  workspaceId?: string;
  title: string;
  subject?: string;
  gradeName?: string;
  groupId?: string;
  groupName?: string;
  durationMinutes: number;            // E.g. 45
  maxAttempts: number;                // E.g. 1
  totalMarks?: number;                // E.g. 20
  totalPoints?: number;               // E.g. 20
  questionsCount: number;             // E.g. 10
  questionIds?: string[];
  submissionsCount?: number;
  submissionCount?: number;
  averageScorePercent?: number;
  status: ExamStatus;
  autoGradingEnabled?: boolean;
  autoSendWhatsappResults?: boolean;
  publishDateUtc?: string;
}

export interface ExamSubmissionAnswer {
  questionId: string;
  selectedOptionIndex?: number;
  selectedOptionId?: string;
  isCorrect?: boolean;
  marksAwarded?: number;
}

export interface ExamSubmission {
  id: string;
  examId: string;
  studentId: string;
  studentCode: string;
  studentName?: string;
  attemptNumber: number;
  score: number;
  maxScore?: number;
  totalMarks?: number;
  percentage: number;
  submittedAt?: string;
  submittedAtUtc?: string;
  answers: ExamSubmissionAnswer[];
}

export interface HomeworkItem {
  id: string;
  workspaceId?: string;
  title: string;
  subject?: string;
  gradeName?: string;
  groupId?: string;
  groupName?: string;
  dueDate?: string;
  dueDateUtc?: string;
  maxScore?: number;
  totalPoints?: number;
  submittedCount?: number;
  submissionCount?: number;
  lateCount?: number;
  missingCount?: number;
  status?: 'Active' | 'Closed';
}

export interface LibraryItem {
  id: string;
  workspaceId?: string;
  title: string;
  subject?: string;
  gradeName?: string;
  groupId?: string;
  groupName?: string;
  type?: 'Pdf' | 'Video' | 'Image';
  fileType?: 'PDF' | 'Video' | 'Image';
  fileSizeBytes?: number;
  fileSizeMb?: number;
  fileUrl?: string;
  downloadUrl?: string;
  downloadCount?: number;
  uploadedAtUtc?: string;
}
