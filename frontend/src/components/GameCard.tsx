import Image from "next/image";
import Link from "next/link";
import type { GameDto } from "@/types";

function formatPlaytime(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    if (hours < 1) {
        return `${minutes}m`;
    }
    return `${hours.toLocaleString()}h`;
}

export default function GameCard({ game }: { game: GameDto }) {
    return (
        <Link href={`/game/${game.appId}`} className="group block rounded-lg overflow-hidden border border-zinc-200 dark:border-zinc-800 hover:border-zinc-400     
  dark:hover:border-zinc-600 transition-colors">
            <div className="relative aspect-[460/215] bg-zinc-100 dark:bg-zinc-900">
                {game.headerImageUrl ? (
                    <Image
                        src={game.headerImageUrl}
                        alt={game.name}
                        fill
                        className="object-cover"
                        sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
                    />
                ) : (
                    <div className="absolute inset-0 flex items-center justify-center text-zinc-400 text-sm">
                        No image
                    </div>
                )}
            </div>
            <div className="p-3">
                <h2 className="font-semibold text-sm truncate text-zinc-900 dark:text-zinc-100 group-hover:text-blue-600 dark:group-hover:text-blue-400">
                    {game.name}
                </h2>
                <p className="text-xs text-zinc-500 mt-1">
                    {formatPlaytime(game.playtimeForever)} played
                </p>
                <div className="flex flex-wrap gap-1 mt-2">
                    {game.genres.slice(0, 3).map((genre) => (
                        <span key={genre} className="text-xs px-1.5 py-0.5 rounded bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400">
                            {genre}
                        </span>
                    ))}
                </div>
            </div>
        </Link>
    );
}