import type { GameDto, PagedResult, StatsDto, LibraryQuery } from "@/types";

const BASE_URL = typeof window === 'undefined'
    ? (process.env.API_BASE_URL || 'http://localhost:5000')
    : (process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000');

export async function getLibrary(query: LibraryQuery = {}): Promise<PagedResult<GameDto>> {
    const params = new URLSearchParams();
    if (query.genre) {
        params.set('genre', query.genre);
    }
    if (query.minPlaytime !== undefined) {
        params.set('minPlaytime', String(query.minPlaytime));
    }
    if (query.sort) {
        params.set('sort', query.sort);
    }
    if (query.page !== undefined) {
        params.set('page', String(query.page));
    }
    if (query.pageSize !== undefined) {
        params.set('pageSize', String(query.pageSize));
    }

    const res = await fetch(`${BASE_URL}/api/v1/library?${params}`, { cache: 'no-store' });
    if (!res.ok) {
        throw new Error('Failed to fetch library');
    }
    return res.json();
}

/**This function is async because fetch returns a Promise.
 * It doesn't give you the data immediately, rather a promise that the data will arrive eventually.
 * `await` keyword pauses execution until that promise resolves.
 */
export async function getGame(appId: number): Promise<GameDto | null> {
    const res = await fetch(`${BASE_URL}/api/v1/library/${appId}`, { cache: 'no-store' });
    if (res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error('Failed to fetch game');
    }
    /**`res.json()` is async - it reads and parses the response body stream, which takes time. */
    return res.json();
}

export async function getStats(): Promise<StatsDto> {
    const res = await fetch(`${BASE_URL}/api/v1/stats`, { cache: 'no-store' });
    if (!res.ok) {
        throw new Error('Failed to fetch stats');
    }
    return res.json();
}