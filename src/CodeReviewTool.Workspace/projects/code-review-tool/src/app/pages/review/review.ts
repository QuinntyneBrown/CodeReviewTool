import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BehaviorSubject, Subject } from 'rxjs';
import { combineLatest } from 'rxjs';
import { map, switchMap, takeUntil, tap, catchError, filter, distinctUntilChanged } from 'rxjs/operators';
import { DiffViewer, DiffFile, DiffLine } from 'code-review-tool-components';
import { ComparisonService } from '../../services/comparison.service';
import { SignalRService } from '../../services/signalr.service';
import { ComparisonResponse, LineDiffResponse } from '../../models/diff-result';

interface ReviewState {
  files: DiffFile[];
  fromBranch: string;
  intoBranch: string;
  loading: boolean;
  error: string | null;
}

@Component({
  selector: 'app-review',
  standalone: true,
  imports: [CommonModule, DiffViewer, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './review.html',
  styleUrl: './review.scss',
})
export class Review implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly comparisonService = inject(ComparisonService);
  private readonly signalRService = inject(SignalRService);
  private readonly destroy$ = new Subject<void>();

  private readonly stateSubject = new BehaviorSubject<ReviewState>({
    files: [],
    fromBranch: '',
    intoBranch: '',
    loading: true,
    error: null,
  });

  readonly state$ = this.stateSubject.asObservable();

  readonly files$ = this.state$.pipe(map((s) => s.files));
  readonly fromBranch$ = this.state$.pipe(map((s) => s.fromBranch));
  readonly intoBranch$ = this.state$.pipe(map((s) => s.intoBranch));
  readonly loading$ = this.state$.pipe(map((s) => s.loading));
  readonly error$ = this.state$.pipe(map((s) => s.error));

  ngOnInit(): void {
    const requestId$ = combineLatest([this.route.paramMap, this.route.queryParamMap]).pipe(
      map(([params, query]) => params.get('id') ?? query.get('requestId')),
      filter((requestId): requestId is string => !!requestId),
      distinctUntilChanged()
    );

    requestId$
      .pipe(
        tap(() => this.updateState({ loading: true, error: null })),
        switchMap((requestId) => this.comparisonService.getComparison(requestId)),
        tap((response) => this.handleComparisonResponse(response)),
        catchError((error) => {
          this.updateState({
            loading: false,
            error: error.message || 'Failed to load comparison',
          });
          throw error;
        }),
        takeUntil(this.destroy$)
      )
      .subscribe();

    this.signalRService.connect().pipe(takeUntil(this.destroy$)).subscribe();

    this.signalRService.comparisonResults$
      .pipe(takeUntil(this.destroy$))
      .subscribe((response) => this.handleComparisonResponse(response));
  }

  private handleComparisonResponse(response: ComparisonResponse): void {
    const files = this.mapResponseToFiles(response);
    this.updateState({
      files,
      fromBranch: response.fromBranch,
      intoBranch: response.intoBranch,
      loading: false,
      error: response.errorMessage || null,
    });
  }

  private mapResponseToFiles(response: ComparisonResponse): DiffFile[] {
    return response.fileDiffs.map((fileDiff) => ({
      fileName: fileDiff.filePath,
      additions: fileDiff.additions,
      deletions: fileDiff.deletions,
      changes: this.mapLineChanges(fileDiff.lineChanges || []),
    }));
  }

  private mapLineChanges(lines: LineDiffResponse[]): DiffLine[] {
    return lines.map((line) => ({
      lineNumber: line.lineNumber,
      content: line.content,
      type: this.mapDiffType(line.type),
    }));
  }

  private mapDiffType(type: string): 'added' | 'removed' | 'context' | 'hunk-header' {
    switch (type) {
      case 'Addition':
        return 'added';
      case 'Deletion':
        return 'removed';
      default:
        return 'context';
    }
  }

  private updateState(partial: Partial<ReviewState>): void {
    this.stateSubject.next({
      ...this.stateSubject.getValue(),
      ...partial,
    });
  }

  onBackToComparison(): void {
    this.router.navigate(['/']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
