import { describe, expect, it } from "vitest";
import { BILLBOARD_PLACEMENTS } from "@/features/advertising/api/billboardsApi";

describe("BILLBOARD_PLACEMENTS", () => {
  it("includes every sellable in-app inventory surface", () => {
    expect(BILLBOARD_PLACEMENTS).toEqual([
      "Map",
      "Dashboard",
      "Directory",
      "Feed",
      "PublicPetProfile",
      "ScanHistory",
      "CaseRoom",
      "ClinicDirectory",
      "ClinicProfile",
      "ServiceProviderDirectory",
      "ServiceProviderProfile",
      "AdoptionDirectory",
      "AdoptionFair",
      "PetRegistration",
      "CollarActivation",
    ]);
  });
});
