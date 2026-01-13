import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CodeComment } from './code-comment';

describe('CodeComment', () => {
  let component: CodeComment;
  let fixture: ComponentFixture<CodeComment>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CodeComment]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CodeComment);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
