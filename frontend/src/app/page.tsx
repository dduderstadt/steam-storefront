import { Suspense } from 'react'; import { getLibrary } from '@/lib/api';
import GameGrid from '@/components/GameGrid';
import FilterBar from '@/components/FilterBar';
import Pagination from '@/components/Pagination';

const PAGE_SIZE = 24;

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