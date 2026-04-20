import { Suspense } from 'react';
import { getLibrary } from '@/lib/api';
import GameGrid from '@/components/GameGrid';
import FilterBar from '@/components/FilterBar';
import Pagination from '@/components/Pagination';

const PAGE_SIZE = 24; // Default page size; controls how many games appear per page. Centralized here so it's easy to tune.

/**
 * Next.js statically caches SSR pages by default. Without this, the first render gets cached and future visits show stale
 * data even after a sync. `force-dynamic` tells Next.js to re-run the server function on every request.
 */
export const dynamic = 'force-dynamic';

/**
 * A Server Component that does SSR. It reads `searchParams` from the URL (which in Next.js 15+ is a Promise and must be awaited),
 * fetches the library data server-side, and renders the full page. No loading spinner - the HTML is complete before it reaches the browser.
 * @param searchParams The URL query parameters, passed in by Next.js. This includes filters (genre, minPlaytime, sort) and pagination (page).
 * @returns A JSX element representing the storefront page, including the filter bar, game grid, and pagination controls.
 */
export default async function StorefrontPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | undefined }>;
}) {
  const params = await searchParams;

  const page = Number(params.page ?? '1');
  const query = {
    genre: params.genre,
    minPlaytime: params.minPlaytime ? Number(params.minPlaytime) : undefined,
    sort: params.sort as 'name' | 'playtime' | 'lastPlayed' | undefined,
    page,
    pageSize: PAGE_SIZE,
  };

  const result = await getLibrary(query);

  return (
    <main className="max-w-7xl mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-zinc-900 dark:text-zinc-100">
          My Library
        </h1>
        <span className="text-sm text-zinc-500">
          {result.totalCount} games
        </span>
      </div>
      <div className="mb-6">
        {/* Required by FilterBar, Pagination, and  useSearchParams ('use client') when used inside a Server Component page */}
        <Suspense>
          <FilterBar />
        </Suspense>
      </div>
      <GameGrid games={result.items} />
      <Suspense>
        <Pagination
          totalCount={result.totalCount}
          page={result.page}
          pageSize={result.pageSize}
        />
      </Suspense>
    </main>
  );
}