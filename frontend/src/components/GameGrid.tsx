import type { GameDto } from '@/types'; import GameCard from './GameCard';

export default function GameGrid({ games }: { games: GameDto[] }) {
    if (games.length === 0) {
        return (
            <div className="flex items-center justify-center py-24 text-zinc-400">
                No games found.
            </div>
        );
    }

    return (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {games.map((game) => (
                <GameCard key={game.appId} game={game} />
            ))}
        </div>
    );
}