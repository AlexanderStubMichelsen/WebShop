import type { NextConfig } from "next";

const isExport = process.env.NEXT_OUTPUT === "export";

const nextConfig: NextConfig = {
  ...(isExport
    ? {
        output: "export",
        images: { unoptimized: true }, // required for static export
      }
    : {
        // normal dev/prod server; API routes work
      }),
  images: {
    domains: ["via.placeholder.com"], // add any domains you use
  },
};

export default nextConfig;
