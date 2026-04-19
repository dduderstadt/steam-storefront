import type { NextConfig } from "next";
const nextConfig: NextConfig = {
  output: 'standalone',
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: '*steamstatic.com',
      },
      {
        protocol: 'https',
        hostname: '*steamstatic.com',
      },
    ],
  },
};

export default nextConfig;