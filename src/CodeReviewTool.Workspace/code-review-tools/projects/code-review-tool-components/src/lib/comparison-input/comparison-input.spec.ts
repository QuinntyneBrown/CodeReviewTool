import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ComparisonInput } from './comparison-input';

describe('ComparisonInput', () => {
  let component: ComparisonInput;
  let fixture: ComponentFixture<ComparisonInput>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ComparisonInput]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ComparisonInput);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
