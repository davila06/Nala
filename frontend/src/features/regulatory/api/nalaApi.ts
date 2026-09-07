import { apiClient } from "@/shared/lib/apiClient";

export interface NalaOverviewDto {
  periodStart: string;
  periodEnd: string;
  activeLostPets: number;
  reunitedPets: number;
  capturedAnimals: number;
  availableAdoptions: number;
  adoptedAnimals: number;
  openWelfareCases: number;
  verifiedMicrochips: number;
  validCertificates: number;
  generatedAt: string;
  isSuppressed: boolean;
}

export type ReportType =
  | "Recovery"
  | "MunicipalCaptures"
  | "Adoptions"
  | "WelfareCases"
  | "SanitaryIdentity"
  | "NetworkCoverage"
  | "NalaOverview";
export type ExportScope = "Public" | "Institutional" | "Admin" | "Nala";
export type ExportFormat = "Csv" | "Json" | "Pdf";

export interface ReportDefinitionDto {
  code: string;
  reportType: ReportType;
  name: string;
  schemaVersion: string;
  scope: ExportScope;
  suppressionThreshold: number;
  retentionDays: number;
}

export interface RegulatoryExportDto {
  id: string;
  exportCode: string;
  reportType: ReportType;
  scope: ExportScope;
  format: ExportFormat;
  schemaVersion: string;
  status: string;
  periodStart: string;
  periodEnd: string;
  rowCount: number | null;
  suppressedRowCount: number | null;
  payloadSha256: string | null;
  requestedAt: string;
  completedAt: string | null;
  expiresAt: string | null;
}

export interface NalaMapCellDto {
  latitude: number;
  longitude: number;
  layer: "Lost" | "Welfare" | "Clinic";
  canton: string;
  count: number;
  isSuppressed: boolean;
}

export interface NalaTrendPointDto {
  date: string;
  lostReports: number;
  reunitedPets: number;
  captures: number;
  adoptedAnimals: number;
  welfareCases: number;
}

export const nalaApi = {
  getOverview: (periodStart: string, periodEnd: string) =>
    apiClient
      .get<NalaOverviewDto>("/nala/overview", {
        params: { periodStart, periodEnd },
      })
      .then((response) => response.data),

  getMapLayers: (bounds: {
    south: number;
    north: number;
    west: number;
    east: number;
  }) =>
    apiClient
      .get<NalaMapCellDto[]>("/nala/map-layers", { params: bounds })
      .then((response) => response.data),

  getTrends: (periodStart: string, periodEnd: string, canton?: string) =>
    apiClient
      .get<NalaTrendPointDto[]>("/nala/trends", {
        params: { periodStart, periodEnd, canton },
      })
      .then((response) => response.data),

  getCatalog: () =>
    apiClient
      .get<ReportDefinitionDto[]>("/institutional/reports/catalog", {
        params: { scope: "Institutional" },
      })
      .then((response) => response.data),

  getExports: () =>
    apiClient
      .get<{ items: RegulatoryExportDto[] }>("/institutional/reports/exports", {
        params: { page: 1, pageSize: 50 },
      })
      .then((response) => response.data),

  requestExport: (payload: {
    reportType: ReportType;
    scope: ExportScope;
    format: ExportFormat;
    periodStart: string;
    periodEnd: string;
    canton?: string;
    organizationId?: string;
    idempotencyKey: string;
  }) =>
    apiClient
      .post<RegulatoryExportDto>("/institutional/reports/exports", payload)
      .then((response) => response.data),

  generateExport: (id: string) =>
    apiClient
      .post<RegulatoryExportDto>(
        `/institutional/reports/exports/${id}/generate`,
      )
      .then((response) => response.data),

  downloadExport: async (id: string, fileName: string) => {
    const response = await apiClient.get<Blob>(
      `/institutional/reports/exports/${id}/download`,
      {
        responseType: "blob",
      },
    );
    const url = URL.createObjectURL(response.data);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  },
};
