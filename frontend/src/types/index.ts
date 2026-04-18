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
    playtimeForever: number;
}

export interface StatsDto {
    totalGames: number;
    totalPlaytimeMinutes: number;
    mostPlayedGames: GamePlaytimeStat[];
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