import Image from "next/image";

type LandingAsset = "hero" | "services" | "qr" | "dogtag";

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
