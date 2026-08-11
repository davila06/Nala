import { useState } from "react";
import { Button } from "@/shared/ui/Button";
import { useDownloadAnnualReport } from "../hooks/useMedical";
import { useMyTier } from "@/features/pets/hooks/useMyTier";
import { toast } from "@/shared/lib/toast";

interface AnnualReportButtonProps {
  petId: string;
  petName: string;
  createdYear?: number;
}

export function AnnualReportButton({ petId, petName, createdYear }: AnnualReportButtonProps) {
  const currentYear = new Date().getFullYear();
  const firstYear = createdYear ?? 2024;
  const years = Array.from(
    { length: currentYear - firstYear + 1 },
    (_, i) => currentYear - i,
  );

  const [year, setYear] = useState(currentYear);
  const { isFamilia } = useMyTier();
  const download = useDownloadAnnualReport(petId);

  if (!isFamilia) return null;

  return (
    <div className="flex items-center gap-2">
      <select
        value={year}
        onChange={(e) => setYear(Number(e.target.value))}
        aria-label="Año del informe"
        className="rounded-xl border border-sand-200 bg-white px-3 py-1.5 text-sm text-sand-800 focus:outline-none focus:ring-2 focus:ring-brand-400"
      >
        {years.map((y) => (
          <option key={y} value={y}>{y}</option>
        ))}
      </select>

      <Button
        size="sm"
        variant="secondary"
        loading={download.isPending}
        onClick={() =>
          download.mutate(year, {
            onError: () => toast.error("No se pudo generar el informe. Intenta de nuevo."),
          })
        }
        aria-label={`Descargar informe anual ${year} de ${petName}`}
      >
        📊 Informe {year}
      </Button>
    </div>
  );
}
