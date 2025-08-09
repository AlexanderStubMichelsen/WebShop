// client/src/lib/api.ts

export async function getProducts() {
  const isProduction = typeof window !== 'undefined' && window.location.hostname === 'shop.devdisplay.online';
    const apiUrl = isProduction 
      ? 'https://webshop-api.devdisplay.online' 
      : 'http://localhost:5195';
      
  const res = await fetch(`${apiUrl}/api/products`, {
    next: { revalidate: 0 },
  });
  if (!res.ok) throw new Error("Failed to fetch products");
  return res.json();
}
