export interface DiffResult {
  sourceBranch: string;
  targetBranch: string;
  files: FileDiff[];
  statistics: DiffStatistics;
}

export interface FileDiff {
  filePath: string;
  hunks: DiffHunk[];
  addedLines: number;
  removedLines: number;
}

export interface DiffHunk {
  oldStart: number;
  oldLines: number;
  newStart: number;
  newLines: number;
  lines: DiffLine[];
  header: string;
}

export interface DiffLine {
  type: 'added' | 'removed' | 'context';
  content: string;
  lineNumber: number;
  oldLineNumber?: number;
  newLineNumber?: number;
}

export interface DiffStatistics {
  totalFiles: number;
  totalAdditions: number;
  totalDeletions: number;
}
