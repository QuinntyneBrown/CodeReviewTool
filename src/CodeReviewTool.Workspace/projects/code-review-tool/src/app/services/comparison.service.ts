import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpService } from './http.service';
import { ComparisonRequest } from 'code-review-tool-components';
import { ComparisonResponse } from '../models/diff-result';

@Injectable({
  providedIn: 'root',
})
export class ComparisonService {
  private readonly httpService = inject(HttpService);

  requestComparison(request: ComparisonRequest): Observable<ComparisonResponse> {
    return this.httpService.post<ComparisonResponse, ComparisonRequest>('/comparison', request);
  }

  getComparison(requestId: string): Observable<ComparisonResponse> {
    return this.httpService.get<ComparisonResponse>(`/comparison/${requestId}`);
  }

  getBranches(repositoryPath: string): Observable<string[]> {
    return this.httpService.get<string[]>(
      `/comparison/branches?repositoryPath=${encodeURIComponent(repositoryPath)}`
    );
  }
}
