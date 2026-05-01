'use client';

/**
 * Next.js App Router error boundary. Must be a Client Component ('use client') because
 * it receives the error object and a reset function as props at runtime.
 * Catches unhandled errors thrown during rendering or data fetching in any page under this layout.
 * The reset() function re-renders the segment — useful for transient failures like network blips.
 */
export default function Error({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <main className="max-w-4xl mx-auto px-4 py-24 text-center">
            <p className="text-6xl font-bold text-zinc-200 dark:text-zinc-800 mb-4">!</p>
            <h1 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-100 mb-2">
                Something went wrong
            </h1>
            <p className="text-zinc-500 mb-8">
                {error.message ?? 'An unexpected error occurred.'}
            </p>
            <button
                onClick={reset}
                className="text-sm px-4 py-2 rounded border border-zinc-300 dark:border-zinc-700 hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors"
            >
                Try again
            </button>
        </main>
    );
}
