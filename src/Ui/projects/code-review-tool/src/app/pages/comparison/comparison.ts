import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { BehaviorSubject, Subject } from 'rxjs';
import { takeUntil, tap, catchError } from 'rxjs/operators';
import { ComparisonInput, ComparisonRequest } from 'code-review-tool-components';
import { ComparisonService } from '../../services/comparison.service';
import { SignalRService } from '../../services/signalr.service';
import { ComparisonState } from '../../models/diff-result';

@Component({
  selector: 'app-comparison',
  standalone: true,
  imports: [CommonModule, ComparisonInput, MatProgressSpinnerModule, MatSnackBarModule],
  templateUrl: './comparison.html',
  styleUrl: './comparison.scss',
})
export class Comparison implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly comparisonService = inject(ComparisonService);
  private readonly signalRService = inject(SignalRService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  private readonly stateSubject = new BehaviorSubject<ComparisonState>({
    requestId: null,
    loading: false,
    error: null,
  });

  readonly state$ = this.stateSubject.asObservable();

  ngOnInit(): void {
    this.signalRService.connect().pipe(takeUntil(this.destroy$)).subscribe();
  }

  onCompare(request: ComparisonRequest): void {
    this.stateSubject.next({ requestId: null, loading: true, error: null });

    this.comparisonService
      .requestComparison(request)
      .pipe(
        tap((response) => {
          this.stateSubject.next({
            requestId: response.requestId,
            loading: true,
            error: null,
          });

          if (response.status === 'Completed') {
            this.router.navigate(['/review', response.requestId]);
            return;
          }

          if (response.status === 'Failed') {
            const message = response.errorMessage || 'Comparison failed';
            this.stateSubject.next({ requestId: null, loading: false, error: message });
            this.snackBar.open(message, 'Close', { duration: 5000 });
            return;
          }

          this.signalRService.subscribeToComparison(response.requestId).subscribe();

          this.signalRService
            .getComparisonResult(response.requestId)
            .pipe(takeUntil(this.destroy$))
            .subscribe((result) => {
              if (result.status === 'Completed') {
                this.router.navigate(['/review', result.requestId]);
              } else if (result.status === 'Failed') {
                this.stateSubject.next({
                  requestId: null,
                  loading: false,
                  error: result.errorMessage || 'Comparison failed',
                });
                this.snackBar.open(result.errorMessage || 'Comparison failed', 'Close', {
                  duration: 5000,
                });
              }
            });
        }),
        catchError((error) => {
          const message = error.message || 'Failed to request comparison';
          this.stateSubject.next({ requestId: null, loading: false, error: message });
          this.snackBar.open(message, 'Close', { duration: 5000 });
          throw error;
        }),
        takeUntil(this.destroy$)
      )
      .subscribe();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
