import { cn } from "@/lib/utils"

interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {}

export function Skeleton({ className, ...props }: SkeletonProps) {
  return (
    <div
      className={cn("relative overflow-hidden rounded-md bg-muted before:absolute before:inset-0 before:-translate-x-full before:animate-[shimmer_2s_infinite] before:bg-gradient-to-r before:from-transparent before:via-white/10 before:to-transparent", className)}
      {...props}
    />
  )
}

// Skeleton específicos reutilizables

export function SkeletonCard() {
  return (
    <div className="rounded-lg border bg-card p-6 space-y-3">
      <Skeleton className="h-4 w-24" />
      <Skeleton className="h-8 w-32" />
      <Skeleton className="h-3 w-40" />
    </div>
  )
}

export function SkeletonKpiCard() {
  return (
    <div className="rounded-lg border bg-card">
      <div className="p-5 space-y-2">
        <div className="flex items-center gap-2">
          <Skeleton className="h-4 w-4 rounded-full" />
          <Skeleton className="h-3 w-16" />
        </div>
        <Skeleton className="h-6 w-24" />
        <Skeleton className="h-3 w-32" />
      </div>
    </div>
  )
}

export function SkeletonTable({ rows = 5 }: { rows?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex items-center justify-between py-3 border-b">
          <div className="space-y-2 flex-1">
            <Skeleton className="h-4 w-40" />
            <Skeleton className="h-3 w-56" />
          </div>
          <Skeleton className="h-4 w-20" />
        </div>
      ))}
    </div>
  )
}

export function SkeletonChart() {
  return (
    <div className="h-60 w-full flex items-end justify-around gap-2 p-4">
      <Skeleton className="w-12 h-32" />
      <Skeleton className="w-12 h-40" />
      <Skeleton className="w-12 h-48" />
      <Skeleton className="w-12 h-36" />
      <Skeleton className="w-12 h-44" />
      <Skeleton className="w-12 h-52" />
    </div>
  )
}

export function SkeletonDashboard() {
  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="space-y-2">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-4 w-32" />
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <SkeletonKpiCard />
        <SkeletonKpiCard />
        <SkeletonKpiCard />
        <SkeletonKpiCard />
      </div>

      {/* Charts */}
      <div className="grid md:grid-cols-2 gap-4">
        <div className="rounded-lg border bg-card p-4">
          <Skeleton className="h-5 w-48 mb-4" />
          <SkeletonChart />
        </div>
        <div className="rounded-lg border bg-card p-4">
          <Skeleton className="h-5 w-48 mb-4" />
          <SkeletonChart />
        </div>
      </div>

      {/* List */}
      <div className="rounded-lg border bg-card p-4">
        <Skeleton className="h-5 w-40 mb-4" />
        <SkeletonTable rows={5} />
      </div>
    </div>
  )
}
