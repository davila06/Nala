import Image from "next/image";

type LandingAsset =
  | "hero"
  | "services"
  | "qr"
  | "dogtag"
  | "lostPreparation"
  | "lostArea"
  | "lostContact"
  | "businessClinics"
  | "businessShelters"
  | "businessMunicipalities"
  | "clinicIdentification"
  | "clinicRecords"
  | "clinicAppointments"
  | "clinicScanStats"
  | "municipalProfiles"
  | "municipalReports"
  | "municipalDialogue";

type LandingPictureProps = {
  alt: string;
  asset: LandingAsset;
  className?: string;
  priority?: boolean;
};

const assetFiles: Record<LandingAsset, { desktop: string; mobile: string }> = {
  hero: {
    desktop: "nala-hero-family-reunion-20261001-desktop",
    mobile: "nala-hero-family-reunion-20261001-mobile",
  },
  services: {
    desktop: "nala-services-care-directory-20261001-desktop",
    mobile: "nala-services-care-directory-20261001-mobile",
  },
  qr: {
    desktop: "nala-qr-scan-identity-20261001-desktop",
    mobile: "nala-qr-scan-identity-20261001-mobile",
  },
  dogtag: {
    desktop: "nala-qr-scan-identity-tagged-20261001-desktop",
    mobile: "nala-qr-scan-identity-tagged-20261001-mobile",
  },
  lostPreparation: {
    desktop: "lost-pet-preparation-20261002-desktop",
    mobile: "lost-pet-preparation-20261002-mobile",
  },
  lostArea: {
    desktop: "lost-pet-approximate-area-20261002-desktop",
    mobile: "lost-pet-approximate-area-20261002-mobile",
  },
  lostContact: {
    desktop: "lost-pet-contact-review-20261002-desktop",
    mobile: "lost-pet-contact-review-20261002-mobile",
  },
  businessClinics: {
    desktop: "business-clinics-20261002-desktop",
    mobile: "business-clinics-20261002-mobile",
  },
  businessShelters: {
    desktop: "business-shelters-20261002-desktop",
    mobile: "business-shelters-20261002-mobile",
  },
  businessMunicipalities: {
    desktop: "business-municipalities-20261002-desktop",
    mobile: "business-municipalities-20261002-mobile",
  },
  clinicIdentification: {
    desktop: "clinic-identification-20261002-desktop",
    mobile: "clinic-identification-20261002-mobile",
  },
  clinicRecords: {
    desktop: "clinic-records-20261002-desktop",
    mobile: "clinic-records-20261002-mobile",
  },
  clinicAppointments: {
    desktop: "clinic-appointments-20261002-desktop",
    mobile: "clinic-appointments-20261002-mobile",
  },
  clinicScanStats: {
    desktop: "clinic-scan-stats-20261002-desktop",
    mobile: "clinic-scan-stats-20261002-mobile",
  },
  municipalProfiles: {
    desktop: "municipal-profiles-captures-20261002-desktop",
    mobile: "municipal-profiles-captures-20261002-mobile",
  },
  municipalReports: {
    desktop: "municipal-internal-reports-20261002-desktop",
    mobile: "municipal-internal-reports-20261002-mobile",
  },
  municipalDialogue: {
    desktop: "municipal-integration-dialogue-20261002-desktop",
    mobile: "municipal-integration-dialogue-20261002-mobile",
  },
};

export function LandingPicture({ alt, asset, className, priority = false }: LandingPictureProps) {
  const files = assetFiles[asset];

  return (
    <picture>
      <source media="(max-width: 700px)" srcSet={`/assets/landing/mobile/${files.mobile}.avif`} type="image/avif" />
      <source media="(max-width: 700px)" srcSet={`/assets/landing/mobile/${files.mobile}.webp`} type="image/webp" />
      <source srcSet={`/assets/landing/desktop/${files.desktop}.avif`} type="image/avif" />
      <Image
        alt={alt}
        className={className}
        fetchPriority={priority ? "high" : undefined}
        height={900}
        loading={priority ? "eager" : "lazy"}
        quality={85}
        src={`/assets/landing/desktop/${files.desktop}.webp`}
        width={1600}
      />
    </picture>
  );
}
