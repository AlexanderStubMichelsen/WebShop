'use client';

import React from 'react';
import { Product } from '@/types/Product';

type Props = {
  products: Product[];
};

const CheckoutButton: React.FC<Props> = ({ products }) => {
  const handleCheckout = async () => {
    const res = await fetch('http://localhost:5195/api/payments/create-checkout-session', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(products),
    });

    const data = await res.json();

    if (data.url) {
      window.location.href = data.url;
    } else {
      alert("Failed to create checkout session.");
    }
  };

  return (
    <button
      onClick={handleCheckout}
      className="mt-6 px-6 py-3 bg-green-600 text-white font-semibold rounded hover:bg-green-700 transition"
    >
      Pay with Card
    </button>
  );
};

export default CheckoutButton;
