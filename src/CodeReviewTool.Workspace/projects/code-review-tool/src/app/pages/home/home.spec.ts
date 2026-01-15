import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Home } from './home';
import { ComparisonService, ComparisonResult } from '../../services/comparison.service';
import { MatSnackBar } from '@angular/material/snack-bar';

describe('Home', () => {
  let mockComparisonService: any;
  let mockRouter: any;
  let mockSnackBar: any;

  beforeEach(async () => {
    mockComparisonService = {
      requestComparison: vi.fn()
    };
    mockRouter = {
      navigate: vi.fn()
    };
    mockSnackBar = {
      open: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [Home],
      providers: [
        { provide: ComparisonService, useValue: mockComparisonService },
        { provide: Router, useValue: mockRouter },
        { provide: MatSnackBar, useValue: mockSnackBar },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations()
      ]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Home);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should initialize with isLoading false', () => {
    const fixture = TestBed.createComponent(Home);
    const component = fixture.componentInstance;
    expect(component.isLoading).toBe(false);
  });

  describe('onCompare', () => {
    it('should navigate to review page on successful comparison with Completed status', () => {
      const fixture = TestBed.createComponent(Home);
      const component = fixture.componentInstance;

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

      mockComparisonService.requestComparison.mockReturnValue(of(mockResult));

      component.onCompare({
        repositoryPath: '/test/repo',
        fromBranch: 'main',
        intoBranch: 'feature'
      });

      expect(component.isLoading).toBe(false);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/review', 'test-id']);
    });

    it('should show processing message and navigate after delay for Pending status', async () => {
      vi.useFakeTimers();
      const fixture = TestBed.createComponent(Home);
      const component = fixture.componentInstance;

      const mockResult: ComparisonResult = {
        requestId: 'test-id',
        status: 'Pending',
        fromBranch: 'main',
        intoBranch: 'feature',
        fileDiffs: [],
        totalAdditions: 0,
        totalDeletions: 0,
        totalModifications: 0
      };

      mockComparisonService.requestComparison.mockReturnValue(of(mockResult));

      component.onCompare({
        repositoryPath: '/test/repo',
        fromBranch: 'main',
        intoBranch: 'feature'
      });

      expect(component.isLoading).toBe(false);
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Comparison is being processed. Please wait...',
        'Close',
        { duration: 3000 }
      );

      await vi.advanceTimersByTimeAsync(2100);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/review', 'test-id']);
      vi.useRealTimers();
    });

    it('should show error message on comparison failure', () => {
      const fixture = TestBed.createComponent(Home);
      const component = fixture.componentInstance;

      const error = new Error('Test error');
      mockComparisonService.requestComparison.mockReturnValue(throwError(() => error));

      component.onCompare({
        repositoryPath: '/test/repo',
        fromBranch: 'main',
        intoBranch: 'feature'
      });

      expect(component.isLoading).toBe(false);
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Error: Test error',
        'Close',
        { duration: 5000 }
      );
    });

    it('should set isLoading to true during comparison', () => {
      const fixture = TestBed.createComponent(Home);
      const component = fixture.componentInstance;

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

      mockComparisonService.requestComparison.mockReturnValue(of(mockResult));

      expect(component.isLoading).toBe(false);

      component.onCompare({
        repositoryPath: '/test/repo',
        fromBranch: 'main',
        intoBranch: 'feature'
      });

      expect(component.isLoading).toBe(false);
    });
  });
});
