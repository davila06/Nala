import { useRef } from "react";
import { Helmet } from "react-helmet-async";
import { Button } from "@/shared/ui";
import { Alert } from "@/shared/ui/Alert";
import { Skeleton } from "@/shared/ui/Spinner";
import { toast } from "@/shared/lib/toast";
import { serviceProvidersApi } from "../api/serviceProvidersApi";
import {
  useProviderVerification,
  useUploadProviderVerificationDocument,
} from "../hooks/useServiceProviders";

const MAX_BYTES = 5 * 1024 * 1024;
const ACCEPTED_TYPES = new Set([
  "application/pdf",
  "image/jpeg",
  "image/png",
  "image/webp",
]);

export default function ProviderVerificationPage() {
  const inputRef = useRef<HTMLInputElement>(null);
  const { data: verification, isLoading } = useProviderVerification();
  const upload = useUploadProviderVerificationDocument();
  const uploadDocument = (file?: File) => {
    if (!file) return;
    if (!ACCEPTED_TYPES.has(file.type) || file.size > MAX_BYTES)
      return toast.error("Usa PDF, JPEG, PNG o WebP de hasta 5 MB.");
    upload.mutate(file, {
      onSuccess: () => toast.success("Documento enviado para revision"),
      onError: () => toast.error("No se pudo cargar el documento."),
    });
  };
  const download = async () => {
    try {
      const fileBlob = await serviceProvidersApi.downloadVerificationDocument();
      const url = URL.createObjectURL(fileBlob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = "verificacion-proveedor";
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error("No se pudo descargar el documento.");
    }
  };
  if (isLoading)
    return (
      <div className="mx-auto max-w-2xl p-8">
        <Skeleton className="h-56 rounded-xl" />
      </div>
    );
  const statusText =
    verification?.status === "Verified"
      ? "Verificado"
      : verification?.status === "Rejected"
        ? "Requiere correccion"
        : verification?.status === "Expired"
          ? "Verificacion vencida"
          : verification
            ? "En revision"
            : "Sin documento";
  return (
    <main className="mx-auto max-w-2xl space-y-5 px-4 py-8">
      <Helmet>
        <title>Verificacion · PawTrack CR</title>
      </Helmet>
      <header>
        <h1 className="font-display text-2xl font-semibold text-ink-900">
          Verificacion del proveedor
        </h1>
        <p className="mt-1 text-sm text-sand-600">
          Tu documento se guarda de forma privada y solo lo revisa el equipo
          autorizado.
        </p>
      </header>
      <section className="space-y-4 rounded-xl border border-sand-100 bg-surface p-5">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-sm font-semibold text-ink-900">
              Estado: {statusText}
            </p>
            {verification?.expiresAt ? (
              <p className="mt-1 text-sm text-sand-600">
                Vence el{" "}
                {new Date(verification.expiresAt).toLocaleDateString("es-CR")}
              </p>
            ) : null}
          </div>
          {verification ? (
            <span className="rounded-full bg-sand-100 px-2 py-1 text-xs font-semibold text-sand-700">
              {verification.status}
            </span>
          ) : null}
        </div>
        {verification?.rejectionReason ? (
          <Alert variant="error">{verification.rejectionReason}</Alert>
        ) : null}
        <input
          ref={inputRef}
          type="file"
          accept="application/pdf,image/jpeg,image/png,image/webp"
          className="sr-only"
          onChange={(event) => {
            uploadDocument(event.target.files?.[0]);
            event.target.value = "";
          }}
        />{" "}
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            loading={upload.isPending}
            onClick={() => inputRef.current?.click()}
          >
            {verification ? "Reemplazar documento" : "Cargar documento"}
          </Button>
          {verification ? (
            <Button
              type="button"
              variant="secondary"
              onClick={() => void download()}
            >
              Descargar documento
            </Button>
          ) : null}
        </div>
      </section>
    </main>
  );
}
