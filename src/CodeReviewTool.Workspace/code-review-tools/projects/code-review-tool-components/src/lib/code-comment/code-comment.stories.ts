import type { Meta, StoryObj } from '@storybook/angular';
import { CodeComment } from './code-comment';
import { CodeComment as CommentModel } from '../models/diff.model';

const simpleComment: CommentModel = {
  commentId: '1',
  lineNumber: 14,
  author: 'John Doe',
  authorInitials: 'JD',
  timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
  content: 'Great improvement! Using observables with the async pipe is much better than manual subscriptions.',
  resolved: false,
};

const resolvedComment: CommentModel = {
  commentId: '2',
  lineNumber: 14,
  author: 'Alice Smith',
  authorInitials: 'AS',
  timestamp: new Date(Date.now() - 1 * 60 * 60 * 1000), // 1 hour ago
  content: 'Agreed! Make sure to also update the template to use the async pipe.',
  resolved: true,
};

const commentWithReplies: CommentModel = {
  commentId: '3',
  lineNumber: 20,
  author: 'Bob Johnson',
  authorInitials: 'BJ',
  timestamp: new Date(Date.now() - 3 * 60 * 60 * 1000), // 3 hours ago
  content: 'Should we add error handling here?',
  resolved: false,
  replies: [
    {
      commentId: '3-1',
      lineNumber: 20,
      author: 'Alice Smith',
      authorInitials: 'AS',
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000),
      content: 'Good point! I\'ll add a try-catch block.',
      resolved: false,
    },
    {
      commentId: '3-2',
      lineNumber: 20,
      author: 'Bob Johnson',
      authorInitials: 'BJ',
      timestamp: new Date(Date.now() - 1 * 60 * 60 * 1000),
      content: 'Thanks! That looks better.',
      resolved: false,
    },
  ],
};

const meta: Meta<CodeComment> = {
  title: 'Code Review Tool/Code Comment',
  component: CodeComment,
  tags: ['autodocs'],
};

export default meta;
type Story = StoryObj<CodeComment>;

export const Simple: Story = {
  args: {
    comment: simpleComment,
    isReplyFormVisible: false,
  },
};

export const Resolved: Story = {
  args: {
    comment: resolvedComment,
    isReplyFormVisible: false,
  },
};

export const WithReplyForm: Story = {
  args: {
    comment: simpleComment,
    isReplyFormVisible: true,
  },
};

export const WithReplies: Story = {
  args: {
    comment: commentWithReplies,
    isReplyFormVisible: true,
  },
};
