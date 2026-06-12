import { describe, it, expect } from 'vitest';
import { formatPlaytime, getTopGenres } from '../utils';

describe('formatPlaytime', () => {
    it('shows minutes when under one hour', () => {
        expect(formatPlaytime(0)).toBe('0m');
        expect(formatPlaytime(45)).toBe('45m');
        expect(formatPlaytime(59)).toBe('59m');
    });

    it('shows hours when at or above one hour', () => {
        expect(formatPlaytime(60)).toBe('1h');
        expect(formatPlaytime(120)).toBe('2h');
    });

    it('floors partial hours', () => {
        expect(formatPlaytime(61)).toBe('1h');
        expect(formatPlaytime(119)).toBe('1h');
    });
});

describe('getTopGenres', () => {
    it('returns empty array for empty input', () => {
        expect(getTopGenres({})).toEqual([]);
    });

    it('sorts by playtime descending', () => {
        const result = getTopGenres({ Action: 100, RPG: 300, Indie: 200 });
        expect(result).toEqual([['RPG', 300], ['Indie', 200], ['Action', 100]]);
    });

    it('limits to 8 by default', () => {
        const input = Object.fromEntries(
            Array.from({ length: 10 }, (_, i) => [`Genre${i}`, i + 1])
        );
        expect(getTopGenres(input)).toHaveLength(8);
    });

    it('returns the top genres when over the limit', () => {
        const input = Object.fromEntries(
            Array.from({ length: 10 }, (_, i) => [`Genre${i}`, i + 1])
        );
        const result = getTopGenres(input);
        expect(result[0][1]).toBe(10);
        expect(result[7][1]).toBe(3);
    });

    it('respects a custom limit', () => {
        const input = { A: 3, B: 1, C: 2 };
        expect(getTopGenres(input, 2)).toEqual([['A', 3], ['C', 2]]);
    });
});
