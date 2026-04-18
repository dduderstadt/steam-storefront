'use client';

import { useRouter, usePathname, useSearchParams } from 'next/navigation';

const SORT_OPTIONS = [
    { value: 'playtime', label: 'Most Played' },
    { value: 'name', label: 'Name (A-Z)' },
    { value: 'lastPlayed', label: 'Recently Played' },
];

export default function FilterBar() {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();

    function updateParam(key: string, value: string) {
        const params = new URLSearchParams(searchParams.toString());
        if (value) {
            params.set(key, value);
        } else {
            params.delete(key);
        }
        params.delete('page');
        router.push(`${pathname}?${params.toString()}`);
    }

    return (
        <div className="flex flex-wrap gap-3 items-center">
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