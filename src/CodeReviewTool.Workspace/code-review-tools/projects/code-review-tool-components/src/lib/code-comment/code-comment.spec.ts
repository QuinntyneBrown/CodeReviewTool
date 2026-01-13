import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { CodeComment } from './code-comment';
import { CodeComment as CommentModel } from '../models/diff.model';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

describe('CodeComment', () => {
  let component: CodeComment;
  let fixture: ComponentFixture<CodeComment>;

  const mockComment: CommentModel = {
    commentId: '1',
    lineNumber: 10,
    author: 'John Doe',
    authorInitials: 'JD',
    timestamp: new Date('2024-01-01T12:00:00'),
    content: 'This is a test comment',
    resolved: false,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CodeComment,
        FormsModule,
        MatButtonModule,
        MatIconModule,
        MatChipsModule,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CodeComment);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('comment', mockComment);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display author name', () => {
    const compiled = fixture.nativeElement;
    const author = compiled.querySelector('.code-comment__author');
    
    expect(author.textContent).toBe('John Doe');
  });

  it('should display author initials', () => {
    const compiled = fixture.nativeElement;
    const avatar = compiled.querySelector('.code-comment__avatar');
    
    expect(avatar.textContent).toBe('JD');
  });

  it('should display comment content', () => {
    const compiled = fixture.nativeElement;
    const body = compiled.querySelector('.code-comment__body');
    
    expect(body.textContent).toBe('This is a test comment');
  });

  it('should emit reply event when reply button is clicked', () => {
    const emitSpy = jest.spyOn(component.reply, 'emit');
    component.replyText = 'Test reply';
    
    component.onReply();
    
    expect(emitSpy).toHaveBeenCalledWith({
      commentId: '1',
      content: 'Test reply',
    });
  });

  it('should not emit reply event when reply text is empty', () => {
    const emitSpy = jest.spyOn(component.reply, 'emit');
    component.replyText = '';
    
    component.onReply();
    
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should not emit reply event when reply text is only whitespace', () => {
    const emitSpy = jest.spyOn(component.reply, 'emit');
    component.replyText = '   ';
    
    component.onReply();
    
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should clear reply text after successful reply', () => {
    component.replyText = 'Test reply';
    
    component.onReply();
    
    expect(component.replyText).toBe('');
  });

  it('should emit resolve event when resolve button is clicked', () => {
    const emitSpy = jest.spyOn(component.resolve, 'emit');
    
    component.onResolve();
    
    expect(emitSpy).toHaveBeenCalledWith('1');
  });

  it('should display resolved badge when comment is resolved', () => {
    const resolvedComment = { ...mockComment, resolved: true };
    fixture.componentRef.setInput('comment', resolvedComment);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement;
    const badge = compiled.querySelector('.code-comment__resolved-badge');
    
    expect(badge).toBeTruthy();
    expect(badge.textContent).toContain('Resolved');
  });

  it('should not display resolved badge when comment is not resolved', () => {
    const compiled = fixture.nativeElement;
    const badge = compiled.querySelector('.code-comment__resolved-badge');
    
    expect(badge).toBeFalsy();
  });

  it('should format timestamp as "just now" for very recent comments', () => {
    const now = new Date();
    const comment = { ...mockComment, timestamp: now };
    fixture.componentRef.setInput('comment', comment);
    
    const result = component.formatTimestamp(now);
    
    expect(result).toBe('just now');
  });

  it('should format timestamp in minutes for recent comments', () => {
    const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000);
    
    const result = component.formatTimestamp(fiveMinutesAgo);
    
    expect(result).toBe('5 minutes ago');
  });

  it('should format timestamp in hours for older comments', () => {
    const twoHoursAgo = new Date(Date.now() - 2 * 60 * 60 * 1000);
    
    const result = component.formatTimestamp(twoHoursAgo);
    
    expect(result).toBe('2 hours ago');
  });

  it('should format timestamp in days for very old comments', () => {
    const threeDaysAgo = new Date(Date.now() - 3 * 24 * 60 * 60 * 1000);
    
    const result = component.formatTimestamp(threeDaysAgo);
    
    expect(result).toBe('3 days ago');
  });

  it('should show reply form when isReplyFormVisible is true', () => {
    fixture.componentRef.setInput('isReplyFormVisible', true);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement;
    const replyForm = compiled.querySelector('.code-comment__reply-form');
    
    expect(replyForm).toBeTruthy();
  });

  it('should not show reply form when isReplyFormVisible is false', () => {
    fixture.componentRef.setInput('isReplyFormVisible', false);
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement;
    const replyForm = compiled.querySelector('.code-comment__reply-form');
    
    expect(replyForm).toBeFalsy();
  });
});
