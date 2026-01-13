import { DiffFile } from 'code-review-tool-components';

export interface DiffResult {
  sourceBranch: string;
  targetBranch: string;
  files: DiffFile[];
  statistics: DiffStatistics;
}

export interface DiffStatistics {
  totalFiles: number;
  totalAdditions: number;
  totalDeletions: number;
}
