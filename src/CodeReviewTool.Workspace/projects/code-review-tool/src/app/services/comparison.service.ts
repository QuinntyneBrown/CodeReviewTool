import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { DiffFile } from 'code-review-tool-components';

export interface ComparisonRequest {
  repositoryPath: string;
  fromBranch: string;
  intoBranch: string;
}

export interface ComparisonResult {
  requestId: string;
  status: string;
  fromBranch: string;
  intoBranch: string;
  fileDiffs: FileDiff[];
  totalAdditions: number;
  totalDeletions: number;
  totalModifications: number;
  completedAt?: Date;
  errorMessage?: string;
}

interface FileDiff {
  filePath: string;
  changeType: string;
  additions: number;
  deletions: number;
  lineChanges: LineDiff[];
}

interface LineDiff {
  lineNumber: number;
  content: string;
  type: string;
}

@Injectable({
  providedIn: 'root'
})
export class ComparisonService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5000';

  requestComparison(request: ComparisonRequest): Observable<ComparisonResult> {
    return this.http.post<ComparisonResult>(
      `${this.baseUrl}/api/comparison`,
      request
    );
  }

  getComparisonResult(requestId: string): Observable<ComparisonResult> {
    return this.http.get<ComparisonResult>(
      `${this.baseUrl}/api/comparison/${requestId}`
    );
  }

  mapToDiffFiles(result: ComparisonResult): DiffFile[] {
    return result.fileDiffs.map(file => ({
      fileName: file.filePath,
      additions: file.additions,
      deletions: file.deletions,
      changes: file.lineChanges.map(line => ({
        lineNumber: line.lineNumber,
        content: line.content,
        type: this.mapLineType(line.type)
      }))
    }));
  }

  private mapLineType(type: string): 'added' | 'removed' | 'context' | 'hunk-header' {
    switch (type.toLowerCase()) {
      case 'added':
      case 'addition':
        return 'added';
      case 'removed':
      case 'deletion':
        return 'removed';
      case 'hunk-header':
      case 'header':
        return 'hunk-header';
      default:
        return 'context';
    }
  }
}
