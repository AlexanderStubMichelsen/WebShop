import { test, expect } from '@playwright/test';

test('homepage loads and displays products', async ({ page }) => {
  await page.route('**/api/products', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 1,
          name: 'Test Product',
          description: 'A test product',
          price: 99,
          imageUrl: 'https://via.placeholder.com/150'
        }
      ])
    });
  });

  await page.goto('/');
  await expect(page.getByRole('heading', { name: /featured products/i })).toBeVisible();

  const cards = page.locator('[data-testid="product-card"]');
  await expect(cards.first()).toBeVisible();
  await expect.poll(async () => cards.count()).toBeGreaterThan(0);

  // Check each product card for details
  const count = await cards.count();
  for (let i = 0; i < count; i++) {
    const card = cards.nth(i);

    // Product name
    await expect(card.locator('h3')).toBeVisible();

    // Product image
    await expect(card.locator('img')).toBeVisible();

    // Product price
    await expect(card.locator('span')).toHaveText(/^\$\d+(\.\d{2})?$/);

    // Add to Cart button
    await expect(card.getByRole('button', { name: /add to cart/i })).toBeVisible();
  }
});

// Optionally, test adding a product to the cart
test('can add a product to the cart', async ({ page }) => {
  await page.route('**/api/products', async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 1,
          name: 'Test Product',
          description: 'A test product',
          price: 99,
          imageUrl: 'https://via.placeholder.com/150'
        }
      ])
    });
  });

  await page.goto('/');
  const firstAddButton = page.getByRole('button', { name: /add to cart/i }).first();
  await firstAddButton.click();

  // If you have a cart indicator, check it updated (adjust selector as needed)
  // await expect(page.getByTestId('cart-count')).toHaveText(/1/);
});
