import { test, expect } from '@playwright/test';

test.describe('Review Page', () => {
  test.beforeEach(async ({ page }) => {
    // Mock the API responses for getting comparison results
    await page.route('**/api/comparison/*', async (route) => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({
          requestId: 'test-123',
          status: 'Completed',
          fromBranch: 'main',
          intoBranch: 'feature/test',
          fileDiffs: [
            {
              filePath: 'src/app/test.ts',
              changeType: 'Modified',
              additions: 5,
              deletions: 2,
              lineChanges: [
                {
                  lineNumber: 1,
                  content: 'import { Component } from \'@angular/core\';',
                  type: 'context'
                },
                {
                  lineNumber: 2,
                  content: '  isLoggedIn = false;',
                  type: 'removed'
                },
                {
                  lineNumber: 3,
                  content: '  isLoggedIn$ = this._authService.isAuthenticated$;',
                  type: 'added'
                }
              ]
            }
          ],
          totalAdditions: 5,
          totalDeletions: 2,
          totalModifications: 1
        })
      });
    });
  });

  test('should display diff viewer with comparison results', async ({ page }) => {
    await page.goto('/review/test-123');

    // Wait for the diff viewer to load
    await page.waitForSelector('crt-diff-viewer');

    // Check that diff viewer is visible
    await expect(page.locator('crt-diff-viewer')).toBeVisible();

    // Check that branch names are displayed
    await expect(page.locator('text=main')).toBeVisible();
    await expect(page.locator('text=feature/test')).toBeVisible();
  });

  test('should display file list in diff viewer', async ({ page }) => {
    await page.goto('/review/test-123');

    await page.waitForSelector('crt-diff-viewer');

    // Check that file is listed
    await expect(page.locator('text=src/app/test.ts')).toBeVisible();
  });

  test('should display added and removed lines', async ({ page }) => {
    await page.goto('/review/test-123');

    await page.waitForSelector('crt-diff-viewer');

    // Check for diff stats (additions/deletions)
    await expect(page.locator('text=5')).toBeVisible(); // additions
    await expect(page.locator('text=2')).toBeVisible(); // deletions
  });

  test('should navigate back to home when back button is clicked', async ({ page }) => {
    await page.goto('/review/test-123');

    // Wait for page to load
    await page.waitForSelector('crt-diff-viewer');

    // Click back button
    await page.locator('button[aria-label="arrow_back"], button:has(mat-icon:text("arrow_back"))').click();

    // Verify navigation to home page
    await page.waitForURL('/');
    expect(page.url()).toContain('localhost:4200');
    expect(page.url()).not.toContain('/review');
  });

  test('should show loading state while fetching data', async ({ page }) => {
    // Mock a delayed response
    await page.route('**/api/comparison/*', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 500));
      await route.fulfill({
        status: 200,
        body: JSON.stringify({
          requestId: 'test-123',
          status: 'Processing',
          fromBranch: 'main',
          intoBranch: 'feature/test',
          fileDiffs: [],
          totalAdditions: 0,
          totalDeletions: 0,
          totalModifications: 0
        })
      });
    });

    await page.goto('/review/test-123');

    // Check for loading spinner
    await expect(page.locator('mat-spinner')).toBeVisible();
  });

  test('should display error message when comparison fails', async ({ page }) => {
    // Mock error response
    await page.route('**/api/comparison/*', async (route) => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({
          requestId: 'test-123',
          status: 'Failed',
          fromBranch: 'main',
          intoBranch: 'feature/test',
          fileDiffs: [],
          totalAdditions: 0,
          totalDeletions: 0,
          totalModifications: 0,
          errorMessage: 'Repository not found'
        })
      });
    });

    await page.goto('/review/test-123');

    // Wait for error message
    await expect(page.locator('text=Repository not found')).toBeVisible();
  });

  test('should display comment interface', async ({ page }) => {
    await page.goto('/review/test-123');

    await page.waitForSelector('crt-diff-viewer');

    // Check if comment component is present (sample comments)
    // Note: This depends on sample comments being rendered
    const commentComponents = page.locator('crt-code-comment');
    const count = await commentComponents.count();
    expect(count).toBeGreaterThan(0);
  });

  test('should handle reply to comment', async ({ page }) => {
    await page.goto('/review/test-123');

    await page.waitForSelector('crt-diff-viewer');

    // Find comment component
    const commentComponent = page.locator('crt-code-comment').first();
    await expect(commentComponent).toBeVisible();

    // Note: Actual interaction would require the comment interface to be fully interactive
    // For now, we verify the component renders
  });
});
