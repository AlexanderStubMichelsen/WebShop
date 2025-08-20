// pages/api/products.ts
import type { NextApiRequest, NextApiResponse } from 'next';

const mock = [
  { id: 1, name: 'Mock Product 1', price: '19.99', imageUrl: 'https://picsum.photos/200' },
  { id: 2, name: 'Mock Product 2', price: '29.99', imageUrl: 'https://picsum.photos/200' },
];

export default function handler(req: NextApiRequest, res: NextApiResponse) {
  // Kun brug mock i E2E-mode
  if (process.env.NEXT_PUBLIC_E2E === '1') {
    return res.status(200).json(mock);
  }
  // Ellers proxy til din rigtige backend
  // Example: fetch fra process.env.NEXT_PUBLIC_API_URL
  const api = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5019';
  fetch(`${api}/api/products`)
    .then(r => r.json())
    .then(data => res.status(200).json(data))
    .catch(() => res.status(500).json({ error: 'Upstream error' }));
}
