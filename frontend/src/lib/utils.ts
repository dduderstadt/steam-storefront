/**
 * Converts Steam API playtime (minutes) to a human-readable string.
 * Shows minutes for durations under one hour, hours otherwise.
 */
export function formatPlaytime(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    if (hours < 1) {
        return `${minutes}m`;
    }
    return `${hours.toLocaleString()}h`;
}

/**
 * Sorts a playtime-by-genre map by playtime descending and returns the top N entries.
 * Used to drive the genre bar chart on the stats page.
 */
export function getTopGenres(
    playtimeByGenre: Record<string, number>,
    limit = 8,
): [string, number][] {
    return Object.entries(playtimeByGenre)
        .sort(([, a], [, b]) => b - a)
        .slice(0, limit);
}
