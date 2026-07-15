import { useQuery } from '@tanstack/react-query';
import { type CategoryListItemDto, categoriesApi } from '@/api/categories.api';
import { queryKeys } from '@/api/queryKeys';

export function useCategories() {
    return useQuery<CategoryListItemDto[]>({
        queryKey: queryKeys.categories.lists(),
        queryFn: () => categoriesApi.getAll(),
        staleTime: 1000 * 60 * 5,
    });
}
