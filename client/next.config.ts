import type { NextConfig } from "next";

const isExport = process.env.NEXT_OUTPUT === "export";

const nextConfig: NextConfig = {
  ...(isExport
    ? {
        output: "export",
        images: {
          unoptimized: true,
          domains: ["via.placeholder.com", "picsum.photos"],
        },
      }
    : {
        images: {
          domains: ["via.placeholder.com", "picsum.photos"],
        },
      }),
};

export default nextConfig;
