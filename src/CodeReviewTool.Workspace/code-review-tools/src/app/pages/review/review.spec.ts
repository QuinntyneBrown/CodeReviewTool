import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Review } from './review';
import { ComparisonService } from '../../services/comparison.service';
import { BehaviorSubject } from 'rxjs';
import { DiffResult } from '../../models/diff-result';
import { CommonModule } from '@angular/common';
import { DiffViewer } from 'code-review-tool-components';

describe('Review', () => {
  let component: Review;
  let fixture: ComponentFixture<Review>;
  let mockComparisonService: jest.Mocked<ComparisonService>;
  let comparisonResultSubject: BehaviorSubject<DiffResult | null>;

  beforeEach(async () => {
    comparisonResultSubject = new BehaviorSubject<DiffResult | null>(null);

    mockComparisonService = {
      comparisonResult$: comparisonResultSubject.asObservable(),
      initiateComparison: jest.fn(),
      clearResult: jest.fn(),
    } as any;

    await TestBed.configureTestingModule({
      imports: [Review, CommonModule, DiffViewer],
      providers: [
        { provide: ComparisonService, useValue: mockComparisonService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Review);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show loading state when no result', (done) => {
    fixture.detectChanges();
    
    component.viewModel$.subscribe(vm => {
      expect(vm.isLoading).toBe(true);
      expect(vm.result).toBeNull();
      done();
    });
  });

  it('should show result when available', (done) => {
    const mockResult: DiffResult = {
      sourceBranch: 'main',
      targetBranch: 'feature/test',
      files: [],
      statistics: {
        totalFiles: 0,
        totalAdditions: 0,
        totalDeletions: 0,
      },
    };

    comparisonResultSubject.next(mockResult);
    fixture.detectChanges();

    component.viewModel$.subscribe(vm => {
      expect(vm.isLoading).toBe(false);
      expect(vm.result).toEqual(mockResult);
      done();
    });
  });

  it('should render diff-viewer when result is available', async () => {
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

    comparisonResultSubject.next(mockResult);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement;
    const diffViewer = compiled.querySelector('crt-diff-viewer');
    expect(diffViewer).toBeTruthy();
  });

  it('should show loading message when result is null', async () => {
    comparisonResultSubject.next(null);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement;
    const loadingMessage = compiled.querySelector('.review__loading');
    expect(loadingMessage).toBeTruthy();
    expect(loadingMessage.textContent).toContain('Loading');
  });
});
