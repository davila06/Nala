import { Suspense } from "react";
import { FeatureErrorBoundary } from "@/shared/ui/FeatureErrorBoundary";
import { Skeleton } from "@/shared/ui/Spinner";

const PageSkeleton = () => (
  <div className="mx-auto max-w-lg space-y-4 px-4 py-10 animate-pulse">
    <Skeleton className="h-8 w-48 rounded" />
    <Skeleton className="h-4 w-72 rounded" />
    <Skeleton className="h-48 rounded-2xl" />
    <Skeleton className="h-10 rounded-xl" />
    <Skeleton className="h-10 rounded-xl" />
  </div>
);

export function RouteShell({
  children,
  name,
}: {
  children: React.ReactNode;
  name?: string;
}) {
  return (
    <FeatureErrorBoundary featureName={name}>
      <Suspense fallback={<PageSkeleton />}>{children}</Suspense>
    </FeatureErrorBoundary>
  );
}
