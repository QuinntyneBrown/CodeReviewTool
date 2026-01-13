import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { DiffFile, DiffLine } from '../models/diff.model';
import { CodeLine } from '../code-line/code-line';

@Component({
  selector: 'crt-diff-viewer',
  imports: [
    CommonModule,
    MatIconModule,
    MatListModule,
    CodeLine
  ],
  templateUrl: './diff-viewer.html',
  styleUrl: './diff-viewer.scss',
})
export class DiffViewer {
  files = input<DiffFile[]>([]);
  sourceBranch = input<string>('');
  targetBranch = input<string>('');

  selectedFile: DiffFile | null = null;

  get totalAdditions(): number {
    return this.files().reduce((sum, file) => sum + file.additions, 0);
  }

  get totalDeletions(): number {
    return this.files().reduce((sum, file) => sum + file.deletions, 0);
  }

  selectFile(file: DiffFile): void {
    this.selectedFile = file;
  }

  getLineClass(line: DiffLine): string {
    switch (line.type) {
      case 'added':
        return 'diff-viewer__line--added';
      case 'removed':
        return 'diff-viewer__line--removed';
      case 'hunk-header':
        return 'diff-viewer__line--hunk-header';
      default:
        return 'diff-viewer__line--context';
    }
  }
}
