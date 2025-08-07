'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useCart } from '@/context/CartContext';
import Link from 'next/link';

type SessionData = {
  id: string;
  amount_total: number;
  customer_email: string | null;
  payment_status: string;
};

export default function ReceiptContent() {
  const searchParams = useSearchParams();
  const sessionId = searchParams.get('session_id');
  const [session, setSession] = useState<SessionData | null>(null);
  const { clearCart } = useCart();

  useEffect(() => {
    if (sessionId) {
      fetch(`https://webshop-api.devdisplay.online/api/payments/session/${sessionId}`)
        .then((res) => res.json())
        .then(setSession);
    }
  }, [sessionId]);

  useEffect(() => {
    if (session?.payment_status === 'paid') {
      clearCart();
    }
  }, [session, clearCart]);

  if (!session) return <p>Loading receipt...</p>;

  return (
    <div className="max-w-xl mx-auto mt-12 text-center">
      <h1 className="text-3xl font-bold text-green-700 mb-4">✅ Payment Successful</h1>
      <p className="mb-2">Thank you for your purchase!</p>
      <div className="bg-gray-100 p-4 rounded mt-4 text-left">
        <p><strong>Session ID:</strong> {session.id}</p>
        <p><strong>Customer Email:</strong> {session.customer_email || 'Not provided'}</p>
        <p><strong>Amount Total:</strong> {(session.amount_total / 100).toFixed(2)} DKK</p>
        <p><strong>Status:</strong> {session.payment_status}</p>
      </div>
      <Link href="/" className="text-blue-500 hover:underline block mt-6">Back to shop</Link>
    </div>
  );
}
