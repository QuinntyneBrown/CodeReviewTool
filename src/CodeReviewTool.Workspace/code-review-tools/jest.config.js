module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: [
    '<rootDir>/node_modules/',
    '<rootDir>/dist/',
    '<rootDir>/src/e2e/'
  ],
  collectCoverageFrom: [
    'projects/code-review-tool-components/src/lib/**/*.ts',
    '!projects/code-review-tool-components/src/lib/**/*.stories.ts',
    '!projects/code-review-tool-components/src/lib/**/*.spec.ts',
    '!projects/code-review-tool-components/src/lib/**/index.ts',
  ],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80,
      lines: 80,
      statements: 80,
    },
  },
  coverageReporters: ['html', 'text', 'text-summary', 'lcov'],
};
