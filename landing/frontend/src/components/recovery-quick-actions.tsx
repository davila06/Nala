type RecoveryQuickActionsProps = {
  foundHref: string;
  lostHref: string;
};

export function RecoveryQuickActions({ foundHref, lostHref }: RecoveryQuickActionsProps) {
  return (
    <nav aria-label="Acciones rápidas de recuperación" className="recovery-quick-actions">
      <a className="recovery-quick-lost" data-analytics-event="report_lost_pet_clicked" href={lostHref}>
        Perdí <span aria-hidden="true">↗</span>
      </a>
      <a className="recovery-quick-found" data-analytics-event="report_found_pet_clicked" href={foundHref}>
        Encontré <span aria-hidden="true">↗</span>
      </a>
    </nav>
  );
}
