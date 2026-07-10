import { useCallback, useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { CATALOG_PAGE_SIZES, PAGINATION } from '@/const/ui.constants';

export type SortBy = 'popular' | 'newest' | 'rating';

const ALLOWED_PAGE_SIZES: readonly number[] = CATALOG_PAGE_SIZES.desktop;

function useDebounce<T>(value: T, delay: number): T {
    const [debounced, setDebounced] = useState(value);
    useEffect(() => {
        const timer = setTimeout(() => setDebounced(value), delay);
        return () => clearTimeout(timer);
    }, [value, delay]);
    return debounced;
}

export function useCatalogFilters() {
    const [searchParams, setSearchParams] = useSearchParams();
    const searchParam = searchParams.get('search') ?? '';
    const categoryId = searchParams.get('categoryId') ?? '';
    const sortBy = (searchParams.get('sortBy') as SortBy) ?? 'popular';
    const isFreeParam = searchParams.get('isFree');
    const isFree: boolean | undefined =
        isFreeParam === '1' ? true : isFreeParam === '0' ? false : undefined;
    const minRatingParam = searchParams.get('minRating');
    const minRating: number | undefined = minRatingParam ? parseFloat(minRatingParam) : undefined;
    const page = parseInt(searchParams.get('page') ?? '1', 10) || 1;
    const sizeParam = parseInt(searchParams.get('size') ?? '', 10);
    const pageSize = ALLOWED_PAGE_SIZES.includes(sizeParam) ? sizeParam : PAGINATION.CATALOG;

    // Local search input state (debounced before hitting URL/API)
    const [searchInput, setSearchInput] = useState(searchParam);
    const debouncedSearch = useDebounce(searchInput, 350);
    const isFirstMount = useRef(true);

    const setParam = useCallback(
        (key: string, value: string | null) => {
            setSearchParams((prev) => {
                const next = new URLSearchParams(prev);
                if (value === null || value === '') next.delete(key);
                else next.set(key, value);
                return next;
            });
        },
        [setSearchParams],
    );

    const prevSearchRef = useRef(debouncedSearch);

    useEffect(() => {
        if (isFirstMount.current) {
            isFirstMount.current = false;
            return;
        }

        if (prevSearchRef.current !== debouncedSearch) {
            prevSearchRef.current = debouncedSearch;
            setParam('search', debouncedSearch || null);
            setParam('page', null);
        }
    }, [debouncedSearch, setParam]);

    function setPage(p: number) {
        setParam('page', p === 1 ? null : String(p));
    }

    function setPageSize(size: number) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (size === PAGINATION.CATALOG) next.delete('size');
            else next.set('size', String(size));
            next.delete('page');
            return next;
        });
    }

    function setCategoryId(id: string) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (!id) next.delete('categoryId');
            else next.set('categoryId', id);
            next.delete('page');
            return next;
        });
    }

    function setSortBy(val: SortBy) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (val === 'popular') next.delete('sortBy');
            else next.set('sortBy', val);
            next.delete('page');
            return next;
        });
    }

    function setIsFree(val: boolean | undefined) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (val === undefined) next.delete('isFree');
            else next.set('isFree', val ? '1' : '0');
            next.delete('page');
            return next;
        });
    }

    function setMinRating(val: number | undefined) {
        setSearchParams((prev) => {
            const next = new URLSearchParams(prev);
            if (val === undefined) next.delete('minRating');
            else next.set('minRating', String(val));
            next.delete('page');
            return next;
        });
    }

    function clearAllFilters() {
        setSearchInput('');
        setSearchParams(new URLSearchParams());
    }

    return {
        // State
        searchParam,
        categoryId,
        sortBy,
        isFree,
        minRating,
        page,
        pageSize,
        searchInput,
        debouncedSearch,

        // Updaters
        setSearchInput,
        setPage,
        setPageSize,
        setCategoryId,
        setSortBy,
        setIsFree,
        setMinRating,
        clearAllFilters,
    };
}
