import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CodeReviewToolComponents } from './code-review-tool-components';

describe('CodeReviewToolComponents', () => {
  let component: CodeReviewToolComponents;
  let fixture: ComponentFixture<CodeReviewToolComponents>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CodeReviewToolComponents]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CodeReviewToolComponents);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
