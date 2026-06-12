import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { getLibrary } from '../api';

const emptyPage = { items: [], totalCount: 0, page: 1, pageSize: 24 };

function mockFetch(ok = true, body: unknown = emptyPage) {
    return vi.fn().mockResolvedValue({
        ok,
        json: () => Promise.resolve(body),
    });
}

function calledUrl(fetchMock: ReturnType<typeof vi.fn>): URL {
    return new URL(fetchMock.mock.calls[0][0] as string);
}

describe('getLibrary', () => {
    let fetchMock: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        fetchMock = mockFetch();
        vi.stubGlobal('fetch', fetchMock);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('sends multiple genres as repeated query params', async () => {
        await getLibrary({ genres: ['Action', 'RPG'] });
        expect(calledUrl(fetchMock).searchParams.getAll('genre')).toEqual(['Action', 'RPG']);
    });

    it('omits the genre param when the array is empty', async () => {
        await getLibrary({ genres: [] });
        expect(calledUrl(fetchMock).searchParams.has('genre')).toBe(false);
    });

    it('omits optional params when not provided', async () => {
        await getLibrary({});
        const params = calledUrl(fetchMock).searchParams;
        expect(params.has('minPlaytime')).toBe(false);
        expect(params.has('sort')).toBe(false);
        expect(params.has('page')).toBe(false);
        expect(params.has('pageSize')).toBe(false);
    });

    it('serialises all provided params as strings', async () => {
        await getLibrary({ minPlaytime: 10, sort: 'playtime', page: 2, pageSize: 12 });
        const params = calledUrl(fetchMock).searchParams;
        expect(params.get('minPlaytime')).toBe('10');
        expect(params.get('sort')).toBe('playtime');
        expect(params.get('page')).toBe('2');
        expect(params.get('pageSize')).toBe('12');
    });

    it('throws when the response is not ok', async () => {
        vi.stubGlobal('fetch', mockFetch(false));
        await expect(getLibrary()).rejects.toThrow('Failed to fetch library');
    });
});
