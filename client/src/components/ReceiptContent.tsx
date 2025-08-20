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
  const sessionId = searchParams ? searchParams.get("session_id") : null;
  const [session, setSession] = useState<SessionData | null>(null);
  const [emailSent, setEmailSent] = useState(false);
  const [sendingEmail, setSendingEmail] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [serverText, setServerText] = useState<string>("");
  const [hydrated, setHydrated] = useState(false); // prevent SSR flicker of the button

  const sentOnceRef = useRef(false);
  const clearedRef = useRef(false);
  const { clearCart } = useCart();

  const apiUrl = getApiUrl();

  useEffect(() => {
    setHydrated(true);
  }, []);

  // Load session data
  useEffect(() => {
    if (!sessionId) return;
    (async () => {
      try {
        const res = await fetch(`${apiUrl}/api/payments/session/${sessionId}`, {
          cache: "no-store",
        });
        if (!res.ok) throw new Error(`Failed to load session (${res.status})`);
        const data = (await res.json()) as SessionData;
        setSession(data);
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : String(err);
        setErrorMsg(msg || "Failed to load session");
      }
    })();
  }, [sessionId, apiUrl]);

  // Send order email
  const sendOrderEmail = useCallback(async () => {
    if (!session?.id) {
      setErrorMsg("No session loaded");
      return;
    }
    if (!session.customer_email) {
      setErrorMsg("No customer email on the session");
      return;
    }

    setErrorMsg(null);
    setServerText("");
    setSendingEmail(true);

    try {
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
      setServerText(text);

      if (res.ok) {
        setEmailSent(true);              // ✅ ensures button hides
      } else {
        setErrorMsg(`Failed to send email (${res.status}). ${text || ""}`);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      setErrorMsg(msg || "Failed to send email");
    } finally {
      setSendingEmail(false);
    }
  }, [apiUrl, session?.id, session?.customer_email, session?.amount_total]);

  // Clear cart after payment (once)
  useEffect(() => {
    if (session?.payment_status === "paid" && !clearedRef.current) {
      clearCart();
      clearedRef.current = true;
    }
  }, [session?.payment_status, clearCart]);

  // Auto-send email after payment (once)
  useEffect(() => {
    if (!session || sentOnceRef.current) return;
    if (session.payment_status === "paid" && session.customer_email) {
      sentOnceRef.current = true;
      void sendOrderEmail();
    }
  }, [session, sendOrderEmail]);

  if (!session) return <p>Loading receipt…</p>;

  const showButton =
    hydrated && !!session.customer_email && !emailSent && !sendingEmail;

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

      {/* Status messages */}
      {sendingEmail && <p className="mt-3">Sending confirmation email…</p>}
      {errorMsg && <p className="mt-3 text-red-600">{errorMsg}</p>}
      {emailSent && <p className="mt-4 text-green-600">✅ Confirmation email sent!

      <br />
      <br />

      Please check your spam folder </p>}

      {!sendingEmail && !emailSent && session.customer_email && (
        <p className="mt-3">A confirmation email will be sent to {session.customer_email}.</p>
      )}
      {serverText && !emailSent && (
        <pre className="mt-2 p-2 text-xs bg-gray-50 rounded text-left whitespace-pre-wrap">
          {serverText}
        </pre>
      )}

      {/* Actions */}
      <div className="mt-4 flex items-center justify-center gap-3 min-h-[42px]">
        {showButton ? (
          <button
            onClick={sendOrderEmail}
            disabled={sendingEmail}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
          >
            Send Order Confirmation Email <br />
          </button>
        ) : null}
        <Link href="/" className="text-blue-500 hover:underline">
          Back to shop
        </Link>
      </div>
    </div>
  );
}
