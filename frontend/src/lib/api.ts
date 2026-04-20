/**
 * We have two URL possibilities defined.
 * We check if `window` is undefined to determine if we're running in a server environment (like during SSR) or in the browser.
 * The two environment variables(API_BASE_URL and NEXT_PUBLIC_API_BASE_URL) exist because Docker containers can't reach `localhost`,
 * they use internal hostnames.
 */

import type { GameDto, PagedResult, StatsDto, LibraryQuery } from "@/types";

/**
 * Server-side rendering uses API_BASE_URL (the internal Docker hostname http://backend:8080),
 * while client-side rendering (browser) uses NEXT_PUBLIC_API_BASE_URL (the public-facing http://localhost:5000).
 * Both URLs fall back to localhost:5000 for local dev outside Docker.
 */
const BASE_URL = typeof window === 'undefined'
    ? (process.env.API_BASE_URL || 'http://localhost:5000')
    : (process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5000');

/**
 * This function is async because fetch returns a Promise.
 * It doesn't give you the data immediately, rather a promise that the data will arrive eventually.
 * `await` keyword pauses execution until that promise resolves.
 * cache: `no-store` disables Next.js's automatic fetch caching so the storefront always gets fresh data on every request.
 * @param query the search parameters to filter the game library by. All fields are optional, any combination is valid (including no filters).
 * @returns The paged result of games as JSON.
 */
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

/**
 * This function is async because fetch returns a Promise.
 * It doesn't give you the data immediately, rather a promise that the data will arrive eventually.
 * `await` keyword pauses execution until that promise resolves.
 * cache: `no-store` disables Next.js's automatic fetch caching so the storefront always gets fresh data on every request.
 * @param appId The application ID of the game to fetch.
 * @returns The game data as JSON, or null if not found.
 */
export async function getGame(appId: number): Promise<GameDto | null> {
    const res = await fetch(`${BASE_URL}/api/v1/library/${appId}`, { cache: 'no-store' });
    if (res.status === 404) {
        return null;
    }
    if (!res.ok) {
        throw new Error('Failed to fetch game');
    }
    /**
     * `res.json()` is async - it reads and parses the response body stream, which takes time.
     * */
    return res.json();
}

/**
 * Fetches the stats snapshot from the backend.
 * This function is async because fetch returns a Promise.
 * It doesn't give you the data immediately, rather a promise that the data will arrive eventually.
 * `await` keyword pauses execution until that promise resolves.
 * cache: `no-store` disables Next.js's automatic fetch caching so the storefront always gets fresh data on every request.
 * @returns The stats snapshot as JSON.
 */
export async function getStats(): Promise<StatsDto> {
    const res = await fetch(`${BASE_URL}/api/v1/stats`, { cache: 'no-store' });
    if (!res.ok) {
        throw new Error('Failed to fetch stats');
    }
    return res.json();
}