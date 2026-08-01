import { useMySubscription } from "./useSubscription";

export function useMyTier() {
  const { data: sub } = useMySubscription();

  const tier =
    sub?.status === "Active"
      ? (sub.tier as "UserPlus" | "UserFamilia" | "Explorador")
      : "Explorador";

  return {
    tier,
    isPlus: tier === "UserPlus" || tier === "UserFamilia",
    isFamilia: tier === "UserFamilia",
    isLoading: sub === undefined,
  };
}
