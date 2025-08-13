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

type ReceiptDebug = {
  session: SessionData | null;
  emailSent: boolean;
  sendingEmail: boolean;
  errorMsg: string | null;
  serverText: string;
  autoSendNote: string;
  apiUrl: string;
  BUILD_TAG: string;
};

declare global {
  interface Window {
    __receipt_debug?: ReceiptDebug;
  }
}

export default function ReceiptContent() {
  const searchParams = useSearchParams();
  const sessionId = searchParams.get("session_id");
  const [session, setSession] = useState<SessionData | null>(null);
  const [emailSent, setEmailSent] = useState(false);
  const [sendingEmail, setSendingEmail] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [serverText, setServerText] = useState<string>("");
  const [autoSendNote, setAutoSendNote] = useState<string>("");
  const sentOnceRef = useRef(false);
  const clearedRef = useRef(false);
  const { clearCart } = useCart();

  const apiUrl = getApiUrl();
  const BUILD_TAG = "receipt-debug-v3";

  // Load session data
  useEffect(() => {
    if (!sessionId) return;
    (async () => {
      try {
        const url = `${apiUrl}/api/payments/session/${sessionId}`;
        console.log("[Receipt] Fetching session:", url);
        const res = await fetch(url, { cache: "no-store" });
        if (!res.ok) throw new Error(`Failed to load session (${res.status})`);
        const data = (await res.json()) as SessionData;
        console.log("[Receipt] Session data loaded:", data);
        setSession(data);
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : String(err);
        setErrorMsg(msg || "Failed to load session");
        console.error("[Receipt] Error loading session:", err);
      }
    })();
  }, [sessionId, apiUrl]);

  // Send order email
  const sendOrderEmail = useCallback(async () => {
    if (!session?.id) {
      setErrorMsg("No session loaded");
      console.warn("[Receipt] sendOrderEmail aborted: no session");
      return;
    }
    if (!session.customer_email) {
      setErrorMsg("No customer email on the session");
      console.warn("[Receipt] sendOrderEmail aborted: no customer email");
      return;
    }

    setErrorMsg(null);
    setServerText("");
    setSendingEmail(true);

    try {
      const url = `${apiUrl}/api/orders/send-confirmation`;
      console.log("[Receipt] POST", url, {
        sessionId: session.id,
        customerEmail: session.customer_email,
        amountTotal: session.amount_total,
      });

      const res = await fetch(url, {
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
        console.log(
          "[Receipt] Email confirmation success:",
          text || res.status
        );
        setEmailSent(true);
      } else {
        console.error("[Receipt] Email confirmation failed:", res.status, text);
        setErrorMsg(`Failed to send email (${res.status}). ${text || ""}`);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      setErrorMsg(msg || "Failed to send email");
      console.error("[Receipt] Error sending email:", err);
    } finally {
      setSendingEmail(false);
    }
  }, [apiUrl, session?.id, session?.customer_email, session?.amount_total]);

  // Clear cart after payment (once)
  useEffect(() => {
    if (session?.payment_status === "paid" && !clearedRef.current) {
      console.log("[Receipt] Clearing cart after paid session");
      clearCart();
      clearedRef.current = true;
    }
  }, [session?.payment_status, clearCart]);

  // Auto-send email after payment
  useEffect(() => {
    if (!session || sentOnceRef.current) return;
    console.log("[Receipt] Checking auto-send conditions:", session);

    if (session.payment_status === "paid" && session.customer_email) {
      sentOnceRef.current = true;
      setAutoSendNote("Auto-send triggered.");
      console.log("[Receipt] Auto-send triggered");
      void sendOrderEmail();
    } else {
      const why = `Auto-send skipped: payment_status=${
        session.payment_status
      }, customer_email=${String(session.customer_email)}`;
      setAutoSendNote(why);
      console.log("[Receipt]", why);
    }
  }, [session, sendOrderEmail]);

  // Expose debug object to the window (typed)
  useEffect(() => {
    if (typeof window !== "undefined") {
      window.__receipt_debug = {
        session,
        emailSent,
        sendingEmail,
        errorMsg,
        serverText,
        autoSendNote,
        apiUrl,
        BUILD_TAG,
      };
    }
  }, [
    session,
    emailSent,
    sendingEmail,
    errorMsg,
    serverText,
    autoSendNote,
    apiUrl,
  ]);

  if (!session)
    return (
      <p>
        Loading receipt… <small>({BUILD_TAG})</small>
      </p>
    );

  return (
    <div className="max-w-xl mx-auto mt-12 text-center">
      <h1 className="text-3xl font-bold text-green-700 mb-4">
        ✅ Payment Successful
      </h1>
      <p className="mb-2">
        Thank you for your purchase!{" "}
        <small className="text-gray-500">[{BUILD_TAG}]</small>
      </p>

      <div className="bg-gray-100 p-4 rounded mt-4 text-left">
        <p>
          <strong>Session ID:</strong> {session.id}
        </p>
        <p>
          <strong>Customer Email:</strong>{" "}
          {session.customer_email || "Not provided"}
        </p>
        <p>
          <strong>Amount Total:</strong>{" "}
          {(session.amount_total / 100).toFixed(2)} DKK
        </p>
        <p>
          <strong>Status:</strong> {session.payment_status}
        </p>
      </div>

      {/* Email status lines */}
      {sendingEmail && <p className="mt-3">Sending confirmation email…</p>}
      {errorMsg && <p className="mt-3 text-red-600">{errorMsg}</p>}
      {emailSent && (
        <p className="mt-4 text-green-600">✅ Confirmation email sent!</p>
      )}
      {!sendingEmail && !emailSent && session.customer_email && (
        <p className="mt-3">
          A confirmation email will be sent to {session.customer_email}.
        </p>
      )}
      {serverText && !emailSent && (
        <pre className="mt-2 p-2 text-xs bg-gray-50 rounded text-left whitespace-pre-wrap">
          {serverText}
        </pre>
      )}

      {/* Actions */}
      <div className="mt-4 flex items-center justify-center gap-3">
        <button
          onClick={sendOrderEmail}
          disabled={sendingEmail || !session.customer_email}
          className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {sendingEmail ? "Sending..." : "Send Order Confirmation Email"}
        </button>
        <Link href="/" className="text-blue-500 hover:underline">
          Back to shop
        </Link>
      </div>

      {/* Debug panel */}
      <details className="mt-6 text-left">
        <summary className="cursor-pointer font-semibold">Debug</summary>
        <div className="mt-2 text-sm">
          <p>
            <strong>apiUrl:</strong> {apiUrl}
          </p>
          <p>
            <strong>autoSend:</strong> {autoSendNote || "(none yet)"}
          </p>
          <p>
            <strong>emailSent:</strong> {String(emailSent)} |{" "}
            <strong>sendingEmail:</strong> {String(sendingEmail)}
          </p>
          <p>
            <strong>errorMsg:</strong> {errorMsg || "(none)"}
          </p>
          <p>
            <strong>serverText:</strong>{" "}
            {serverText ? "(see above)" : "(empty)"}{" "}
          </p>
          <pre className="mt-2 p-2 bg-gray-50 rounded whitespace-pre-wrap">
            {JSON.stringify({ session }, null, 2)}
          </pre>
        </div>
      </details>
    </div>
  );
}
