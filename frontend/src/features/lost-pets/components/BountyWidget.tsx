import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import {
  useBountyForEvent,
  useConfirmBountyDeposit,
  useCreateBounty,
  useReleaseBounty,
} from "../hooks/useBounty";

const SINPE_NUMBER = import.meta.env.VITE_SINPE_PHONE ?? "7000-0000";

interface BountyWidgetProps {
  lostEventId: string;
  isOwner: boolean;
}

export function BountyWidget({ lostEventId, isOwner }: BountyWidgetProps) {
  const [showCreate, setShowCreate] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [amount, setAmount] = useState("");

  const { data: bounty, isLoading } = useBountyForEvent(lostEventId);
  const { mutateAsync: createBounty, isPending: isCreating } =
    useCreateBounty();
  const { mutateAsync: confirmDeposit, isPending: isConfirming } =
    useConfirmBountyDeposit();
  const { mutateAsync: releaseBounty, isPending: isReleasing } =
    useReleaseBounty();

  if (isLoading)
    return <div className="h-14 animate-pulse rounded-2xl bg-sand-100" />;

  const statusColor: Record<string, string> = {
    PendingDeposit: "bg-warn-100 text-warn-700",
    Active: "bg-rescue-100 text-rescue-800",
    Claimed: "bg-trust-100 text-trust-700",
    Released: "bg-sand-100 text-sand-600",
    Refunded: "bg-sand-100 text-sand-500",
    Expired: "bg-sand-100 text-sand-400",
  };

  const statusLabel: Record<string, string> = {
    PendingDeposit: "Depósito pendiente",
    Active: "Activa 🟢",
    Claimed: "Reclamada",
    Released: "Pagada",
    Refunded: "Reembolsada",
    Expired: "Expirada",
  };

  if (!bounty && isOwner) {
    return (
      <div className="rounded-2xl border border-dashed border-warn-300 bg-warn-50 p-4">
        <p className="text-sm font-semibold text-warn-800">
          ¿Quieres ofrecer una recompensa?
        </p>
        <p className="mt-0.5 text-xs text-warn-600">
          La recompensa aparece en el mapa y motiva a la red a buscar
          activamente. PawTrack retiene un 10% de comisión al liberarla.
        </p>
        {!showCreate ? (
          <button
            type="button"
            onClick={() => setShowCreate(true)}
            className="mt-3 rounded-xl bg-warn-600 px-4 py-2 text-xs font-bold text-white hover:bg-warn-700 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-warn-400"
          >
            Ofrecer recompensa →
          </button>
        ) : (
          <div className="mt-3 space-y-2">
            <label className="block text-xs font-semibold text-warn-800">
              Monto (₡ CRC)
              <input
                type="number"
                min="5000"
                step="1000"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="ej. 25000"
                className="mt-1 w-full rounded-xl border border-warn-300 px-3 py-2 text-sm text-sand-900 outline-none focus:border-warn-500"
              />
            </label>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={async () => {
                  const parsed = parseFloat(amount);
                  if (!parsed || parsed < 5000) return;
                  await createBounty({ lostEventId, amount: parsed });
                  setShowCreate(false);
                  setShowConfirm(true);
                }}
                disabled={isCreating || !amount}
                className="rounded-xl bg-warn-600 px-4 py-2 text-xs font-bold text-white hover:bg-warn-700 disabled:opacity-60"
              >
                {isCreating ? "Creando…" : "Continuar con SINPE →"}
              </button>
              <button
                type="button"
                onClick={() => setShowCreate(false)}
                className="text-xs text-sand-500 hover:text-sand-700"
              >
                Cancelar
              </button>
            </div>
          </div>
        )}

        {showConfirm && (
          <div className="rounded-2xl border border-warn-200 bg-warn-50 px-4 py-3 text-sm text-warn-800">
            <p className="font-semibold">🏦 Deposita vía SINPE Móvil</p>
            <p className="mt-1 text-xs text-warn-700">
              Envía ₡{parseFloat(amount || "0").toLocaleString("es-CR")} con el
              número de referencia que recibirás por notificación.
            </p>
            <button
              type="button"
              onClick={() => setShowConfirm(false)}
              className="mt-2 text-xs font-semibold text-warn-700 underline hover:text-warn-900"
            >
              Entendido
            </button>
          </div>
        )}
      </div>
    );
  }

  if (!bounty) return null;

  return (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0, y: 4 }}
        animate={{ opacity: 1, y: 0 }}
        className="rounded-2xl border border-warn-200 bg-warn-50 p-4"
      >
        <div className="flex items-start justify-between gap-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-warn-700">
              Recompensa
            </p>
            <p className="mt-0.5 text-xl font-black text-sand-900">
              ₡{bounty.amount.toLocaleString("es-CR")}
            </p>
            {bounty.netPayoutAmount < bounty.amount && (
              <p className="text-[10px] text-sand-400">
                Pago neto: ₡{bounty.netPayoutAmount.toLocaleString("es-CR")}{" "}
                (10% fee)
              </p>
            )}
          </div>
          <span
            className={`rounded-full px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wide ${statusColor[bounty.status] ?? "bg-sand-100 text-sand-500"}`}
          >
            {statusLabel[bounty.status] ?? bounty.status}
          </span>
        </div>

        {/* Owner: confirm deposit for PendingDeposit bounty */}
        {isOwner && bounty.status === "PendingDeposit" && (
          <div className="mt-3 rounded-xl border border-warn-200 bg-warn-50/80 p-3 space-y-2">
            <p className="text-xs font-semibold text-warn-800">
              Envía ₡{bounty.amount.toLocaleString("es-CR")} al{" "}
              <strong>{SINPE_NUMBER}</strong> con referencia:
            </p>
            <p className="font-mono text-center text-lg font-black tracking-[0.2em] text-sand-900">
              {bounty.depositReference}
            </p>
            <button
              type="button"
              onClick={() => void confirmDeposit(bounty.depositReference)}
              disabled={isConfirming}
              className="w-full rounded-xl bg-warn-600 py-2 text-xs font-bold text-white hover:bg-warn-700 disabled:opacity-60"
            >
              {isConfirming ? "Verificando…" : "✓ Ya deposité"}
            </button>
          </div>
        )}

        {/* Owner: release payment after HandoverCode confirmation */}
        {isOwner && bounty.status === "Claimed" && (
          <div className="mt-3 rounded-xl border border-rescue-200 bg-rescue-50 p-3 space-y-2">
            <p className="text-sm font-semibold text-rescue-800">
              🎉 ¡Entrega confirmada!
            </p>
            <p className="text-xs text-rescue-700">
              El rescatador verificó el HandoverCode. Confirma para liberar ₡
              {bounty.netPayoutAmount.toLocaleString("es-CR")} (después del fee
              10%).
            </p>
            <button
              type="button"
              onClick={() => void releaseBounty(bounty.id)}
              disabled={isReleasing}
              className="w-full rounded-xl bg-rescue-600 py-2 text-xs font-bold text-white hover:bg-rescue-700 disabled:opacity-60"
            >
              {isReleasing ? "Liberando…" : "✓ Liberar recompensa"}
            </button>
          </div>
        )}
      </motion.div>
    </AnimatePresence>
  );
}
