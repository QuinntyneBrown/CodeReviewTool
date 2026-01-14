import type { Meta, StoryObj } from '@storybook/angular';
import { DiffViewer } from './diff-viewer';
import { DiffFile } from '../models/diff.model';

const mockDiffFiles: DiffFile[] = [
  {
    fileName: 'src/app/components/header.ts',
    additions: 12,
    deletions: 3,
    changes: [
      { lineNumber: 12, content: '  private _authService = inject(AuthService);', type: 'context' },
      { lineNumber: 13, content: '  private _router = inject(Router);', type: 'context' },
      { lineNumber: 14, content: '-  isLoggedIn = false;', type: 'removed' },
      { lineNumber: 14, content: '+  isLoggedIn$ = this._authService.isAuthenticated$;', type: 'added' },
      { lineNumber: 15, content: '+  user$ = this._authService.currentUser$;', type: 'added' },
      { lineNumber: 16, content: '', type: 'context' },
      { lineNumber: 17, content: '-  ngOnInit(): void {', type: 'removed' },
      { lineNumber: 18, content: '-    this._authService.isAuthenticated$.subscribe(', type: 'removed' },
      { lineNumber: 19, content: '-      isAuth => this.isLoggedIn = isAuth', type: 'removed' },
      { lineNumber: 20, content: '-    );', type: 'removed' },
      { lineNumber: 21, content: '-  }', type: 'removed' },
      { lineNumber: 17, content: '+  logout(): void {', type: 'added' },
      { lineNumber: 18, content: '+    this._authService.logout();', type: 'added' },
      { lineNumber: 19, content: '+    this._router.navigate([\'/login\']);', type: 'added' },
      { lineNumber: 20, content: '+  }', type: 'added' },
      { lineNumber: 21, content: '}', type: 'context' },
    ],
  },
  {
    fileName: 'src/app/services/api.service.ts',
    additions: 45,
    deletions: 10,
    changes: [
      { lineNumber: 1, content: 'import { Injectable } from \'@angular/core\';', type: 'context' },
      { lineNumber: 2, content: 'import { HttpClient } from \'@angular/common/http\';', type: 'context' },
      { lineNumber: 3, content: '+import { Observable } from \'rxjs\';', type: 'added' },
    ],
  },
  {
    fileName: 'package.json',
    additions: 3,
    deletions: 1,
    changes: [
      { lineNumber: 10, content: '  "dependencies": {', type: 'context' },
      { lineNumber: 11, content: '-    "@angular/core": "^18.0.0",', type: 'removed' },
      { lineNumber: 11, content: '+    "@angular/core": "^21.0.0",', type: 'added' },
    ],
  },
];

const meta: Meta<DiffViewer> = {
  title: 'Code Review Tool/Diff Viewer',
  component: DiffViewer,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<DiffViewer>;

export const WithFiles: Story = {
  args: {
    files: mockDiffFiles,
    fromBranch: 'main',
    intoBranch: 'feature/new-feature',
  },
};

export const Empty: Story = {
  args: {
    files: [],
    fromBranch: 'main',
    intoBranch: 'feature/empty',
  },
};

export const SingleFile: Story = {
  args: {
    files: [mockDiffFiles[0]],
    fromBranch: 'develop',
    intoBranch: 'feature/header-refactor',
  },
};
