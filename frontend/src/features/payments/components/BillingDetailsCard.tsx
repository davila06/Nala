import { useState, useEffect } from "react";
import { Card, Button } from "@/shared/ui";
import { toast } from "@/shared/lib/toast";
import { useHaptic } from "@/shared/hooks/useHaptic";
import { useBillingProfile, useUpsertBillingProfile, useUserInvoices } from "../hooks/useBilling";
import { billingApi, type TaxIdentificationType } from "../api/billingApi";

export function BillingDetailsCard() {
  const { data: profile, isLoading } = useBillingProfile();
  const { data: invoices = [], isLoading: loadingInvoices } = useUserInvoices();
  const upsertMutation = useUpsertBillingProfile();
  const { tap, success, warning } = useHaptic();

  const [isEditing, setIsEditing] = useState(false);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  // Form state
  const [idType, setIdType] = useState<TaxIdentificationType>("Fisica");
  const [idNumber, setIdNumber] = useState("");
  const [legalName, setLegalName] = useState("");
  const [billingEmail, setBillingEmail] = useState("");
  const [requiresInvoice, setRequiresInvoice] = useState(true);
  const [phoneNumber, setPhoneNumber] = useState("");
  const [addressDetails, setAddressDetails] = useState("");

  useEffect(() => {
    if (profile) {
      setIdType(profile.identificationType);
      setIdNumber(profile.identificationNumber);
      setLegalName(profile.legalName);
      setBillingEmail(profile.billingEmail);
      setRequiresInvoice(profile.requiresInvoice);
      setPhoneNumber(profile.phoneNumber ?? "");
      setAddressDetails(profile.addressDetails ?? "");
    }
  }, [profile]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!idNumber.trim() || !legalName.trim() || !billingEmail.trim()) {
      toast.error("Por favor completa los campos obligatorios.");
      warning();
      return;
    }

    try {
      await upsertMutation.mutateAsync({
        identificationType: idType,
        identificationNumber: idNumber.trim(),
        legalName: legalName.trim(),
        billingEmail: billingEmail.trim(),
        requiresInvoice,
        phoneNumber: phoneNumber.trim() || undefined,
        addressDetails: addressDetails.trim() || undefined,
      });

      toast.success("Datos fiscales guardados con éxito.");
      success();
      setIsEditing(false);
    } catch {
      toast.error("No se pudieron guardar los datos fiscales.");
      warning();
    }
  };

  const handleDownloadPdf = async (invoiceId: string, consecutivo: string) => {
    tap();
    setDownloadingId(invoiceId);
    try {
      const blob = await billingApi.downloadInvoicePdf(invoiceId);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `Factura-${consecutivo}.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch {
      toast.error("No se pudo descargar el PDF de la factura.");
    } finally {
      setDownloadingId(null);
    }
  };

  return (
    <Card>
      <div className="space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-base font-bold text-sand-900">Datos de Facturación Electrónica (DGT Costa Rica)</h2>
            <p className="text-xs text-sand-500">
              Información tributaria para emisión de facturas y tiquetes oficiales según resolución DGT v4.3
            </p>
          </div>
          <span className="text-xl" aria-hidden="true">
            🧾
          </span>
        </div>

        <div className="rounded-xl border border-brand-200 bg-brand-50/70 p-3 text-xs text-brand-900 space-y-1">
          <p className="font-semibold text-brand-950">Política de precios e IVA (13%)</p>
          <p className="text-brand-800 text-[11px] leading-relaxed">
            Los precios vigentes de nuestros planes y servicios son base y no reflejan el 13% de IVA. Si requieres
            Factura Electrónica formal con crédito fiscal para tu contabilidad o empresa, se adicionará el 13% de IVA al
            costo del servicio en tus pagos y renovaciones automáticas.
          </p>
        </div>

        {isLoading ? (
          <div className="py-4 text-center text-xs text-sand-400">Cargando perfil fiscal…</div>
        ) : !isEditing && profile ? (
          <div className="space-y-3">
            <div className="rounded-2xl border border-sand-200 bg-surface-warm p-4 space-y-2">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm font-bold text-sand-900">{profile.legalName}</p>
                  <p className="text-xs text-sand-600 font-mono">
                    {profile.identificationType}: {profile.identificationNumber}
                  </p>
                </div>
                <span className="rounded-full bg-brand-100 px-2.5 py-0.5 text-[10px] font-bold text-brand-800">
                  {profile.requiresInvoice ? "Factura Electrónica" : "Tiquete Electrónico"}
                </span>
              </div>

              <div className="text-xs text-sand-500 pt-1 space-y-0.5">
                <p>📧 Correo de recepción: {profile.billingEmail}</p>
                {profile.phoneNumber && <p>📞 Teléfono: {profile.phoneNumber}</p>}
                {profile.addressDetails && <p>📍 Dirección: {profile.addressDetails}</p>}
              </div>
            </div>

            <Button
              variant="secondary"
              onClick={() => {
                tap();
                setIsEditing(true);
              }}
              className="w-full text-xs font-semibold"
            >
              Editar datos fiscales
            </Button>
          </div>
        ) : !isEditing && !profile ? (
          <div className="rounded-2xl border border-dashed border-sand-300 p-5 text-center space-y-3">
            <p className="text-xs font-semibold text-sand-700">No has configurado tus datos de facturación</p>
            <p className="text-[11px] text-sand-500 max-w-sm mx-auto">
              Si requieres factura electrónica con crédito fiscal para deducción de gastos, registra tu cédula física o
              jurídica.
            </p>
            <Button
              variant="primary"
              onClick={() => {
                tap();
                setIsEditing(true);
              }}
              className="text-xs font-bold"
            >
              + Configurar datos fiscales
            </Button>
          </div>
        ) : (
          <form
            onSubmit={(e) => {
              void handleSubmit(e);
            }}
            className="space-y-3 rounded-2xl border border-sand-200 bg-surface-warm p-4 text-left"
          >
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="tax-id-type" className="block text-[11px] font-semibold text-sand-700 mb-1">
                  Tipo de identificación *
                </label>
                <select
                  id="tax-id-type"
                  value={idType}
                  onChange={(e) => setIdType(e.target.value as TaxIdentificationType)}
                  className="w-full rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500"
                >
                  <option value="Fisica">Cédula Física (9 dígitos)</option>
                  <option value="Juridica">Cédula Jurídica (10 dígitos)</option>
                  <option value="Dimex">DIMEX (11 o 12 dígitos)</option>
                  <option value="Nite">NITE (10 dígitos)</option>
                  <option value="Extranjero">Extranjero / Pasaporte</option>
                </select>
              </div>

              <div>
                <label htmlFor="tax-id-number" className="block text-[11px] font-semibold text-sand-700 mb-1">
                  Número de cédula o ID *
                </label>
                <input
                  id="tax-id-number"
                  type="text"
                  value={idNumber}
                  onChange={(e) => setIdNumber(e.target.value)}
                  placeholder="Ej: 101110222 o 3101123456"
                  className="w-full rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs font-mono text-sand-900 outline-none focus:border-brand-500"
                />
              </div>
            </div>

            <div>
              <label htmlFor="tax-legal-name" className="block text-[11px] font-semibold text-sand-700 mb-1">
                Razón Social / Nombre fiscal completo *
              </label>
              <input
                id="tax-legal-name"
                type="text"
                value={legalName}
                onChange={(e) => setLegalName(e.target.value)}
                placeholder="Nombre exacto registrado en Hacienda"
                className="w-full rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500 uppercase"
              />
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="tax-email" className="block text-[11px] font-semibold text-sand-700 mb-1">
                  Correo para recibir XML/Facturas *
                </label>
                <input
                  id="tax-email"
                  type="email"
                  value={billingEmail}
                  onChange={(e) => setBillingEmail(e.target.value)}
                  placeholder="facturacion@empresa.com"
                  className="w-full rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500"
                />
              </div>

              <div>
                <label htmlFor="tax-phone" className="block text-[11px] font-semibold text-sand-700 mb-1">
                  Teléfono de contacto
                </label>
                <input
                  id="tax-phone"
                  type="tel"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  placeholder="8888-8888"
                  className="w-full rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500"
                />
              </div>
            </div>

            <div>
              <label htmlFor="tax-address" className="block text-[11px] font-semibold text-sand-700 mb-1">
                Dirección fiscal
              </label>
              <input
                id="tax-address"
                type="text"
                value={addressDetails}
                onChange={(e) => setAddressDetails(e.target.value)}
                placeholder="Provincia, cantón y otras señas"
                className="w-full rounded-xl border border-sand-200 bg-surface px-3 py-2 text-xs text-sand-900 outline-none focus:border-brand-500"
              />
            </div>

            <label className="flex items-start gap-2 pt-1 cursor-pointer">
              <input
                type="checkbox"
                checked={requiresInvoice}
                onChange={(e) => setRequiresInvoice(e.target.checked)}
                className="mt-0.5 h-4 w-4 rounded text-brand-600 focus:ring-brand-500 border-sand-300"
              />
              <span className="text-[11px] text-sand-700 font-medium">
                Deseo Factura Electrónica formal con crédito fiscal (+13% IVA sobre el costo base del servicio;
                desmarcar si solo requiere Tiquete Electrónico)
              </span>
            </label>

            <div className="flex gap-2 pt-2">
              <Button
                type="submit"
                variant="primary"
                loading={upsertMutation.isPending}
                className="flex-1 text-xs font-bold"
              >
                Guardar datos fiscales
              </Button>
              <Button
                type="button"
                variant="secondary"
                onClick={() => setIsEditing(false)}
                className="text-xs font-semibold"
              >
                Cancelar
              </Button>
            </div>
          </form>
        )}

        {/* ── Historial de Facturas Emitidas ───────────────────────────────── */}
        <div className="pt-2 border-t border-sand-200">
          <p className="text-xs font-bold text-sand-800 mb-2">Mis comprobantes electrónicos emitidos</p>

          {loadingInvoices ? (
            <div className="py-2 text-center text-xs text-sand-400">Cargando comprobantes…</div>
          ) : invoices.length === 0 ? (
            <p className="text-[11px] text-sand-400">Aún no tienes comprobantes electrónicos generados.</p>
          ) : (
            <div className="space-y-2">
              {invoices.map((inv) => {
                const isDownloading = downloadingId === inv.id;
                return (
                  <div
                    key={inv.id}
                    className="flex items-center justify-between p-3 rounded-xl border border-sand-200 bg-surface text-xs"
                  >
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-sand-900">
                          {inv.documentType === "FacturaElectronica" ? "Factura" : "Tiquete"} #
                          {inv.numeroConsecutivo.slice(-6)}
                        </span>
                        <span className="rounded-md bg-rescue-100 text-rescue-800 px-1.5 py-0.2 text-[9px] font-bold">
                          ₡{inv.totalAmountCrc.toLocaleString("es-CR")}
                        </span>
                      </div>
                      <p className="text-[10px] text-sand-500 font-mono mt-0.5">
                        {new Date(inv.issuedAt).toLocaleDateString("es-CR")} · {inv.serviceDescription}
                      </p>
                    </div>

                    <button
                      type="button"
                      disabled={isDownloading}
                      onClick={() => void handleDownloadPdf(inv.id, inv.numeroConsecutivo)}
                      className="inline-flex items-center gap-1 rounded-lg border border-sand-200 bg-surface-warm px-2.5 py-1 text-[11px] font-bold text-sand-700 hover:bg-sand-100 transition-colors disabled:opacity-50"
                    >
                      {isDownloading ? (
                        <span className="h-3 w-3 rounded-full border-2 border-sand-400 border-t-transparent animate-spin" />
                      ) : (
                        <span>📄 PDF</span>
                      )}
                    </button>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}
