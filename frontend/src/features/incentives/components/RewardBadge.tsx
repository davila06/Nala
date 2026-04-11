interface RewardBadgeProps {
  rewardAmount?: number | null
  rewardNote?: string | null
  className?: string
}

const CRC_FORMAT = new Intl.NumberFormat('es-CR', {
  style: 'currency',
  currency: 'CRC',
  maximumFractionDigits: 0,
})

export function RewardBadge({ rewardAmount, rewardNote, className = '' }: RewardBadgeProps) {
  if (!rewardAmount) return null

  return (
    <div
      className={`inline-flex items-start gap-2 rounded-xl border border-warn-200 bg-warn-50 px-3 py-2 ${className}`}
      title={rewardNote ?? undefined}
    >
      <span className="mt-0.5 text-base" aria-hidden="true">🏅</span>
      <div>
        <p className="text-sm font-bold text-warn-800 leading-none">
          Recompensa: {CRC_FORMAT.format(rewardAmount)}
        </p>
        {rewardNote && (
          <p className="mt-0.5 text-xs text-warn-700 leading-snug">{rewardNote}</p>
        )}
      </div>
    </div>
  )
}
