import type { NextConfig } from "next";

const isExport = process.env.NEXT_OUTPUT === "export";

const nextConfig: NextConfig = {
  output: "export",
  images: {
    domains: ["via.placeholder.com", "picsum.photos"],
    unoptimized: true,
  },
};

export default nextConfig;
