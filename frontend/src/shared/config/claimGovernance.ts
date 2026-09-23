export type ApprovedClaim = "recovery" | "enterprise" | "senasa-ready" | "verified-provider";

export function parseApprovedClaims(raw: string | undefined): ReadonlySet<string> {
  return new Set(
    (raw ?? "")
      .split(",")
      .map((claim) => claim.trim().toLowerCase())
      .filter(Boolean),
  );
}

const approvedClaims = parseApprovedClaims(import.meta.env.VITE_APPROVED_CLAIMS as string | undefined);

export function isClaimApproved(claim: ApprovedClaim): boolean {
  return approvedClaims.has(claim);
}
