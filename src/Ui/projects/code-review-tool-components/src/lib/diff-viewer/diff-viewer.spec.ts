import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DiffViewer } from './diff-viewer';
import { DiffFile } from '../models/diff.model';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';

describe('DiffViewer', () => {
  let component: DiffViewer;
  let fixture: ComponentFixture<DiffViewer>;

  const mockFiles: DiffFile[] = [
    {
      fileName: 'file1.ts',
      additions: 10,
      deletions: 5,
      changes: [
        { lineNumber: 1, content: 'line 1', type: 'context' },
        { lineNumber: 2, content: '+ added line', type: 'added' },
        { lineNumber: 3, content: '- removed line', type: 'removed' },
      ],
    },
    {
      fileName: 'file2.ts',
      additions: 3,
      deletions: 1,
      changes: [
        { lineNumber: 1, content: 'line 1', type: 'context' },
      ],
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DiffViewer, MatIconModule, MatListModule],
    }).compileComponents();

    fixture = TestBed.createComponent(DiffViewer);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should calculate total additions correctly', () => {
    fixture.componentRef.setInput('files', mockFiles);
    fixture.detectChanges();
    
    expect(component.totalAdditions).toBe(13);
  });

  it('should calculate total deletions correctly', () => {
    fixture.componentRef.setInput('files', mockFiles);
    fixture.detectChanges();
    
    expect(component.totalDeletions).toBe(6);
  });

  it('should return 0 for total additions when files are empty', () => {
    fixture.componentRef.setInput('files', []);
    fixture.detectChanges();
    
    expect(component.totalAdditions).toBe(0);
  });

  it('should return 0 for total deletions when files are empty', () => {
    fixture.componentRef.setInput('files', []);
    fixture.detectChanges();
    
    expect(component.totalDeletions).toBe(0);
  });

  it('should select a file', () => {
    const file = mockFiles[0];
    component.selectFile(file);
    
    expect(component.selectedFile).toBe(file);
  });

  it('should return correct line class for added line', () => {
    const addedLine = { lineNumber: 1, content: '+ added', type: 'added' as const };
    expect(component.getLineClass(addedLine)).toBe('diff-viewer__line--added');
  });

  it('should return correct line class for removed line', () => {
    const removedLine = { lineNumber: 1, content: '- removed', type: 'removed' as const };
    expect(component.getLineClass(removedLine)).toBe('diff-viewer__line--removed');
  });

  it('should return correct line class for context line', () => {
    const contextLine = { lineNumber: 1, content: 'context', type: 'context' as const };
    expect(component.getLineClass(contextLine)).toBe('diff-viewer__line--context');
  });

  it('should return correct line class for hunk header', () => {
    const hunkHeader = { lineNumber: 0, content: '@@ -1,3 +1,4 @@', type: 'hunk-header' as const };
    expect(component.getLineClass(hunkHeader)).toBe('diff-viewer__line--hunk-header');
  });

  it('should initialize selectedFile as null', () => {
    expect(component.selectedFile).toBeNull();
  });
});
