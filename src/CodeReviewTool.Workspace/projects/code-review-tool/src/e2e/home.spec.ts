import { test, expect } from '@playwright/test';

test.describe('Home Page', () => {
  test('should display comparison input form', async ({ page }) => {
    await page.goto('/');

    // Check that the page title is correct
    await expect(page.getByRole('heading', { name: 'Code Review Tool' })).toBeVisible();

    // Check that the comparison input component is visible
    await expect(page.locator('crt-comparison-input')).toBeVisible();

    // Check for form fields
    await expect(page.locator('input[placeholder*="repository"]')).toBeVisible();
    await expect(page.locator('input[placeholder*="main"]')).toBeVisible();
    await expect(page.locator('input[placeholder*="feature"]')).toBeVisible();

    // Check for buttons
    await expect(page.locator('button:has-text("Clear")')).toBeVisible();
    await expect(page.locator('button:has-text("Compare")')).toBeVisible();
  });

  test('should clear form fields when Clear button is clicked', async ({ page }) => {
    await page.goto('/');

    // Fill in the form
    await page.locator('input[placeholder*="repository"]').fill('/test/repo');
    await page.locator('input[placeholder*="main"]').first().fill('develop');
    await page.locator('input[placeholder*="feature"]').fill('feature/test');

    // Click Clear button
    await page.locator('button:has-text("Clear")').click();

    // Verify fields are cleared or reset
    const repoInput = page.locator('input[placeholder*="repository"]');
    await expect(repoInput).toHaveValue('');
  });

  test('should show loading spinner during comparison', async ({ page }) => {
    // Mock the API response to delay
    await page.route('**/api/comparison', async (route) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      await route.fulfill({
        status: 200,
        body: JSON.stringify({
          requestId: 'test-123',
          status: 'Completed',
          fromBranch: 'main',
          intoBranch: 'feature',
          fileDiffs: [],
          totalAdditions: 0,
          totalDeletions: 0,
          totalModifications: 0
        })
      });
    });

    await page.goto('/');

    // Fill in the form
    await page.locator('input[placeholder*="repository"]').fill('/test/repo');
    await page.locator('input[placeholder*="main"]').first().fill('main');
    await page.locator('input[placeholder*="feature"]').fill('feature/test');

    // Click Compare button
    await page.locator('button:has-text("Compare")').click();

    // Check that loading spinner appears
    await expect(page.locator('mat-spinner')).toBeVisible();
  });

  test('should navigate to review page on successful comparison', async ({ page }) => {
    // Mock the API response
    await page.route('**/api/comparison', async (route) => {
      await route.fulfill({
        status: 200,
        body: JSON.stringify({
          requestId: 'test-123',
          status: 'Completed',
          fromBranch: 'main',
          intoBranch: 'feature',
          fileDiffs: [],
          totalAdditions: 0,
          totalDeletions: 0,
          totalModifications: 0
        })
      });
    });

    await page.goto('/');

    // Fill in the form
    await page.locator('input[placeholder*="repository"]').fill('/test/repo');
    await page.locator('input[placeholder*="main"]').first().fill('main');
    await page.locator('input[placeholder*="feature"]').fill('feature/test');

    // Click Compare button
    await page.locator('button:has-text("Compare")').click();

    // Wait for navigation
    await page.waitForURL('**/review/**');

    // Verify we're on the review page
    expect(page.url()).toContain('/review/');
  });

  test('should display error message on API failure', async ({ page }) => {
    // Mock the API to return an error
    await page.route('**/api/comparison', async (route) => {
      await route.fulfill({
        status: 500,
        body: JSON.stringify({ error: 'Server error' })
      });
    });

    await page.goto('/');

    // Fill in the form
    await page.locator('input[placeholder*="repository"]').fill('/test/repo');
    await page.locator('input[placeholder*="main"]').first().fill('main');
    await page.locator('input[placeholder*="feature"]').fill('feature/test');

    // Click Compare button
    await page.locator('button:has-text("Compare")').click();

    // Wait for and verify error message
    await expect(page.locator('.mat-mdc-snack-bar-container')).toBeVisible();
  });
});
