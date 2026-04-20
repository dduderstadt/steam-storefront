'use client';

import { useRouter, usePathname, useSearchParams } from 'next/navigation';

const SORT_OPTIONS = [
    { value: 'playtime', label: 'Most Played' },
    { value: 'name', label: 'Name (A-Z)' },
    { value: 'lastPlayed', label: 'Recently Played' },
];

/**
 * Filters aren't stored in React state (`useState`) - they live in the URL query string.
 * This means that filter state survives a page refresh, can be bookmarked or shared, and the SSR storefront page
 * can read the same params on the server without any special handling.
 * 
 * 'use client' directive at the top of the file is required for components that use hooks like `useRouter`, `useSearchParams`, `usePathname`, etc.
 * All hooks that depend on the browser environment. Server components cannot use these hooks.
 * @returns A JSX element representing the filter bar.
 */
export default function FilterBar() {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();

    /**
     * Reads the current query string, sets or deletes the changed key, resets `page` back to 1 (by deleting it) so
     * you don't land on page 3 of a newly filtered result, and pushes the new URL to the router.
     * @param key The query parameter key to update (e.g., 'genre', 'minPlaytime', 'sort').
     * @param value The new value for the query parameter. If empty, the parameter will be removed from the URL.
     */
    function updateParam(key: string, value: string) {
        const params = new URLSearchParams(searchParams.toString());
        if (value) {
            params.set(key, value);
        } else {
            params.delete(key);
        }
        // Reset page to 1 when filters change
        params.delete('page');
        router.push(`${pathname}?${params.toString()}`);
    }

    return (
        <div className="flex flex-wrap gap-3 items-center">
            {/* Genre and minPlaytime inputs use `defaultValue` (uncontrolled) which lets the user type freely without re-rendering on every keystroke */}
            <input
                type="text"
                placeholder="Filter by genre..."
                defaultValue={searchParams.get('genre') ?? ''}
                onChange={(e) => updateParam('genre', e.target.value)}
                className="border border-zinc-300 dark:border-zinc-700 rounded px-3 py-1.5 text-sm bg-white dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100      
  placeholder:text-zinc-400"
            />
            <input
                type="number"
                placeholder="Min hours played..."
                defaultValue={searchParams.get('minPlaytime') ?? ''}
                onChange={(e) => updateParam('minPlaytime', e.target.value)}
                min={0}
                className="border border-zinc-300 dark:border-zinc-700 rounded px-3 py-1.5 text-sm bg-white dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100      
  placeholder:text-zinc-400 w-44"
            />
            {/* Sort select uses value (controlled) because its options are finite and known (no user keystrokes, select from a predefined list) */}
            <select
                value={searchParams.get('sort') ?? ''}
                onChange={(e) => updateParam('sort', e.target.value)}
                className="border border-zinc-300 dark:border-zinc-700 rounded px-3 py-1.5 text-sm bg-white dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100"
            >
                <option value="">Sort by...</option>
                {SORT_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
            </select>
        </div>
    );
}