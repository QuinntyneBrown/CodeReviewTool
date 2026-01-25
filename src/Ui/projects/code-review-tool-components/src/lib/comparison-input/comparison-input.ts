import { Component, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ComparisonRequest {
  repositoryPath: string;
  fromBranch: string;
  intoBranch: string;
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
  fromBranch = 'main';
  intoBranch = '';

  compare = output<ComparisonRequest>();

  onCompare(): void {
    if (this.repositoryPath && this.fromBranch && this.intoBranch) {
      this.compare.emit({
        repositoryPath: this.repositoryPath,
        fromBranch: this.fromBranch,
        intoBranch: this.intoBranch
      });
    }
  }

  onClear(): void {
    this.repositoryPath = '';
    this.fromBranch = 'main';
    this.intoBranch = '';
  }
}
