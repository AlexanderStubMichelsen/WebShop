"use client";

import { useEffect, useRef, useState, useCallback } from "react";
import { useSearchParams } from "next/navigation";
import { useCart } from "@/context/CartContext";
import { getApiUrl } from "@/lib/config";
import Link from "next/link";

type SessionData = {
  id: string;
  amount_total: number;
  customer_email: string | null;
  payment_status: "paid" | "unpaid" | string;
};

export default function ReceiptContent() {
  const searchParams = useSearchParams();
  const sessionId = searchParams.get("session_id");
  const [session, setSession] = useState<SessionData | null>(null);
  const [emailSent, setEmailSent] = useState(false);
  const [sendingEmail, setSendingEmail] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const sentOnceRef = useRef(false);
  const clearedRef = useRef(false);
  const { clearCart } = useCart();

  const apiUrl = getApiUrl();

  // Load session data
  useEffect(() => {
    if (!sessionId) return;
    (async () => {
      try {
        console.log("Fetching session:", `${apiUrl}/api/payments/session/${sessionId}`);
        const res = await fetch(`${apiUrl}/api/payments/session/${sessionId}`, { cache: "no-store" });
        if (!res.ok) throw new Error(`Failed to load session (${res.status})`);
        const data = await res.json();
        console.log("Session data loaded:", data);
        setSession(data);
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : String(err);
        setErrorMsg(msg || "Failed to load session");
        console.error("Error loading session:", err);
      }
    })();
  }, [sessionId, apiUrl]);

  // Send order email
  const sendOrderEmail = useCallback(async () => {
    if (!session?.customer_email) {
      console.warn("No customer email — skipping sendOrderEmail");
      return;
    }
    setErrorMsg(null);
    setSendingEmail(true);
    try {
      console.log("Sending order confirmation to:", session.customer_email);
      const res = await fetch(`${apiUrl}/api/orders/send-confirmation`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sessionId: session.id,
          customerEmail: session.customer_email,
          amountTotal: session.amount_total,
        }),
      });

      const text = await res.text().catch(() => "");
      if (res.ok) {
        console.log("Email confirmation success:", text || res.status);
        setEmailSent(true);
      } else {
        console.error("Email confirmation failed:", res.status, text);
        setErrorMsg(`Failed to send email (${res.status}). ${text || ""}`);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      setErrorMsg(msg || "Failed to send email");
      console.error("Error sending email:", err);
    } finally {
      setSendingEmail(false);
    }
  }, [apiUrl, session?.id, session?.customer_email, session?.amount_total]);

  // Clear cart after payment
  useEffect(() => {
    if (session?.payment_status === "paid" && !clearedRef.current) {
      clearCart();
      clearedRef.current = true;
    }
  }, [session?.payment_status, clearCart]);

  // Auto-send email after payment
  useEffect(() => {
    if (!session || sentOnceRef.current) return;
    console.log("Checking auto-send conditions:", session);
    if (session.payment_status === "paid" && session.customer_email) {
      sentOnceRef.current = true;
      void sendOrderEmail();
    } else {
      console.log("Auto-send skipped:", {
        payment_status: session.payment_status,
        customer_email: session.customer_email,
      });
    }
  }, [session, sendOrderEmail]);

  if (!session) return <p>Loading receipt...</p>;

  return (
    <div className="max-w-xl mx-auto mt-12 text-center">
      <h1 className="text-3xl font-bold text-green-700 mb-4">✅ Payment Successful</h1>
      <p className="mb-2">Thank you for your purchase!</p>

      <div className="bg-gray-100 p-4 rounded mt-4 text-left">
        <p><strong>Session ID:</strong> {session.id}</p>
        <p><strong>Customer Email:</strong> {session.customer_email || "Not provided"}</p>
        <p><strong>Amount Total:</strong> {(session.amount_total / 100).toFixed(2)} DKK</p>
        <p><strong>Status:</strong> {session.payment_status}</p>
      </div>

      {sendingEmail && <p className="mt-3">Sending confirmation email…</p>}
      {errorMsg && <p className="mt-3 text-red-600">{errorMsg}</p>}
      {emailSent && <p className="mt-4 text-green-600">✅ Confirmation email sent!</p>}
      {!sendingEmail && !emailSent && session.customer_email && (
        <p className="mt-3">A confirmation email will be sent to {session.customer_email}.</p>
      )}

      {session.customer_email && !emailSent && (
        <button
          onClick={sendOrderEmail}
          disabled={sendingEmail}
          className="mt-4 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {sendingEmail ? "Sending..." : "Send Order Confirmation Email"}
        </button>
      )}

      <Link href="/" className="text-blue-500 hover:underline block mt-6">Back to shop</Link>
    </div>
  );
}
