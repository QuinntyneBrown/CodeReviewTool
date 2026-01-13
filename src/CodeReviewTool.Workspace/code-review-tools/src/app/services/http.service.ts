import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ComparisonRequest } from 'code-review-tool-components';
import { environment } from '../../environments/environment';

export interface CompareCommand {
  repositoryPath: string;
  sourceBranch: string;
  targetBranch: string;
}

@Injectable({
  providedIn: 'root'
})
export class HttpService {
  private _http = inject(HttpClient);
  private _baseUrl = environment.apiUrl;

  compareRepositories(request: ComparisonRequest): Observable<void> {
    const command: CompareCommand = {
      repositoryPath: request.repositoryPath,
      sourceBranch: request.sourceBranch,
      targetBranch: request.targetBranch
    };
    
    return this._http.post<void>(`${this._baseUrl}/api/compare`, command);
  }
}
