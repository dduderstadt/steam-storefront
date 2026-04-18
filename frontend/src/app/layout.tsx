import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Steam Library",
  description: "A personal Steam library storefront.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full
  antialiased`}
    >
      <body className="min-h-full flex flex-col">
        <nav className="border-b border-zinc-200 dark:border-zinc-800 px-4
  py-3">
          <div className="max-w-7xl mx-auto flex items-center gap-6">
            <Link
              href="/"
              className="font-semibold text-zinc-900 dark:text-zinc-100
   hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
            >
              My Library
            </Link>
            <Link
              href="/stats"
              className="text-sm text-zinc-500 hover:text-zinc-900
  dark:hover:text-zinc-100 transition-colors"
            >
              Stats
            </Link>
          </div>
        </nav>
        {children}
      </body>
    </html>
  );
}