import { test, expect } from '@playwright/test';

test.describe('Cart Page', () => {
  test('shows empty cart message', async ({ page }) => {
    await page.goto('/cart');
    await expect(page.getByRole('heading', { name: /your cart is empty/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /go shopping/i })).toBeVisible();
    });
  });