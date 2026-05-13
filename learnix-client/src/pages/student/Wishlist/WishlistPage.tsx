import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Heart } from 'lucide-react';

import { wishlistApi } from '@/api/wishlist.api';
import { queryKeys } from '@/api/queryKeys';
import { WISHLIST_PAGE } from '@/const/localization/wishlistPage';
import { WishlistCard } from './components/WishlistCard';

export default function WishlistPage() {
    const { data, isLoading } = useQuery({
        queryKey: queryKeys.wishlist.mine(),
        queryFn: () => wishlistApi.getMine(0, 50),
    });

    return (
        <div className="container py-10">
            <h1 className="font-heading text-3xl font-bold md:text-4xl">{WISHLIST_PAGE.title}</h1>

            {isLoading ? (
                <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {[1, 2, 3, 4].map((i) => (
                        <div key={i} className="h-[300px] animate-pulse rounded-xl bg-card" />
                    ))}
                </div>
            ) : data?.items.length === 0 ? (
                <div className="mt-16 text-center">
                    <div className="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-accent/10">
                        <Heart className="h-12 w-12 text-accent" />
                    </div>
                    <h2 className="mt-6 font-heading text-2xl font-bold">
                        {WISHLIST_PAGE.emptyTitle}
                    </h2>
                    <p className="mt-2 text-muted-foreground">{WISHLIST_PAGE.emptyDescription}</p>
                    <Link
                        to="/courses"
                        className="mt-8 inline-flex h-11 items-center justify-center rounded-md bg-primary px-8 font-medium text-primary-foreground hover:bg-primary/90"
                    >
                        {WISHLIST_PAGE.browseCourses}
                    </Link>
                </div>
            ) : (
                <div className="mt-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                    {data?.items.map((course) => (
                        <WishlistCard key={course.courseId} course={course} />
                    ))}
                </div>
            )}
        </div>
    );
}
