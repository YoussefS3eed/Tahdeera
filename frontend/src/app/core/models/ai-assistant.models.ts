export interface PromptChip {
  id: string;
  labelAr?: string;
  labelEn?: string;
  titleAr?: string;
  titleEn?: string;
  prompt?: string;
  promptText?: string;
  category?: 'Risk' | 'Finance' | 'Academics' | string;
}

export interface AiDailyQuota {
  used: number;
  limit: number;
  resetAt?: string;
}

export interface AiMetric {
  label: string;
  value: string | number;
}

export interface AiQueryRequest {
  question?: string;
  queryText?: string;
  workspaceId?: string;
}

export interface AiQueryResponse {
  id?: string;
  queryId?: string;
  question?: string;
  isOutOfScope: boolean;
  answerAr?: string;
  analysisNarrativeAr?: string;
  analysisNarrativeEn?: string;
  metrics?: AiMetric[];
  sqlSnippet?: string;
  resultTable?: {
    headers: string[];
    rows: (string | number)[][];
  };
  suggestedChips?: PromptChip[];
  suggestedPromptChips?: string[];
  queriesUsedToday?: number;
  dailyLimit?: number;
  createdAt?: string;
}
