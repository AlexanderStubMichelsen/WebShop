// src/app/layout.tsx
import "./globals.css";
import Link from "next/link";
import { CartProvider } from '@/context/CartContext';

export const metadata = {
  title: "Webshop",
  description: "My webshop built with Next.js and TypeScript",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className="bg-gray-100 text-gray-900">
        <CartProvider>
          <nav className="sticky top-0 z-50 bg-[#dabe4f] border-b shadow-sm px-6 py-4 flex justify-between items-center">
            <div className="text-2xl font-extrabold tracking-tight text-blue-600 hover:text-blue-700 transition-colors duration-200">
              <Link href="/">🛍️ MyShop</Link>
            </div>
            <div className="flex space-x-6 text-sm font-medium text-gray-700">
              <Link
                href="/"
                className="hover:text-blue-500 transition-colors duration-200"
              >
                Shop
              </Link>
              <Link
                href="/cart"
                className="hover:text-blue-500 transition-colors duration-200 flex items-center gap-1"
              >
                🛒<span>Cart</span>
              </Link>
            </div>
          </nav>

          <main className="max-w-6xl mx-auto p-4">{children}</main>
        </CartProvider>
      </body>
    </html>
  );
}
