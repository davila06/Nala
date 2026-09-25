import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { clinicsApi, type ClinicPaymentMethod } from "../api/clinicsApi";
import { Button, Input } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";

const paymentMethods: Array<{ value: ClinicPaymentMethod; label: string }> = [
  { value: "Cash", label: "Efectivo" },
  { value: "Card", label: "Tarjeta" },
  { value: "Sinpe", label: "SINPE" },
  { value: "Transfer", label: "Transferencia" },
  { value: "InternalCredit", label: "Crédito interno" },
];

export default function ClinicFinanceWorkspacePage() {
  const queryClient = useQueryClient();
  const [clinicId, setClinicId] = useState("");
  const [businessDate, setBusinessDate] = useState(() =>
    new Intl.DateTimeFormat("en-CA", {
      timeZone: "America/Costa_Rica",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    }).format(new Date()),
  );
  const [saleId, setSaleId] = useState("");
  const [receiptNumber, setReceiptNumber] = useState("");
  const [description, setDescription] = useState("");
  const [amountCrc, setAmountCrc] = useState(0);
  const [paymentMethod, setPaymentMethod] = useState<ClinicPaymentMethod>("Sinpe");
  const [paymentReference, setPaymentReference] = useState("");
  const [paymentId, setPaymentId] = useState("");
  const [refundAmountCrc, setRefundAmountCrc] = useState(0);
  const [refundReason, setRefundReason] = useState("");
  const [refundEvidence, setRefundEvidence] = useState("");
  const [voidReason, setVoidReason] = useState("");
  const { data: workspaces = [], isLoading } = useQuery({
    queryKey: ["clinics", "finance-workspaces"],
    queryFn: clinicsApi.getFinanceWorkspaces,
  });
  const selected = workspaces.find((workspace) => workspace.clinicId === clinicId) ?? workspaces[0];
  const activeClinicId = selected?.clinicId ?? "";
  const isAdministrator = selected?.role === "Administrator";
  const reportKey = ["clinics", "finance", activeClinicId, businessDate];
  const ledgerKey = ["clinics", "finance-ledger", activeClinicId, saleId];
  const { data: report } = useQuery({
    queryKey: reportKey,
    queryFn: () => clinicsApi.getStaffSalesReport(activeClinicId, businessDate),
    enabled: Boolean(activeClinicId && businessDate),
  });
  const { data: ledger } = useQuery({
    queryKey: ledgerKey,
    queryFn: () => clinicsApi.getStaffSaleLedger(activeClinicId, saleId),
    enabled: Boolean(activeClinicId && saleId),
  });
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: reportKey });
    void queryClient.invalidateQueries({ queryKey: ledgerKey });
  };
  const createSale = useMutation({
    mutationFn: () =>
      clinicsApi.createStaffSale(activeClinicId, {
        receiptNumber,
        lines: [{ description, type: "Service", quantity: 1, unitPriceCrc: amountCrc }],
      }),
    onSuccess: (sale) => {
      setSaleId(sale.id);
      refresh();
      toast.success("Venta registrada.");
    },
    onError: () => toast.error("No se pudo registrar la venta."),
  });
  const pay = useMutation({
    mutationFn: () =>
      clinicsApi.registerStaffPayment(activeClinicId, saleId, amountCrc, paymentMethod, paymentReference),
    onSuccess: () => {
      refresh();
      toast.success("Pago registrado.");
    },
    onError: () => toast.error("No se pudo registrar el pago."),
  });
  const refund = useMutation({
    mutationFn: () =>
      clinicsApi.recordStaffRefund(activeClinicId, saleId, paymentId, refundAmountCrc, refundReason, refundEvidence),
    onSuccess: () => {
      refresh();
      toast.success("Devolución registrada.");
    },
    onError: () => toast.error("No se pudo registrar la devolución."),
  });
  const voidSale = useMutation({
    mutationFn: () => clinicsApi.voidStaffSale(activeClinicId, saleId, voidReason),
    onSuccess: () => {
      refresh();
      toast.success("Venta anulada.");
    },
    onError: () => toast.error("Devuelve todos los pagos y verifica el estado fiscal antes de anular."),
  });
  const closeCash = useMutation({
    mutationFn: () => clinicsApi.closeStaffCash(activeClinicId, businessDate),
    onSuccess: () => toast.success("Cierre registrado."),
    onError: () => toast.error("No se pudo cerrar caja."),
  });
  const fiscal = useMutation({
    mutationFn: () => clinicsApi.submitStaffFiscalSale(activeClinicId, saleId),
    onSuccess: (submission) => toast.success(`Estado fiscal: ${submission.status}`),
    onError: () => toast.error("Proveedor o emisor fiscal no verificado."),
  });
  const selectedPayment = ledger?.payments.find((payment) => payment.id === paymentId);
  const refundable = selectedPayment
    ? selectedPayment.amountCrc -
      (ledger?.refunds ?? [])
        .filter((item) => item.paymentId === paymentId)
        .reduce((sum, item) => sum + item.amountCrc, 0)
    : 0;

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-8">
      <header className="border-b border-sand-200 pb-4">
        <h1 className="font-display text-2xl font-semibold text-sand-900">Caja clínica</h1>
        <div className="mt-3 flex flex-wrap items-center gap-3">
          <select
            className="field-input max-w-xs"
            aria-label="Clínica de caja"
            value={activeClinicId}
            onChange={(event) => {
              setClinicId(event.target.value);
              setSaleId("");
            }}
          >
            {workspaces.map((workspace) => (
              <option key={workspace.clinicId} value={workspace.clinicId}>
                {workspace.clinicName}
              </option>
            ))}
          </select>
          <input
            className="field-input"
            type="date"
            aria-label="Fecha de caja"
            value={businessDate}
            onChange={(event) => setBusinessDate(event.target.value)}
          />
          {selected && (
            <span className="text-xs font-semibold text-sand-600">{isAdministrator ? "Administración" : "Caja"}</span>
          )}
        </div>
      </header>
      {!isLoading && !selected && <p className="text-sm text-sand-700">No tienes acceso a una caja clínica.</p>}
      {selected && (
        <>
          <section className="grid gap-4 border-b border-sand-200 pb-5 sm:grid-cols-3">
            <div>
              <h2 className="text-xs font-semibold text-sand-600">Neto del día</h2>
              <p className="text-xl font-semibold text-sand-900">
                ₡{(report?.totalPaidCrc ?? 0).toLocaleString("es-CR")}
              </p>
            </div>
            <div>
              <h2 className="text-xs font-semibold text-sand-600">Métodos de pago</h2>
              {Object.entries(report?.byPaymentMethod ?? {}).map(([method, amount]) => (
                <p key={method} className="text-sm">
                  {method}: ₡{amount.toLocaleString("es-CR")}
                </p>
              ))}
            </div>
            <div>
              <h2 className="text-xs font-semibold text-sand-600">Por veterinario</h2>
              {Object.entries(report?.byVeterinarian ?? {}).map(([name, amount]) => (
                <p key={name} className="text-sm">
                  {name}: ₡{amount.toLocaleString("es-CR")}
                </p>
              ))}
            </div>
          </section>
          <section className="space-y-3">
            <h2 className="text-base font-semibold text-sand-900">Venta</h2>
            <div className="grid gap-2 sm:grid-cols-3">
              <Input
                aria-label="Recibo"
                placeholder="Número de recibo"
                value={receiptNumber}
                onChange={(event) => setReceiptNumber(event.target.value)}
              />
              <Input
                aria-label="Descripción de servicio"
                placeholder="Servicio"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
              />
              <input
                className="field-input"
                aria-label="Monto CRC"
                type="number"
                min={1}
                value={amountCrc}
                onChange={(event) => setAmountCrc(Number(event.target.value))}
              />
            </div>
            <Button
              disabled={!receiptNumber.trim() || !description.trim() || amountCrc <= 0 || createSale.isPending}
              onClick={() => createSale.mutate()}
            >
              Crear venta
            </Button>
          </section>
          <section className="space-y-3 border-t border-sand-200 pt-5">
            <h2 className="text-base font-semibold text-sand-900">Cobro y libro de venta</h2>
            <Input
              aria-label="ID de venta"
              placeholder="ID de venta"
              value={saleId}
              onChange={(event) => setSaleId(event.target.value)}
            />
            {ledger && (
              <p className="text-sm text-sand-700">
                {ledger.sale.receiptNumber} · {ledger.sale.status} · Saldo ₡
                {ledger.sale.balanceCrc.toLocaleString("es-CR")}
              </p>
            )}
            <div className="grid gap-2 sm:grid-cols-2">
              <select
                className="field-input"
                aria-label="Método de pago"
                value={paymentMethod}
                onChange={(event) => setPaymentMethod(event.target.value as ClinicPaymentMethod)}
              >
                {paymentMethods.map(({ value, label }) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
              <Input
                aria-label="Referencia de pago"
                placeholder="Referencia"
                value={paymentReference}
                onChange={(event) => setPaymentReference(event.target.value)}
              />
            </div>
            <Button disabled={!saleId || amountCrc <= 0 || pay.isPending} onClick={() => pay.mutate()}>
              Registrar pago
            </Button>
            {ledger?.payments.map((payment) => (
              <p key={payment.id} className="text-xs text-sand-700">
                {payment.method} · ₡{payment.amountCrc.toLocaleString("es-CR")} · {payment.reference || payment.id}
              </p>
            ))}
          </section>
          {isAdministrator && (
            <section className="space-y-3 border-t border-sand-200 pt-5">
              <h2 className="text-base font-semibold text-sand-900">Conciliación y fiscal</h2>
              <select
                className="field-input"
                aria-label="Pago a devolver"
                value={paymentId}
                onChange={(event) => setPaymentId(event.target.value)}
              >
                <option value="">Selecciona un pago</option>
                {ledger?.payments.map((payment) => (
                  <option key={payment.id} value={payment.id}>
                    {payment.method} · ₡{payment.amountCrc.toLocaleString("es-CR")}
                  </option>
                ))}
              </select>
              <p className="text-xs text-sand-600">Saldo devolvible: ₡{refundable.toLocaleString("es-CR")}</p>
              <div className="grid gap-2 sm:grid-cols-3">
                <input
                  className="field-input"
                  aria-label="Monto devolución"
                  type="number"
                  min={1}
                  max={refundable}
                  value={refundAmountCrc}
                  onChange={(event) => setRefundAmountCrc(Number(event.target.value))}
                />
                <Input
                  aria-label="Motivo devolución"
                  placeholder="Motivo"
                  value={refundReason}
                  onChange={(event) => setRefundReason(event.target.value)}
                />
                <Input
                  aria-label="Comprobante devolución"
                  placeholder="Comprobante"
                  value={refundEvidence}
                  onChange={(event) => setRefundEvidence(event.target.value)}
                />
              </div>
              <Button
                disabled={
                  !paymentId ||
                  refundAmountCrc <= 0 ||
                  refundAmountCrc > refundable ||
                  !refundReason.trim() ||
                  !refundEvidence.trim() ||
                  refund.isPending
                }
                onClick={() => refund.mutate()}
              >
                Registrar devolución
              </Button>
              <div className="flex flex-wrap items-center gap-2">
                <Input
                  aria-label="Motivo anulación"
                  placeholder="Motivo de anulación"
                  value={voidReason}
                  onChange={(event) => setVoidReason(event.target.value)}
                />
                <Button
                  variant="secondary"
                  disabled={!saleId || !voidReason.trim() || voidSale.isPending}
                  onClick={() => voidSale.mutate()}
                >
                  Anular venta
                </Button>
                <Button variant="secondary" disabled={!saleId || fiscal.isPending} onClick={() => fiscal.mutate()}>
                  Presentar a proveedor fiscal
                </Button>
                <Button variant="secondary" disabled={closeCash.isPending} onClick={() => closeCash.mutate()}>
                  Cerrar caja
                </Button>
              </div>
            </section>
          )}
        </>
      )}
    </main>
  );
}
