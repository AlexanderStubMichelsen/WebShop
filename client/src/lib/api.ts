// client/src/lib/api.ts

export async function getProducts() {
  const apiUrl = (typeof process !== 'undefined' && process.env.NEXT_PUBLIC_API_URL) ? process.env.NEXT_PUBLIC_API_URL : 'http://localhost:5195';
  const res = await fetch(`${apiUrl}/api/products`, {
    next: { revalidate: 0 },
  });
  if (!res.ok) throw new Error("Failed to fetch products");
  return res.json();
}
