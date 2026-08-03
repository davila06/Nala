import { useEffect, useState } from "react";
import { useNavigate, useParams, Link } from "react-router-dom";
import { useAcceptFamilyInvitation } from "@/features/family/hooks/useFamily";
import { useAuthStore } from "@/features/auth/store/authStore";
import { Skeleton } from "@/shared/ui/Spinner";

export default function AcceptFamilyInvitationPage() {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const isAuthenticated = !!useAuthStore((s) => s.accessToken);
  const accept = useAcceptFamilyInvitation();
  const [status, setStatus] = useState<"pending" | "success" | "error">(
    "pending",
  );
  const [errorMsg, setErrorMsg] = useState<string>("");

  useEffect(() => {
    if (!token) {
      setStatus("error");
      setErrorMsg("Enlace de invitación inválido.");
      return;
    }

    // Not logged in → redirect to login, return here after
    if (!isAuthenticated) {
      navigate(
        `/login?redirect=${encodeURIComponent(`/familia/invitacion/${token}`)}`,
        { replace: true },
      );
      return;
    }

    accept.mutate(token, {
      onSuccess: () => {
        setStatus("success");
        setTimeout(() => navigate("/perfil", { replace: true }), 2000);
      },
      onError: (err: unknown) => {
        const apiErr = err as { response?: { data?: { detail?: string } } };
        setStatus("error");
        setErrorMsg(
          apiErr?.response?.data?.detail ?? "No se pudo aceptar la invitación.",
        );
      },
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, isAuthenticated]);

  return (
    <main className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-sm rounded-2xl border border-sand-200 bg-surface-warm p-8 text-center shadow-sm">
        <span className="text-5xl" aria-hidden="true">
          👨‍👩‍👧
        </span>

        {status === "pending" && (
          <div className="mt-6 space-y-3">
            <Skeleton className="mx-auto h-5 w-48 rounded" />
            <p className="text-sm text-sand-500">Procesando tu invitación…</p>
          </div>
        )}

        {status === "success" && (
          <div className="mt-6 space-y-2">
            <p className="text-lg font-display font-semibold text-trust-700">
              ¡Bienvenido a la familia! 🎉
            </p>
            <p className="text-sm text-sand-500">Redirigiendo a tu perfil…</p>
          </div>
        )}

        {status === "error" && (
          <div className="mt-6 space-y-4">
            <p className="text-base font-semibold text-danger-700">
              No pudimos procesar la invitación
            </p>
            <p className="text-sm text-sand-600">{errorMsg}</p>
            <Link
              to="/perfil"
              className="inline-block rounded-xl bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
            >
              Ir a mi perfil
            </Link>
          </div>
        )}
      </div>
    </main>
  );
}
