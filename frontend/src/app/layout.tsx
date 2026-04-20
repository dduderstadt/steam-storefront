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

// Next.js reads this object and injects the <title> and <meta name="description"> tags into the <head> automatically. No manual <head> needed.
export const metadata: Metadata = {
  title: "Steam Library",
  description: "A personal Steam library storefront.",
};

/**
 * RootLayout wraps every page in the app. The children prop is whatever page is currently active.
 * The nav bar here renders on every page without any duplication in individual page files.
 * @param children Whatever page is currently active - passed in by Next.js automatically. This is the main content of each page, rendered below the nav bar.
 * @returns The full HTML structure of the page, including the <html> and <body> tags, with a nav bar and the active page content. The fonts are loaded here and applied globally via CSS variables.
 */
export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    //Next.js loads these fonts from Google at build time and injects them as CSS custom properties (--font-geist-sans, --font-geist-mono)
    // The variables are then applied to the <html> element so Tailwind can use them via font-sans and font-mono.
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