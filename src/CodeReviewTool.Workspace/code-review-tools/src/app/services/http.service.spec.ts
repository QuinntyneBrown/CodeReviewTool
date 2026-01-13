import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpService } from './http.service';
import { ComparisonRequest } from 'code-review-tool-components';

describe('HttpService', () => {
  let service: HttpService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        HttpService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(HttpService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should send compare request to API', () => {
    const request: ComparisonRequest = {
      repositoryPath: '/path/to/repo',
      sourceBranch: 'main',
      targetBranch: 'feature/test',
    };

    service.compareRepositories(request).subscribe({
      next: response => {
        expect(response).toBeUndefined();
      }
    });

    const req = httpMock.expectOne('http://localhost:5000/api/compare');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      repositoryPath: request.repositoryPath,
      sourceBranch: request.sourceBranch,
      targetBranch: request.targetBranch,
    });

    req.flush(null);
  });

  it('should handle API errors', () => {
    const request: ComparisonRequest = {
      repositoryPath: '/path/to/repo',
      sourceBranch: 'main',
      targetBranch: 'feature/test',
    };

    service.compareRepositories(request).subscribe({
      next: () => fail('Should have failed'),
      error: error => {
        expect(error.status).toBe(500);
      }
    });

    const req = httpMock.expectOne('http://localhost:5000/api/compare');
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
  });
});
