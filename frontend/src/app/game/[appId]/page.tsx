import { notFound } from 'next/navigation';
import Image from 'next/image';
import Link from 'next/link';
import { getGame } from '@/lib/api';

// TODO: extract to a shared utility (lib/utils.ts) - duplicated from GameCard.tsx
/**
 * Formats playtime in minutes into a human-readable string.
 * @param minutes The playtime in minutes.
 * @returns The formatted playtime string.
 */
function formatPlaytime(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    if (hours < 1) {
        return `${minutes}m`;
    }
    return `${hours.toLocaleString()}h`;
}

/**
 * SSR detail page for a single game. Fetches the game data server-side based on the `appId` from the URL, and renders a detailed view of the game.
 * If no game is found for the given `appId`, it calls `notFound()` to render the 404 page.
 * Priority on the Image preloads it since it's above the fold.
 * @param params Next.js route params as a Promise - must be awaited to extract the `appId`.
 * @returns A full-page JSX element with game details, or triggers a 404 if the game is not found.
 */
export default async function GameDetailPage({
    params,
}: {
    params: Promise<{ appId: string }>;
}) {
    const { appId } = await params;
    const game = await getGame(Number(appId));

    if (!game) {
        notFound(); // Renders the nearest not-found.tsx; falls back to Next.js default 404 if none exists.
    }

    return (
        <main className="max-w-4xl mx-auto px-4 py-8">
            <Link
                href="/"
                className="text-sm text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100 transition-colors mb-6 inline-block"
            >
                ← Back to library
            </Link>
            {game.headerImageUrl && (
                <div className="relative w-full aspect-[460/215] rounded-lg overflow-hidden mb-6">
                    <Image
                        src={game.headerImageUrl}
                        alt={game.name}
                        fill
                        className="object-cover"
                        priority
                    />
                </div>
            )}
            <h1 className="text-3xl font-bold text-zinc-900 dark:text-zinc-100 mb-2">
                {game.name}
            </h1>
            <div className="flex flex-wrap gap-2 mb-4">
                {game.genres.map((genre) => (
                    <span
                        key={genre}
                        className="text-sm px-2 py-1 rounded bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400"
                    >
                        {genre}
                    </span>
                ))}
            </div>
            <div className="flex gap-6 text-sm text-zinc-500 mb-6">
                <span>Total playtime: <strong className="text-zinc-900 dark:text-zinc-100">{formatPlaytime(game.playtimeForever)}</strong></span>
                <span>Last 2 weeks: <strong className="text-zinc-900 dark:text-zinc-100">{formatPlaytime(game.playtimeTwoWeeks)}</strong></span>
                {game.lastPlayed && (
                    <span>Last played: <strong className="text-zinc-900 dark:text-zinc-100">{new Date(game.lastPlayed).toLocaleDateString()}</strong></span>
                )}
            </div>
            {game.shortDescription && (
                <p className="text-zinc-600 dark:text-zinc-400 leading-relaxed">
                    {game.shortDescription}
                </p>
            )}
        </main>
    );
}