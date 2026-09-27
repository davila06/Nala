import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "@/shared/lib/toast";
import { clinicsApi } from "../api/clinicsApi";

const activeSiteQueryKey = ["clinics", "active-site"] as const;

interface ClinicSiteOption {
  clinicId: string;
}

export function useActiveClinicSite(workspaces: ClinicSiteOption[], isLoadingWorkspaces: boolean) {
  const queryClient = useQueryClient();
  const [selectedClinicId, setSelectedClinicId] = useState("");
  const [isSiteReady, setIsSiteReady] = useState(false);
  const { data: activeSite, isLoading: isLoadingActiveSite } = useQuery({
    queryKey: activeSiteQueryKey,
    queryFn: clinicsApi.getActiveClinicSite,
  });

  useEffect(() => {
    if (isLoadingWorkspaces || isLoadingActiveSite) return;

    let cancelled = false;
    const preferredWorkspace = workspaces.find((workspace) => workspace.clinicId === activeSite?.clinicId);
    const workspace = preferredWorkspace ?? workspaces[0];

    if (!workspace) {
      setSelectedClinicId("");
      setIsSiteReady(true);
      return;
    }

    const activateWorkspace = async () => {
      setIsSiteReady(false);
      try {
        if (activeSite?.clinicId !== workspace.clinicId) await clinicsApi.selectActiveClinicSite(workspace.clinicId);

        if (cancelled) return;
        setSelectedClinicId(workspace.clinicId);
        setIsSiteReady(true);
        queryClient.setQueryData(activeSiteQueryKey, { clinicId: workspace.clinicId });
      } catch {
        if (cancelled) return;
        setIsSiteReady(true);
        toast.error("No se pudo seleccionar la sede clínica.");
      }
    };

    void activateWorkspace();
    return () => {
      cancelled = true;
    };
  }, [activeSite?.clinicId, isLoadingActiveSite, isLoadingWorkspaces, queryClient, workspaces]);

  const selectClinic = async (clinicId: string) => {
    if (!clinicId || clinicId === selectedClinicId) return;

    setIsSiteReady(false);
    try {
      await clinicsApi.selectActiveClinicSite(clinicId);
      if (selectedClinicId)
        queryClient.removeQueries({
          predicate: (query) => query.queryKey[0] === "clinics" && query.queryKey.includes(selectedClinicId),
        });
      void queryClient.invalidateQueries({ queryKey: ["my-clinic"] });
      setSelectedClinicId(clinicId);
      queryClient.setQueryData(activeSiteQueryKey, { clinicId });
    } catch {
      toast.error("No se pudo cambiar la sede activa.");
    } finally {
      setIsSiteReady(true);
    }
  };

  return { activeClinicId: selectedClinicId, isSiteReady, selectClinic };
}
