import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ComparisonInput, ComparisonRequest } from 'code-review-tool-components';
import { ComparisonService } from '../../services/comparison.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  imports: [
    CommonModule,
    ComparisonInput,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly comparisonService = inject(ComparisonService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  isLoading = false;

  onCompare(request: ComparisonRequest): void {
    this.isLoading = true;

    this.comparisonService.requestComparison({
      repositoryPath: request.repositoryPath,
      fromBranch: request.fromBranch,
      intoBranch: request.intoBranch
    }).subscribe({
      next: (result) => {
        this.isLoading = false;
        if (result.status === 'Completed' || result.status === 'Success') {
          this.router.navigate(['/review', result.requestId]);
        } else {
          this.snackBar.open(
            'Comparison is being processed. Please wait...',
            'Close',
            { duration: 3000 }
          );
          setTimeout(() => {
            this.router.navigate(['/review', result.requestId]);
          }, 2000);
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.snackBar.open(
          `Error: ${error.message || 'Failed to compare branches'}`,
          'Close',
          { duration: 5000 }
        );
      }
    });
  }
}
