import { Suspense } from 'react';
import ReceiptContent from '@/components/ReceiptContent';

export default function ReceiptPage() {
  return (
    <Suspense fallback={<p>Loading...</p>}>
      <ReceiptContent />
    </Suspense>
  );
}
