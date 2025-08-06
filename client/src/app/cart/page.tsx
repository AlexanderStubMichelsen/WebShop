'use client';

import { useCart } from '@/context/CartContext';
import Link from 'next/link';

export default function CartPage() {
  const { cart, removeFromCart, increaseQuantity, decreaseQuantity } = useCart();

  const total = cart.reduce(
    (acc, item) => acc + item.price * item.quantity,
    0
  );

  if (cart.length === 0) {
    return (
      <div className="text-center mt-20">
        <h2 className="text-2xl font-semibold mb-4">Your cart is empty 🛒</h2>
        <Link
          href="/"
          className="inline-block mt-4 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition"
        >
          Go shopping
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-10 mt-10">
      <h2 className="text-3xl font-bold">Your Cart</h2>

      <ul className="space-y-6">
        {cart.map((item) => (
          <li
            key={item.id}
            className="flex items-center justify-between bg-white p-4 rounded shadow"
          >
            <div className="flex items-center space-x-4">
              <img
                src={item.imageUrl}
                alt={item.name}
                className="w-20 h-20 object-cover rounded"
              />
              <div>
                <h3 className="text-lg font-semibold">{item.name}</h3>
                <p className="text-sm text-gray-600">{item.description}</p>
                <div className="text-sm font-medium text-blue-700">
                  ${item.price.toFixed(2)} x {item.quantity} = $
                  {(item.price * item.quantity).toFixed(2)}
                </div>

                {/* Quantity controls */}
                <div className="flex items-center gap-2 mt-2">
                  <button
                    onClick={() => decreaseQuantity(item.id)}
                    className="w-8 h-8 rounded-full bg-gray-200 text-gray-700 hover:bg-gray-300"
                  >
                    −
                  </button>
                  <span className="text-lg font-medium">{item.quantity}</span>
                  <button
                    onClick={() => increaseQuantity(item.id)}
                    className="w-8 h-8 rounded-full bg-gray-200 text-gray-700 hover:bg-gray-300"
                  >
                    +
                  </button>
                </div>
              </div>
            </div>

            <button
              onClick={() => removeFromCart(item.id)}
              className="text-red-600 text-sm font-semibold hover:underline"
            >
              Remove
            </button>
          </li>
        ))}
      </ul>

      <div className="text-right text-xl font-bold text-blue-800">
        Total: ${total.toFixed(2)}
      </div>
    </div>
  );
}
