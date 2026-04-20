'use client';
import { useRouter, usePathname, useSearchParams } from 'next/navigation';

/**
 * `totalCount`: comes from the backend's `PagedResult`
 * `page` is the current page (1-indexed),
 * `pageSize` is how many items per page
 */
interface PaginationProps {
    totalCount: number;
    page: number;
    pageSize: number;
}

/**
 * Pagination isn't stored in React state (`useState`) - it lives in the URL query string.
 * This means that pagination state survives a page refresh, can be bookmarked or shared, and the SSR storefront page
 * can read the same params on the server without any special handling.
 * 
 * 'use client' directive at the top of the file is required for components that use hooks like `useRouter`, `useSearchParams`, `usePathname`, etc.
 * All hooks that depend on the browser environment. Server components cannot use these hooks.
 * @param totalCount The total number of games to display across all pages, used to calculate total pages.
 * @param page The current page number (1-indexed) to determine which page the user is on and whether "Previous" or "Next" buttons should be disabled.
 * @param pageSize The number of games to display per page.
 * @returns Pagination controls (Previous/Next buttons) or null if there is only one page.
 */
export default function Pagination({ totalCount, page, pageSize }: PaginationProps) {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();

    const totalPages = Math.ceil(totalCount / pageSize);

    if (totalPages <= 1) {
        return null;
    }

    /**
     * Updates the `page` query parameter in the URL to navigate to a different page.
     * It reads the current query string, sets the new page, and pushes the new URL to the router.
     * This function preserves all existing query filter parameters (genre, sort, minPlaytime, etc.) while only updating the `page` parameter.
     * Naively setting `?page=2` directly would wipe all other filters - reading the full current query first prevents that.
     * @param newPage The new page number to navigate to.
     */
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