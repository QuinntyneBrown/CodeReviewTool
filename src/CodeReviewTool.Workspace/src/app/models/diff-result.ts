// Re-export library models
export type { DiffFile, DiffLine, DiffHunk, CommentData } from 'code-review-tool-components';

// Backend API response types
export interface ComparisonResponse {
  requestId: string;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  sourceBranch: string;
  targetBranch: string;
  fileDiffs: FileDiffResponse[];
  totalAdditions: number;
  totalDeletions: number;
  totalModifications: number;
  completedAt?: string;
  errorMessage?: string;
}

export interface FileDiffResponse {
  filePath: string;
  changeType: string;
  additions: number;
  deletions: number;
  lineChanges?: LineDiffResponse[];
}

export interface LineDiffResponse {
  lineNumber: number;
  content: string;
  type: 'Addition' | 'Deletion' | 'Context';
}

// SignalR notification type
export interface NotificationMessage {
  messageId: string;
  type: string;
  payload: string;
  createdAt: string;
}

// Comparison state for async pipe
export interface ComparisonState {
  requestId: string | null;
  loading: boolean;
  error: string | null;
}
