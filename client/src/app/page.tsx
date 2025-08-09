'use client';

import { useEffect, useState } from 'react';
import { useCart } from '@/context/CartContext';
import { Product } from '@/lib/products';

export default function HomePage() {
  const [products, setProducts] = useState<Product[]>([]);
  const { addToCart } = useCart();

  useEffect(() => {
    const apiUrl = (typeof process !== 'undefined' && process.env.NEXT_PUBLIC_API_URL) ? process.env.NEXT_PUBLIC_API_URL : 'http://localhost:5195';
    fetch(`${apiUrl}/api/products`)
      .then((res) => res.json())
      .then(setProducts)
      .catch((err) => console.error('Failed to fetch products:', err));
  }, []);

  return (
    <div className="space-y-20">
      {/* Hero Section */}
      <section className="text-center py-20 bg-gradient-to-br from-blue-50 to-white rounded-md shadow-md">
        <h1 className="text-4xl sm:text-5xl font-extrabold tracking-tight text-blue-800 mb-4">
          Welcome to MyShop
        </h1>
        <p className="text-gray-600 text-lg max-w-xl mx-auto mb-8">
          Discover our latest collection of high-quality, stylish products
          designed just for you.
        </p>
        <a
          href="#products"
          className="inline-block px-6 py-3 bg-blue-600 text-white text-sm font-semibold rounded shadow hover:bg-blue-700 transition"
        >
          Start Shopping
        </a>
      </section>

      {/* Featured Products */}
      <section id="products" className="max-w-6xl mx-auto px-4">
        <h2 className="text-2xl font-semibold mb-6">Featured Products</h2>
        <div className="grid gap-6 grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
          {products.map((product) => (
            <div
              key={product.id}
              className="bg-white border rounded-lg p-4 shadow-sm hover:shadow-md transition flex flex-col"
            >
              <img
                src={product.imageUrl}
                alt={product.name}
                className="h-40 w-full object-cover rounded mb-4"
              />
              <h3 className="font-semibold text-gray-800 mb-1">
                {product.name}
              </h3>
              <p className="text-sm text-gray-500 mb-2">
                {product.description}
              </p>
              <span className="text-blue-600 font-bold mb-3">
                ${product.price.toFixed(2)}
              </span>
              <button
                onClick={() => addToCart(product)}
                className="mt-auto px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded hover:bg-blue-700 transition"
              >
                Add to Cart
              </button>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
