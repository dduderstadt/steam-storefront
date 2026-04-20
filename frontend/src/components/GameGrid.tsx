import type { GameDto } from '@/types';
import GameCard from './GameCard';

/**
 * Renders a grid of game cards. This is a pure layout component - it takes an array of games, renders them in a responsive CSS grid,
 * and handles the empty state. No state, no fetching.
 * @param games - An array of GameDto objects to display in the grid.
 * @returns A JSX element representing the game grid or "No games found." message if the array is empty.
 */
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
                // `key={game.appId}` - appId is a unique, stable ID from Steam, so it's perfect for React's key prop. It ensures efficient rendering
                // and helps React track items in the list.
                <GameCard key={game.appId} game={game} />
            ))}
        </div>
    );
}