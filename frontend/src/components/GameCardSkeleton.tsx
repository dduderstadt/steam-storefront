/**
 * Placeholder card shown while the game grid is loading.
 * Matches the dimensions of GameCard so the layout doesn't shift when real data arrives.
 * The animate-pulse class uses a CSS opacity animation to indicate loading state.
 */
export default function GameCardSkeleton() {
    return (
        <div className="rounded-lg overflow-hidden border border-zinc-200 dark:border-zinc-800">
            {/* Header image area */}
            <div className="aspect-[460/215] bg-zinc-200 dark:bg-zinc-800 animate-pulse" />
            <div className="p-3 space-y-2">
                {/* Game name */}
                <div className="h-4 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse w-3/4" />
                {/* Playtime */}
                <div className="h-3 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse w-1/4" />
                {/* Genre badges */}
                <div className="flex gap-1 mt-2">
                    <div className="h-4 w-12 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse" />
                    <div className="h-4 w-10 bg-zinc-200 dark:bg-zinc-800 rounded animate-pulse" />
                </div>
            </div>
        </div>
    );
}
