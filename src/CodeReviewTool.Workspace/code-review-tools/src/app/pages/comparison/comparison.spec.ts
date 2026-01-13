import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Comparison } from './comparison';
import { ComparisonService } from '../../services/comparison.service';
import { ComparisonInput, ComparisonRequest } from 'code-review-tool-components';

describe('Comparison', () => {
  let component: Comparison;
  let fixture: ComponentFixture<Comparison>;
  let mockComparisonService: jest.Mocked<ComparisonService>;
  let mockRouter: jest.Mocked<Router>;

  beforeEach(async () => {
    mockComparisonService = {
      initiateComparison: jest.fn(),
      comparisonResult$: jest.fn() as any,
      clearResult: jest.fn(),
    } as any;

    mockRouter = {
      navigate: jest.fn(),
    } as any;

    await TestBed.configureTestingModule({
      imports: [Comparison, ComparisonInput],
      providers: [
        { provide: ComparisonService, useValue: mockComparisonService },
        { provide: Router, useValue: mockRouter },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Comparison);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call comparison service and navigate on compare', () => {
    const request: ComparisonRequest = {
      repositoryPath: '/path/to/repo',
      sourceBranch: 'main',
      targetBranch: 'feature/test',
    };

    component.onCompare(request);

    expect(mockComparisonService.initiateComparison).toHaveBeenCalledWith(request);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/review']);
  });

  it('should render comparison-input component', () => {
    const compiled = fixture.nativeElement;
    const comparisonInput = compiled.querySelector('crt-comparison-input');
    expect(comparisonInput).toBeTruthy();
  });
});
