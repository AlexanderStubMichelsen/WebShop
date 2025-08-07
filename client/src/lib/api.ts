// client/src/lib/api.ts

export async function getProducts() {
  const res = await fetch("https://localhost:5001/api/products", {
    next: { revalidate: 0 },
  });
  if (!res.ok) throw new Error("Failed to fetch products");
  return res.json();
}
