import type { Meta, StoryObj } from '@storybook/angular';
import { ComparisonInput } from './comparison-input';

const meta: Meta<ComparisonInput> = {
  title: 'Code Review Tool/Comparison Input',
  component: ComparisonInput,
  tags: ['autodocs'],
  argTypes: {
    repositoryPath: { control: 'text' },
    fromBranch: { control: 'text' },
    intoBranch: { control: 'text' },
  },
};

export default meta;
type Story = StoryObj<ComparisonInput>;

export const Empty: Story = {
  args: {
    repositoryPath: '',
    fromBranch: 'main',
    intoBranch: '',
  },
};

export const WithData: Story = {
  args: {
    repositoryPath: '/home/user/projects/my-repo',
    fromBranch: 'main',
    intoBranch: 'feature/new-feature',
  },
};

export const CustomBranches: Story = {
  args: {
    repositoryPath: '/path/to/repository',
    fromBranch: 'develop',
    intoBranch: 'feature/user-auth',
  },
};
