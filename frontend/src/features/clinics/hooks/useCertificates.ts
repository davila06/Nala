import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  certificateApi,
  type IssueCertificateRequest,
  type IssueVaccinePassportRequest,
} from "../api/certificateApi";

export function useCertificatesForPet(petId: string) {
  return useQuery({
    queryKey: ["certificates", petId],
    queryFn: () => certificateApi.getForPet(petId),
    enabled: !!petId,
  });
}

export function useCertificatesForClinic(clinicId: string, page = 1) {
  return useQuery({
    queryKey: ["certificates", "clinic", clinicId, page],
    queryFn: () => certificateApi.getForClinic(clinicId, page),
    enabled: !!clinicId,
    staleTime: 30_000,
  });
}

export function useIssueCertificate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: IssueCertificateRequest) =>
      certificateApi.issue(request),
    onSuccess: (_data, req) => {
      void queryClient.invalidateQueries({
        queryKey: ["certificates", req.petId],
      });
    },
  });
}

export function useIssueVaccinePassport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: IssueVaccinePassportRequest) =>
      certificateApi.issuePassport(request),
    onSuccess: (_data, req) => {
      void queryClient.invalidateQueries({
        queryKey: ["certificates", req.petId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["certificates", "clinic", req.clinicId],
      });
    },
  });
}

export function useCertificateIssuers() {
  return useQuery({
    queryKey: ["certificate-issuers"],
    queryFn: () => certificateApi.getCertificateIssuers(),
    staleTime: 30_000,
  });
}

export function useMyClinicVerification() {
  return useQuery({
    queryKey: ["clinic-verification"],
    queryFn: () => certificateApi.getMyVerification(),
    staleTime: 30_000,
  });
}

export function useSubmitClinicVerification() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => certificateApi.submitVerification(),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinic-verification"] });
      void queryClient.invalidateQueries({ queryKey: ["certificate-issuers"] });
    },
  });
}

export function useUploadClinicVerificationDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => certificateApi.uploadVerificationDocument(file),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["clinic-verification"] });
      void queryClient.invalidateQueries({ queryKey: ["certificate-issuers"] });
    },
  });
}

export function useMyClinicVeterinarians() {
  return useQuery({
    queryKey: ["clinic-veterinarians"],
    queryFn: () => certificateApi.getMyVeterinarians(),
    staleTime: 30_000,
  });
}

export function useCreateVeterinarian() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: { fullName: string; licenseNumber: string }) =>
      certificateApi.createVeterinarian(
        request.fullName,
        request.licenseNumber,
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["certificate-issuers"] });
      void queryClient.invalidateQueries({
        queryKey: ["clinic-veterinarians"],
      });
    },
  });
}

export function useUploadVeterinarianDocument() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      veterinarianId,
      file,
    }: {
      veterinarianId: string;
      file: File;
    }) => certificateApi.uploadVeterinarianDocument(veterinarianId, file),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinic-veterinarians"],
      });
      void queryClient.invalidateQueries({ queryKey: ["certificate-issuers"] });
    },
  });
}

export function useUploadVeterinarianSignature() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      veterinarianId,
      file,
    }: {
      veterinarianId: string;
      file: File;
    }) => certificateApi.uploadVeterinarianSignature(veterinarianId, file),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinic-veterinarians"],
      });
      void queryClient.invalidateQueries({ queryKey: ["certificate-issuers"] });
    },
  });
}

export function useRevokeVeterinarian() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      veterinarianId,
      reason,
    }: {
      veterinarianId: string;
      reason: string;
    }) => certificateApi.revokeVeterinarian(veterinarianId, reason),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["clinic-veterinarians"],
      });
      void queryClient.invalidateQueries({ queryKey: ["certificate-issuers"] });
    },
  });
}

export function useDownloadCertificatePdf() {
  return useMutation({
    mutationFn: (certificateId: string) =>
      certificateApi.downloadPdf(certificateId),
  });
}
