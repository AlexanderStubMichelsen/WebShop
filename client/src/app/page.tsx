// src/app/page.tsx
import Image from "next/image";
import Link from "next/link";

export const metadata = {
  title: "DevDisplay Shop",
  description: "Welcome to the shop",
};

export default function Home() {
  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <section className="grid gap-6 md:grid-cols-2 items-center">
        <div>
          <h1 className="text-4xl font-bold tracking-tight">Welcome to DevDisplay</h1>
          <p className="mt-3 text-lg text-gray-600">
            Browse our products and complete a secure checkout. Thanks for supporting us!
          </p>
          <div className="mt-6 flex gap-3">
            <Link
              href="/products"
              className="rounded bg-blue-600 px-5 py-2.5 text-white hover:bg-blue-700"
            >
              Shop now
            </Link>
            <Link
              href="/"
              className="rounded border px-5 py-2.5 hover:bg-gray-50"
            >
              Go to frontpage
            </Link>
          </div>
        </div>

        {/* Replace the src/width/height to match your actual image */}
        <div className="relative aspect-[16/9] w-full overflow-hidden rounded-xl shadow">
          <Image
            src="/hero.jpg"          // put the file in /public/hero.jpg
            alt="Featured products"
            fill                      // fills the parent (uses the aspect-ratio above)
            priority                  // improves LCP for above-the-fold image
            sizes="(max-width: 768px) 100vw, 50vw"
            className="object-cover"
          />
        </div>
      </section>

      <section className="mt-12">
        <h2 className="text-2xl font-semibold">Popular categories</h2>
        <ul className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <li className="rounded-lg border p-4 hover:shadow">
            <Link href="/products?category=new">New Arrivals →</Link>
          </li>
          <li className="rounded-lg border p-4 hover:shadow">
            <Link href="/products?category=best">Best Sellers →</Link>
          </li>
          <li className="rounded-lg border p-4 hover:shadow">
            <Link href="/products?category=sale">On Sale →</Link>
          </li>
        </ul>
      </section>
    </main>
  );
}
