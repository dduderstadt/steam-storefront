import GameCardSkeleton from '@/components/GameCardSkeleton';

/**
 * Top-level loading UI shown by Next.js while the storefront page is fetching data server-side.
 * Next.js automatically renders this file while the nearest async page/layout is suspended.
 * Renders a grid of skeleton cards so the layout matches what will appear once data loads.
 */
export default function Loading() {
    return (
        <main className="max-w-7xl mx-auto px-4 py-8">
            <div className="flex items-center justify-between mb-6">
                {/* Page title placeholder */}
                <div className="h-8 w-32 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse" />
                {/* Game count placeholder */}
                <div className="h-4 w-20 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse" />
            </div>
            {/* Filter bar placeholder */}
            <div className="flex gap-3 mb-6">
                <div className="h-9 w-48 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse" />
                <div className="h-9 w-44 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse" />
                <div className="h-9 w-36 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse" />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                {Array.from({ length: 24 }).map((_, i) => (
                    <GameCardSkeleton key={i} />
                ))}
            </div>
        </main>
    );
}
