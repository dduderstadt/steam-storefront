import Link from 'next/link';

/**
 * Rendered by Next.js when notFound() is called (e.g. game detail page for an unknown AppId)
 * or when no route matches the URL. Replaces the default Next.js 404 page.
 */
export default function NotFound() {
    return (
        <main className="max-w-4xl mx-auto px-4 py-24 text-center">
            <p className="text-6xl font-bold text-zinc-200 dark:text-zinc-800 mb-4">404</p>
            <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-100 mb-2">
                Page not found
            </h1>
            <p className="text-zinc-500 mb-8">
                The page you're looking for doesn't exist or has been removed.
            </p>
            <Link
                href="/"
                className="text-sm text-blue-600 dark:text-blue-400 hover:underline"
            >
                ← Back to library
            </Link>
        </main>
    );
}
