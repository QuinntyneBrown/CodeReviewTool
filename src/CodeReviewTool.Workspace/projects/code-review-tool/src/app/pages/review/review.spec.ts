import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Review } from './review';
import { ComparisonService, ComparisonResult } from '../../services/comparison.service';
import { MatSnackBar } from '@angular/material/snack-bar';

describe('Review', () => {
  let mockComparisonService: any;
  let mockRouter: any;
  let mockSnackBar: any;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockComparisonService = {
      getComparisonResult: vi.fn(),
      mapToDiffFiles: vi.fn()
    };
    mockRouter = {
      navigate: vi.fn()
    };
    mockSnackBar = {
      open: vi.fn()
    };

    mockActivatedRoute = {
      paramMap: of(convertToParamMap({ id: 'test-id' }))
    };

    await TestBed.configureTestingModule({
      imports: [Review],
      providers: [
        { provide: ComparisonService, useValue: mockComparisonService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
        { provide: MatSnackBar, useValue: mockSnackBar },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations()
      ]
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Review);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should have sample comments', () => {
    const fixture = TestBed.createComponent(Review);
    const component = fixture.componentInstance;
    expect(component.sampleComments).toBeDefined();
    expect(component.sampleComments.length).toBeGreaterThan(0);
  });

  describe('ngOnInit', () => {
    it('should load comparison result on init', async () => {
      const mockResult: ComparisonResult = {
        requestId: 'test-id',
        status: 'Completed',
        fromBranch: 'main',
        intoBranch: 'feature',
        fileDiffs: [],
        totalAdditions: 10,
        totalDeletions: 5,
        totalModifications: 2
      };

      mockComparisonService.getComparisonResult.mockReturnValue(of(mockResult));
      mockComparisonService.mapToDiffFiles.mockReturnValue([]);

      const fixture = TestBed.createComponent(Review);
      const component = fixture.componentInstance;

      component.ngOnInit();

      const result = await new Promise((resolve) => {
        component.comparisonResult$.subscribe(result => {
          resolve(result);
        });
      });

      expect(result).toEqual(mockResult);
      expect(mockComparisonService.getComparisonResult).toHaveBeenCalledWith('test-id');
    });

    it('should navigate to home if no id is provided', () => {
      mockActivatedRoute.paramMap = of(convertToParamMap({}));

      const fixture = TestBed.createComponent(Review);
      const component = fixture.componentInstance;

      component.ngOnInit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should handle error when loading comparison result', async () => {
      const error = new Error('Test error');
      mockComparisonService.getComparisonResult.mockReturnValue(throwError(() => error));

      const fixture = TestBed.createComponent(Review);
      const component = fixture.componentInstance;

      component.ngOnInit();

      const result = await new Promise((resolve) => {
        component.comparisonResult$.subscribe(result => {
          resolve(result);
        });
      });

      expect(result).toBeNull();
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Error loading comparison: Test error',
        'Close',
        { duration: 5000 }
      );
    });

    it('should map comparison result to diff files', async () => {
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

      const mockDiffFiles = [
        {
          fileName: 'test.ts',
          additions: 5,
          deletions: 2,
          changes: []
        }
      ];

      mockComparisonService.getComparisonResult.mockReturnValue(of(mockResult));
      mockComparisonService.mapToDiffFiles.mockReturnValue(mockDiffFiles);

      const fixture = TestBed.createComponent(Review);
      const component = fixture.componentInstance;

      component.ngOnInit();

      const files = await new Promise((resolve) => {
        component.diffFiles$.subscribe(files => {
          resolve(files);
        });
      });

      expect(files).toEqual(mockDiffFiles);
    });
  });

  describe('onBackHome', () => {
    it('should navigate to home page', () => {
      const fixture = TestBed.createComponent(Review);
      const component = fixture.componentInstance;

      component.onBackHome();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
    });
  });

  describe('onReply', () => {
    it('should show snackbar with reply confirmation', () => {
      const fixture = TestBed.createComponent(Review);
      const component = fixture.componentInstance;

      component.onReply({ commentId: 'test-comment', content: 'Test reply' });

      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Reply added to comment test-comment',
        'Close',
        { duration: 3000 }
      );
    });
  });

  describe('onResolve', () => {
    it('should show snackbar with resolve confirmation', () => {
      const fixture = TestBed.createComponent(Review);
      const component = fixture.componentInstance;

      component.onResolve('test-comment');

      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Comment test-comment marked as resolved',
        'Close',
        { duration: 3000 }
      );
    });
  });
});
