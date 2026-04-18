'use client';
import { useRouter, usePathname, useSearchParams } from 'next/navigation';

interface PaginationProps {
    totalCount: number;
    page: number;
    pageSize: number;
}

export default function Pagination({ totalCount, page, pageSize }: PaginationProps) {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();

    const totalPages = Math.ceil(totalCount / pageSize);

    if (totalPages <= 1) {
        return null;
    }

    function goToPage(newPage: number) {
        const params = new URLSearchParams(searchParams.toString());
        params.set('page', String(newPage));
        router.push(`${pathname}?${params.toString()}`);
    }

    return (
        <div className="flex items-center justify-center gap-4 py-6">
            <button
                onClick={() => goToPage(page - 1)}
                disabled={page <= 1}
                className="px-4 py-1.5 text-sm rounded border border-zinc-300 dark:border-zinc-700 disabled:opacity-40 disabled:cursor-not-allowed
  hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors"
            >
                Previous
            </button>
            <span className="text-sm text-zinc-500">
                Page {page} of {totalPages}
            </span>
            <button
                onClick={() => goToPage(page + 1)}
                disabled={page >= totalPages}
                className="px-4 py-1.5 text-sm rounded border border-zinc-300 dark:border-zinc-700 disabled:opacity-40 disabled:cursor-not-allowed
  hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors"
            >
                Next
            </button>
        </div>
    );
}