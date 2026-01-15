import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { vi } from 'vitest';
import { ComparisonService, ComparisonRequest, ComparisonResult } from './comparison.service';

describe('ComparisonService', () => {
  let service: ComparisonService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ComparisonService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(ComparisonService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('requestComparison', () => {
    it('should send POST request to comparison endpoint', () => {
      const mockRequest: ComparisonRequest = {
        repositoryPath: '/test/repo',
        fromBranch: 'main',
        intoBranch: 'feature'
      };

      const mockResponse: ComparisonResult = {
        requestId: 'test-id',
        status: 'Pending',
        fromBranch: 'main',
        intoBranch: 'feature',
        fileDiffs: [],
        totalAdditions: 0,
        totalDeletions: 0,
        totalModifications: 0
      };

      service.requestComparison(mockRequest).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne('http://localhost:5000/api/comparison');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(mockRequest);
      req.flush(mockResponse);
    });
  });

  describe('getComparisonResult', () => {
    it('should send GET request to retrieve comparison result', () => {
      const requestId = 'test-id';
      const mockResponse: ComparisonResult = {
        requestId: 'test-id',
        status: 'Completed',
        fromBranch: 'main',
        intoBranch: 'feature',
        fileDiffs: [],
        totalAdditions: 10,
        totalDeletions: 5,
        totalModifications: 2
      };

      service.getComparisonResult(requestId).subscribe(result => {
        expect(result).toEqual(mockResponse);
      });

      const req = httpMock.expectOne(`http://localhost:5000/api/comparison/${requestId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('mapToDiffFiles', () => {
    it('should map ComparisonResult to DiffFile array', () => {
      const mockResult: ComparisonResult = {
        requestId: 'test-id',
        status: 'Completed',
        fromBranch: 'main',
        intoBranch: 'feature',
        fileDiffs: [
          {
            filePath: 'src/test.ts',
            changeType: 'Modified',
            additions: 5,
            deletions: 2,
            lineChanges: [
              { lineNumber: 1, content: 'added line', type: 'added' },
              { lineNumber: 2, content: 'removed line', type: 'removed' },
              { lineNumber: 3, content: 'context line', type: 'context' }
            ]
          }
        ],
        totalAdditions: 5,
        totalDeletions: 2,
        totalModifications: 1
      };

      const result = service.mapToDiffFiles(mockResult);

      expect(result).toHaveLength(1);
      expect(result[0].fileName).toBe('src/test.ts');
      expect(result[0].additions).toBe(5);
      expect(result[0].deletions).toBe(2);
      expect(result[0].changes).toHaveLength(3);
      expect(result[0].changes[0].type).toBe('added');
      expect(result[0].changes[1].type).toBe('removed');
      expect(result[0].changes[2].type).toBe('context');
    });

    it('should handle empty file diffs', () => {
      const mockResult: ComparisonResult = {
        requestId: 'test-id',
        status: 'Completed',
        fromBranch: 'main',
        intoBranch: 'feature',
        fileDiffs: [],
        totalAdditions: 0,
        totalDeletions: 0,
        totalModifications: 0
      };

      const result = service.mapToDiffFiles(mockResult);

      expect(result).toHaveLength(0);
    });

    it('should map hunk-header type correctly', () => {
      const mockResult: ComparisonResult = {
        requestId: 'test-id',
        status: 'Completed',
        fromBranch: 'main',
        intoBranch: 'feature',
        fileDiffs: [
          {
            filePath: 'test.ts',
            changeType: 'Modified',
            additions: 0,
            deletions: 0,
            lineChanges: [
              { lineNumber: 1, content: '@@ -1,5 +1,5 @@', type: 'hunk-header' }
            ]
          }
        ],
        totalAdditions: 0,
        totalDeletions: 0,
        totalModifications: 1
      };

      const result = service.mapToDiffFiles(mockResult);

      expect(result[0].changes[0].type).toBe('hunk-header');
    });
  });
});
