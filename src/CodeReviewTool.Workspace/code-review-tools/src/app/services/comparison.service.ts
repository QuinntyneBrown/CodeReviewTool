import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, Subject, takeUntil } from 'rxjs';
import { ComparisonRequest } from 'code-review-tool-components';
import { DiffResult } from '../models/diff-result';
import { SignalRService } from './signalr.service';
import { HttpService } from './http.service';

@Injectable({
  providedIn: 'root'
})
export class ComparisonService implements OnDestroy {
  private _comparisonResultSubject = new BehaviorSubject<DiffResult | null>(null);
  public comparisonResult$: Observable<DiffResult | null> = this._comparisonResultSubject.asObservable();
  private _destroy$ = new Subject<void>();

  constructor(
    private _signalRService: SignalRService,
    private _httpService: HttpService
  ) {
    this._signalRService.diffResult$
      .pipe(takeUntil(this._destroy$))
      .subscribe(result => {
        if (result) {
          this._comparisonResultSubject.next(result);
        }
      });
  }

  initiateComparison(request: ComparisonRequest): void {
    this._comparisonResultSubject.next(null);
    this._httpService.compareRepositories(request).subscribe();
  }

  clearResult(): void {
    this._comparisonResultSubject.next(null);
  }

  ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }
}
