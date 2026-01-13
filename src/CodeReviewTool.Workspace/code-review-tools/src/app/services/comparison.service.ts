import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ComparisonRequest } from 'code-review-tool-components';
import { DiffResult } from '../models/diff-result';
import { SignalRService } from './signalr.service';
import { HttpService } from './http.service';

@Injectable({
  providedIn: 'root'
})
export class ComparisonService {
  private _comparisonResultSubject = new BehaviorSubject<DiffResult | null>(null);
  public comparisonResult$: Observable<DiffResult | null> = this._comparisonResultSubject.asObservable();

  constructor(
    private _signalRService: SignalRService,
    private _httpService: HttpService
  ) {
    this._signalRService.diffResult$.subscribe(result => {
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
}
