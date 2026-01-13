import type { Meta, StoryObj } from '@storybook/angular';
import { ComparisonInput } from './comparison-input';

const meta: Meta<ComparisonInput> = {
  title: 'Code Review Tool/Comparison Input',
  component: ComparisonInput,
  tags: ['autodocs'],
  argTypes: {
    repositoryPath: { control: 'text' },
    sourceBranch: { control: 'text' },
    targetBranch: { control: 'text' },
  },
};

export default meta;
type Story = StoryObj<ComparisonInput>;

export const Empty: Story = {
  args: {
    repositoryPath: '',
    sourceBranch: 'main',
    targetBranch: '',
  },
};

export const WithData: Story = {
  args: {
    repositoryPath: '/home/user/projects/my-repo',
    sourceBranch: 'main',
    targetBranch: 'feature/new-feature',
  },
};

export const CustomBranches: Story = {
  args: {
    repositoryPath: '/path/to/repository',
    sourceBranch: 'develop',
    targetBranch: 'feature/user-auth',
  },
};
