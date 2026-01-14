import type { Meta, StoryObj } from '@storybook/angular';
import { CodeLine } from './code-line';
import { DiffLine } from '../models/diff.model';

const contextLine: DiffLine = {
  lineNumber: 12,
  content: '  private _authService = inject(AuthService);',
  type: 'context'
};

const addedLine: DiffLine = {
  lineNumber: 14,
  content: '+  isLoggedIn$ = this._authService.isAuthenticated$;',
  type: 'added'
};

const removedLine: DiffLine = {
  lineNumber: 14,
  content: '-  isLoggedIn = false;',
  type: 'removed'
};

const meta: Meta<CodeLine> = {
  title: 'Code Review Tool/Code Line',
  component: CodeLine,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<CodeLine>;

export const Context: Story = {
  args: {
    line: contextLine,
    showCommentButton: true,
    hasComment: false,
  },
};

export const Added: Story = {
  args: {
    line: addedLine,
    showCommentButton: true,
    hasComment: false,
  },
};

export const Removed: Story = {
  args: {
    line: removedLine,
    showCommentButton: true,
    hasComment: false,
  },
};

export const WithComment: Story = {
  args: {
    line: addedLine,
    showCommentButton: true,
    hasComment: true,
  },
};

export const NoCommentButton: Story = {
  args: {
    line: contextLine,
    showCommentButton: false,
    hasComment: false,
  },
};
