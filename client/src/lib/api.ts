const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5001";

export async function getProducts() {
  const res = await fetch(`${API_URL}/api/products`, {
    next: { revalidate: 0 },
  });
  if (!res.ok) throw new Error("Failed to fetch products");
  return res.json();
}
