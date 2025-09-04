"use client";

import { useCart } from "@/context/CartContext";
import Link from "next/link";
import CheckoutButton from "@/components/CheckoutButton";
import Image from "next/image";

export default function CartPage() {
  const { cart, removeFromCart, increaseQuantity, decreaseQuantity } = useCart();
  const total = cart.reduce((acc, item) => acc + item.price * item.quantity, 0);

  // Video background (always rendered)
  // Place this outside your main content
  // Or at the top of your return statement
  return (
    <div className="relative min-h-screen">
      {/* Video background */}
      <video
        className="fixed inset-0 w-full h-full object-cover z-0"
        src="/153450-805374052_small-ezgif.com-reverse-video.mp4"
        autoPlay
        loop
        muted
        playsInline
      />

      {/* Overlay for readability (optional) */}
      <div className="fixed inset-0 z-10 pointer-events-none" />

      {/* Main content */}
      <div className="relative z-20 space-y-10 mt-10">
        {cart.length === 0 ? (
          <div className="text-center mt-20">
            <h2 className="text-2xl font-semibold mb-4 text-blue-800">Your cart is empty 🛒</h2>
            <Link
              href="/"
              className="inline-block mt-4 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition"
            >
              Go shopping
            </Link>
          </div>
        ) : (
          <>
            <h2 className="text-3xl font-bold text-blue-800">Your Cart</h2>
            <ul className="space-y-6">
              {cart.map((item) => (
                <li
                  key={item.id}
                  className="flex items-center justify-between border-1 p-4 rounded shadow"
                >
                  <div className="flex items-center space-x-4">
                    <Image
                      src={item.imageUrl || '/fallback.png'}
                      alt={item.name}
                      width={80}
                      height={80}
                      className="w-20 h-20 object-cover rounded"
                    />
                    <div className="text-blue-700">
                      <h3 className="text-lg font-semibold">{item.name}</h3>
                      <p className="text-sm">{item.description}</p>
                      <div className="text-sm font-medium">
                        {(item.price).toFixed(2)} DKK × {item.quantity} = {(item.price * item.quantity).toFixed(2)} DKK
                      </div>
                      <div className="flex items-center gap-2 mt-2">
                        <button
                          onClick={() => decreaseQuantity(item.id)}
                          className="w-8 h-8 rounded-full bg-gray-200 text-blue-700 hover:bg-gray-300"
                        >
                          −
                        </button>
                        <span className="text-lg font-medium">{item.quantity}</span>
                        <button
                          onClick={() => increaseQuantity(item.id)}
                          className="w-8 h-8 rounded-full bg-gray-200 text-blue-700 hover:bg-gray-300"
                        >
                          +
                        </button>
                      </div>
                    </div>
                  </div>
                  <button
                    onClick={() => removeFromCart(item.id)}
                    className="text-blue-600 text-sm font-semibold hover:underline"
                  >
                    Remove
                  </button>
                </li>
              ))}
            </ul>
            <div className="text-right text-xl font-bold text-blue-800">
              Total: {total.toFixed(2)} DKK
            </div>
            <div className="text-right">
              <CheckoutButton products={cart} />
            </div>
          </>
        )}
      </div>
    </div>
  );
}
