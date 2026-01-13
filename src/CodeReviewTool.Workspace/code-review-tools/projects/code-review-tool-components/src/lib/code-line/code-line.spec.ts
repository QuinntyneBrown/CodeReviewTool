import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CodeLine } from './code-line';
import { DiffLine } from '../models/diff.model';
import { MatIconModule } from '@angular/material/icon';

describe('CodeLine', () => {
  let component: CodeLine;
  let fixture: ComponentFixture<CodeLine>;

  const mockLine: DiffLine = {
    lineNumber: 10,
    content: 'test content',
    type: 'context',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CodeLine, MatIconModule],
    }).compileComponents();

    fixture = TestBed.createComponent(CodeLine);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('line', mockLine);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display line number', () => {
    const compiled = fixture.nativeElement;
    const lineNumber = compiled.querySelector('.code-line__number');
    
    expect(lineNumber.textContent.trim()).toContain('10');
  });

  it('should display line content', () => {
    const compiled = fixture.nativeElement;
    const lineContent = compiled.querySelector('.code-line__content');
    
    expect(lineContent.textContent).toBe('test content');
  });

  it('should show comment button when showCommentButton is true', () => {
    fixture.componentRef.setInput('showCommentButton', true);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement;
    const commentButton = compiled.querySelector('.code-line__add-comment');
    
    expect(commentButton).toBeTruthy();
  });

  it('should not show comment button when showCommentButton is false', () => {
    fixture.componentRef.setInput('showCommentButton', false);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement;
    const commentButton = compiled.querySelector('.code-line__add-comment');
    
    expect(commentButton).toBeFalsy();
  });

  it('should emit addComment event when comment button is clicked', () => {
    const emitSpy = jest.spyOn(component.addComment, 'emit');
    fixture.componentRef.setInput('showCommentButton', true);
    fixture.detectChanges();
    
    component.onAddComment();
    
    expect(emitSpy).toHaveBeenCalledWith(10);
  });

  it('should add visible class when hasComment is true', () => {
    fixture.componentRef.setInput('hasComment', true);
    fixture.componentRef.setInput('showCommentButton', true);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement;
    const commentButton = compiled.querySelector('.code-line__add-comment');
    
    expect(commentButton.classList.contains('code-line__add-comment--visible')).toBe(true);
  });
});
