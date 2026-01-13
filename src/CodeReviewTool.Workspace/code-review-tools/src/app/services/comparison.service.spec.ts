import { TestBed } from '@angular/core/testing';
import { ComparisonService } from './comparison.service';
import { SignalRService } from './signalr.service';
import { HttpService } from './http.service';
import { BehaviorSubject, of } from 'rxjs';
import { DiffResult } from '../models/diff-result';
import { ComparisonRequest } from 'code-review-tool-components';

describe('ComparisonService', () => {
  let service: ComparisonService;
  let mockSignalRService: jest.Mocked<SignalRService>;
  let mockHttpService: jest.Mocked<HttpService>;
  let diffResultSubject: BehaviorSubject<DiffResult | null>;

  beforeEach(() => {
    diffResultSubject = new BehaviorSubject<DiffResult | null>(null);

    mockSignalRService = {
      diffResult$: diffResultSubject.asObservable(),
      isConnected$: of(true),
      disconnect: jest.fn(),
    } as any;

    mockHttpService = {
      compareRepositories: jest.fn().mockReturnValue(of(void 0)),
    } as any;

    TestBed.configureTestingModule({
      providers: [
        ComparisonService,
        { provide: SignalRService, useValue: mockSignalRService },
        { provide: HttpService, useValue: mockHttpService },
      ],
    });

    service = TestBed.inject(ComparisonService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should expose comparison result observable', (done) => {
    service.comparisonResult$.subscribe(result => {
      expect(result).toBeNull();
      done();
    });
  });

  it('should receive diff result from SignalR', (done) => {
    const mockResult: DiffResult = {
      sourceBranch: 'main',
      targetBranch: 'feature/test',
      files: [],
      statistics: {
        totalFiles: 1,
        totalAdditions: 10,
        totalDeletions: 5,
      },
    };

    service.comparisonResult$.subscribe(result => {
      if (result) {
        expect(result).toEqual(mockResult);
        done();
      }
    });

    diffResultSubject.next(mockResult);
  });

  it('should initiate comparison and clear previous result', () => {
    const request: ComparisonRequest = {
      repositoryPath: '/path/to/repo',
      sourceBranch: 'main',
      targetBranch: 'feature/test',
    };

    service.initiateComparison(request);

    expect(mockHttpService.compareRepositories).toHaveBeenCalledWith(request);
  });

  it('should clear result', (done) => {
    const mockResult: DiffResult = {
      sourceBranch: 'main',
      targetBranch: 'feature/test',
      files: [],
      statistics: {
        totalFiles: 1,
        totalAdditions: 10,
        totalDeletions: 5,
      },
    };

    diffResultSubject.next(mockResult);

    service.clearResult();

    service.comparisonResult$.subscribe(result => {
      expect(result).toBeNull();
      done();
    });
  });
});
