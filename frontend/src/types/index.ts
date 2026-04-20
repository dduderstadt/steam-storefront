/**
 * Why a dedicated types file - single source of truth for the API contract between frontend and backend.
 * If the backend DTO changes, you update one file and TypeScript flags every broken consumer. */

/** Data transfer object for a game
 * headerImageUrl, lastPlayed, and shortDescription are nullable beccause not every game has been played,
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

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
}

export interface GamePlaytimeStat {
    appId: number;
    name: string;
    playtimeMinutes: number;
}

export interface StatsDto {
    totalGames: number;
    totalPlaytimeMinutes: number;
    topGames: GamePlaytimeStat[];
    playtimeByGenre: Record<string, number>;
    lastSyncedAt: string;
}

export interface LibraryQuery {
    genre?: string;
    minPlaytime?: number;
    sort?: 'name' | 'playtime' | 'lastPlayed';
    page?: number;
    pageSize?: number;
}