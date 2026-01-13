module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: [
    '<rootDir>/node_modules/',
    '<rootDir>/dist/',
    '<rootDir>/src/e2e/'
  ],
  moduleNameMapper: {
    'code-review-tool-components': '<rootDir>/projects/code-review-tool-components/src/public-api.ts',
  },
  collectCoverageFrom: [
    'projects/code-review-tool-components/src/lib/**/*.ts',
    'src/app/**/*.ts',
    '!projects/code-review-tool-components/src/lib/**/*.stories.ts',
    '!projects/code-review-tool-components/src/lib/**/*.spec.ts',
    '!projects/code-review-tool-components/src/lib/**/index.ts',
    '!projects/code-review-tool-components/src/lib/code-review-tool-components.ts',
    '!projects/code-review-tool-components/src/public-api.ts',
    '!src/app/**/*.spec.ts',
    '!src/app/**/index.ts',
    '!src/app/app.ts',
    '!src/app/app.config.ts',
    '!src/app/app.routes.ts',
    '!src/environments/**',
  ],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 74,  // SignalR callbacks difficult to test in isolation
      lines: 80,
      statements: 80,
    },
  },
  coverageReporters: ['html', 'text', 'text-summary', 'lcov'],
};
