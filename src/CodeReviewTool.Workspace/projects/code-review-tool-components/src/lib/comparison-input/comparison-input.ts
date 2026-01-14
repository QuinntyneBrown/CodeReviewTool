import { Component, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ComparisonRequest {
  repositoryPath: string;
  sourceBranch: string;
  targetBranch: string;
}

@Component({
  selector: 'crt-comparison-input',
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './comparison-input.html',
  styleUrl: './comparison-input.scss',
})
export class ComparisonInput {
  repositoryPath = '';
  sourceBranch = 'main';
  targetBranch = '';

  compare = output<ComparisonRequest>();

  onCompare(): void {
    if (this.repositoryPath && this.sourceBranch && this.targetBranch) {
      this.compare.emit({
        repositoryPath: this.repositoryPath,
        sourceBranch: this.sourceBranch,
        targetBranch: this.targetBranch
      });
    }
  }

  onClear(): void {
    this.repositoryPath = '';
    this.sourceBranch = 'main';
    this.targetBranch = '';
  }
}
