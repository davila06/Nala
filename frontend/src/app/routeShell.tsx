import { Suspense } from "react";
import { FeatureErrorBoundary } from "@/shared/ui/FeatureErrorBoundary";
import { Skeleton } from "@/shared/ui/Spinner";

export type PageSkeletonVariant = "default" | "dashboard" | "detail" | "form" | "directory" | "map";

const PageSkeleton = ({ variant = "default" }: { variant?: PageSkeletonVariant }) => {
  const content = {
    default: (
      <>
        <Skeleton className="h-8 w-48 rounded" />
        <Skeleton className="h-4 w-72 rounded" />
        <Skeleton className="h-48 rounded-2xl" />
        <Skeleton className="h-10 rounded-xl" />
        <Skeleton className="h-10 rounded-xl" />
      </>
    ),
    dashboard: (
      <>
        <div className="flex items-center justify-between gap-4">
          <div className="space-y-2">
            <Skeleton className="h-4 w-32 rounded" />
            <Skeleton className="h-8 w-56 rounded" />
          </div>
          <Skeleton className="h-10 w-36 rounded-xl" />
        </div>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <Skeleton className="h-16 rounded-xl" />
          <Skeleton className="h-16 rounded-xl" />
          <Skeleton className="h-16 rounded-xl" />
          <Skeleton className="h-16 rounded-xl" />
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <Skeleton className="h-64 rounded-2xl" />
          <Skeleton className="h-64 rounded-2xl" />
        </div>
      </>
    ),
    detail: (
      <>
        <Skeleton className="h-72 rounded-2xl" />
        <div className="space-y-2">
          <Skeleton className="h-8 w-56 rounded" />
          <Skeleton className="h-4 w-72 rounded" />
        </div>
        <Skeleton className="h-32 rounded-xl" />
      </>
    ),
    form: (
      <>
        <Skeleton className="h-8 w-56 rounded" />
        <Skeleton className="h-4 w-80 rounded" />
        <Skeleton className="h-12 rounded-xl" />
        <Skeleton className="h-12 rounded-xl" />
        <Skeleton className="h-32 rounded-xl" />
        <Skeleton className="h-12 rounded-xl" />
      </>
    ),
    directory: (
      <>
        <Skeleton className="h-8 w-64 rounded" />
        <Skeleton className="h-10 rounded-xl" />
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          <Skeleton className="h-52 rounded-xl" />
          <Skeleton className="h-52 rounded-xl" />
          <Skeleton className="h-52 rounded-xl" />
          <Skeleton className="h-52 rounded-xl" />
        </div>
      </>
    ),
    map: <Skeleton className="h-[min(70vh,640px)] rounded-2xl" />,
  }[variant];

  return <div className="mx-auto max-w-5xl space-y-4 px-4 py-10 animate-pulse">{content}</div>;
};

export function RouteShell({
  children,
  name,
  skeleton = "default",
}: {
  children: React.ReactNode;
  name?: string;
  skeleton?: PageSkeletonVariant;
}) {
  return (
    <FeatureErrorBoundary featureName={name}>
      <Suspense fallback={<PageSkeleton variant={skeleton} />}>{children}</Suspense>
    </FeatureErrorBoundary>
  );
}
