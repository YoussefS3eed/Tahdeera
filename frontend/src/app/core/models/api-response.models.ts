/**
 * Tahdeera API Response & Error Models
 * Authoritative interfaces matching RFC 9457 Problem Details and Paged Results
 */

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/**
 * RFC 9457 Problem Details Standard
 * Matches ASP.NET Core backend error schema
 */
export interface ProblemDetails {
  type: string;                  // URI reference identifying problem type
  title: string;                 // Short, human-readable summary
  status: number;                // HTTP status code (e.g., 400, 404, 409, 422)
  detail: string;                // Detailed explanation of specific occurrence
  instance?: string;             // URI reference of the specific occurrence
  errorCode?: string;            // Business domain error code (e.g., 'NFC_CARD_UNREGISTERED')
  traceId?: string;              // Request trace identifier for debugging
  errors?: Record<string, string[]>; // Validation error dictionary
}
