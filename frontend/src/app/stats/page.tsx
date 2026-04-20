'use client';
import { useEffect, useState } from 'react';
import { getStats } from '@/lib/api';
import type { StatsDto } from '@/types';

// TODO: extract to lib/utils.ts - duplicated from GameCard.tsx and game/[appId]/page.tsx
/**
 * Formats playtime in minutes into a human-readable string.
 * @param minutes The playtime in minutes.
 * @returns The formatted playtime string in hours - always.
 */
function formatPlaytime(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    return `${hours.toLocaleString()}h`;
}

/**
 * 'use client' makes this a CSR page (intentional to the SSR storefront).
 * Stats are data-heavy and don't need to be in the initial HTML - fetching client-side after load is fine.
 * Three render states: loading, error, and data.
 * @returns A JSX element representing the stats page, which shows library statistics and visualizations of playtime by genre and top games.
 */
export default function StatsPage() {
    const [stats, setStats] = useState<StatsDto | null>(null);
    const [error, setError] = useState<string | null>(null);


    // The empty dependency array means this runs once after the first render (on mount).
    // Without it, it would re-run on every render.
    useEffect(() => {
        getStats()
            .then(setStats)
            .catch(() => setError('Failed to load stats.'));
    }, []);

    if (error) {
        return (
            <main className="max-w-4xl mx-auto px-4 py-8">
                <p className="text-red-500">{error}</p>
            </main>
        );
    }

    if (!stats) {
        return (
            <main className="max-w-4xl mx-auto px-4 py-8">
                <p className="text-zinc-400">Loading stats...</p>
            </main>
        );
    }

    // topGenres sorts the genre map by playtime descending and takes the top 8.
    const topGenres = Object.entries(stats.playtimeByGenre)
        .sort(([, a], [, b]) => b - a)
        .slice(0, 8);

    // maxGenreMinutes is the largest value - used to calculate bar widths as a percentage so the top genre always fills 100% width
    // and others scale relative to it.
    const maxGenreMinutes = topGenres[0]?.[1] ?? 1;

    return (
        <main className="max-w-4xl mx-auto px-4 py-8">
            <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-100 mb-8">
                Library Stats
            </h1>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 mb-10">
                <div className="rounded-lg border border-zinc-200 dark:border-zinc-800 p-4">
                    <p className="text-sm text-zinc-500">Total Games</p>
                    <p className="text-3xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">
                        {stats.totalGames.toLocaleString()}
                    </p>
                </div>
                <div className="rounded-lg border border-zinc-200 dark:border-zinc-800 p-4">
                    <p className="text-sm text-zinc-500">Total Playtime</p>
                    <p className="text-3xl font-bold text-zinc-900 dark:text-zinc-100 mt-1">
                        {formatPlaytime(stats.totalPlaytimeMinutes)}
                    </p>
                </div>
                <div className="rounded-lg border border-zinc-200 dark:border-zinc-800 p-4">
                    <p className="text-sm text-zinc-500">Last Synced</p>
                    <p className="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mt-1">
                        {new Date(stats.lastSyncedAt).toLocaleString()}
                    </p>
                </div>
            </div>
            <section className="mb-10">
                <h2 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mb-4">
                    Most Played
                </h2>
                <ol className="space-y-2">
                    {stats.topGames.map((game, i) => (
                        <li key={game.appId} className="flex items-center gap-3 text-sm">
                            <span className="w-5 text-zinc-400 text-right">{i + 1}.</span>
                            <span className="flex-1 text-zinc-900 dark:text-zinc-100 truncate">{game.name}</span>
                            <span className="text-zinc-500">{formatPlaytime(game.playtimeMinutes)}</span>
                        </li>
                    ))}
                </ol>
            </section>
            <section>
                <h2 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mb-4">
                    Playtime by Genre
                </h2>
                <div className="space-y-3">
                    {topGenres.map(([genre, minutes]) => (
                        <div key={genre}>
                            <div className="flex justify-between text-sm mb-1">
                                <span className="text-zinc-700 dark:text-zinc-300">{genre}</span>
                                <span className="text-zinc-500">{formatPlaytime(minutes)}</span>
                            </div>
                            <div className="h-2 rounded-full bg-zinc-100 dark:bg-zinc-800">
                                {/* Width is a percentage of the top genre's playtime, so the longest bar always fills the full width */}
                                <div
                                    className="h-2 rounded-full bg-blue-500"
                                    style={{ width: `${(minutes / maxGenreMinutes) * 100}%` }}
                                />
                            </div>
                        </div>
                    ))}
                </div>
            </section>
            <p className="text-xs text-zinc-400 mt-10">
                Data as of {new Date(stats.lastSyncedAt).toLocaleString()}
            </p>
        </main>
    );
}