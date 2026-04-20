/**
 * Why a dedicated types file - single source of truth for the API contract between frontend and backend.
 * If the backend DTO changes, you update one file and TypeScript flags every broken consumer.
 */

/** Data transfer object for a game
 * headerImageUrl, lastPlayed, and shortDescription are nullable because not every game has been played,
 * has an image, or has a description in Steam's API.
*/
export interface GameDto {
    appId: number;
    name: string;
    shortDescription: string | null;
    headerImageUrl: string | null;
    genres: string[];
    playtimeForever: number;
    playtimeTwoWeeks: number;
    lastPlayed: string | null;
}

/**
 * Object type for a paged result.
 * 
 * Generic wrapper the backend sends for any paginated list. The frontend uses totalCount and pageSize to calculate
 * how many pages exist without the backend sending every record.
 */
export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
}

/**
 * Object type that is a slim projection used only inside the stats snapshot.
 * 
 * It omits images, genres, and descriptions because the stats dashboard only needs a name and a number
 * to render the bar chart.
 */
export interface GamePlaytimeStat {
    appId: number;
    name: string;
    playtimeMinutes: number;
}

/**
 * Object type for the full stats snapshot.
 * 
 * playtimeByGenre is a Record<string, number> because the set of genres isn't fixed - it's whatever genres
 * exist in the user's library.
 * 
 * lastSyncedAt is an ISO string (not Date) because JSON has no date type; parse it in the component.
 */
export interface StatsDto {
    totalGames: number;
    totalPlaytimeMinutes: number;
    topGames: GamePlaytimeStat[];
    playtimeByGenre: Record<string, number>;
    lastSyncedAt: string;
}

/**
 * Object type that mirrors the query parameters the storefront page reads from the URL
 * and passes to getGames(). All fields are optional because any combination is valid (no filters = return all games).
 */
export interface LibraryQuery {
    genre?: string;
    minPlaytime?: number;
    sort?: 'name' | 'playtime' | 'lastPlayed';
    page?: number;
    pageSize?: number;
}