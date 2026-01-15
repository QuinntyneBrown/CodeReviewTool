import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { DiffViewer, DiffFile, CodeComment, CommentData } from 'code-review-tool-components';
import { ComparisonService, ComparisonResult } from '../../services/comparison.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { map, Observable, switchMap, timer, takeWhile, catchError, of } from 'rxjs';

@Component({
  selector: 'app-review',
  imports: [
    CommonModule,
    DiffViewer,
    CodeComment,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './review.html',
  styleUrl: './review.scss',
})
export class Review implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly comparisonService = inject(ComparisonService);
  private readonly snackBar = inject(MatSnackBar);

  comparisonResult$!: Observable<ComparisonResult | null>;
  diffFiles$!: Observable<DiffFile[]>;
  isLoading$!: Observable<boolean>;
  error$!: Observable<string | null>;

  // Sample comments for demonstration
  sampleComments: CommentData[] = [
    {
      commentId: '1',
      lineNumber: 14,
      author: 'John Doe',
      authorInitials: 'JD',
      timestamp: new Date(Date.now() - 7200000),
      content: 'Great improvement! Using observables with the async pipe is much better than manual subscriptions.',
      resolved: false,
      replies: [
        {
          commentId: '2',
          lineNumber: 14,
          author: 'Alice Smith',
          authorInitials: 'AS',
          timestamp: new Date(Date.now() - 3600000),
          content: 'Agreed! Make sure to also update the template to use the async pipe.',
          resolved: true
        }
      ]
    }
  ];

  ngOnInit(): void {
    const requestId$ = this.route.paramMap.pipe(
      map(params => params.get('id'))
    );

    this.comparisonResult$ = requestId$.pipe(
      switchMap(id => {
        if (!id) {
          this.router.navigate(['/']);
          return of(null);
        }
        return this.pollForCompletion(id);
      }),
      catchError(error => {
        this.snackBar.open(
          `Error loading comparison: ${error.message}`,
          'Close',
          { duration: 5000 }
        );
        return of(null);
      })
    );

    this.diffFiles$ = this.comparisonResult$.pipe(
      map(result => result ? this.comparisonService.mapToDiffFiles(result) : [])
    );

    this.isLoading$ = this.comparisonResult$.pipe(
      map(result => !result || result.status === 'Processing' || result.status === 'Pending')
    );

    this.error$ = this.comparisonResult$.pipe(
      map(result => result?.errorMessage || null)
    );
  }

  private pollForCompletion(requestId: string): Observable<ComparisonResult> {
    return timer(0, 2000).pipe(
      switchMap(() => this.comparisonService.getComparisonResult(requestId)),
      takeWhile(result => 
        result.status === 'Processing' || result.status === 'Pending',
        true
      )
    );
  }

  onBackHome(): void {
    this.router.navigate(['/']);
  }

  onReply(event: { commentId: string; content: string }): void {
    this.snackBar.open(
      `Reply added to comment ${event.commentId}`,
      'Close',
      { duration: 3000 }
    );
  }

  onResolve(commentId: string): void {
    this.snackBar.open(
      `Comment ${commentId} marked as resolved`,
      'Close',
      { duration: 3000 }
    );
  }
}
