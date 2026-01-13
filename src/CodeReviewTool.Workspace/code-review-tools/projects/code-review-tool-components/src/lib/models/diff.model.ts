export interface DiffFile {
  fileName: string;
  additions: number;
  deletions: number;
  changes: DiffLine[];
}

export interface DiffLine {
  lineNumber: number;
  content: string;
  type: 'added' | 'removed' | 'context' | 'hunk-header';
}

export interface DiffHunk {
  header: string;
  lines: DiffLine[];
}

export interface CodeComment {
  commentId: string;
  lineNumber: number;
  author: string;
  authorInitials: string;
  timestamp: Date;
  content: string;
  resolved: boolean;
  replies?: CodeComment[];
}
