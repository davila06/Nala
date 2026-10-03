import fs from "node:fs/promises";
import path from "node:path";
import sharp from "sharp";

const root = path.join(process.cwd(), "public", "assets", "landing");
const images = [
  {
    name: "nala-hero-family-reunion-20261001",
    source: "nala-hero-family-reunion-20261001.png",
    mobilePosition: "right",
  },
  {
    name: "nala-services-care-directory-20261001",
    source: "nala-services-care-directory-20261001.png",
    mobilePosition: "centre",
  },
  {
    name: "nala-qr-scan-identity-20261001",
    source: "nala-qr-scan-identity-20261001.png",
    mobilePosition: "centre",
  },
  {
    name: "nala-qr-scan-identity-tagged-20261001",
    source: "nala-qr-scan-identity-tagged-20261001.png",
    mobilePosition: "centre",
  },
  {
    name: "service-veterinary-20261001",
    source: "service-veterinary-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "service-grooming-20261001",
    source: "service-grooming-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "service-training-20261001",
    source: "service-training-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "service-boarding-20261001",
    source: "service-boarding-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "service-walking-20261001",
    source: "service-walking-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "service-temporary-care-20261001",
    source: "service-temporary-care-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "service-booking-request-20261001",
    source: "service-booking-request-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "service-provider-verification-20261001",
    source: "service-provider-verification-20261001.png",
    mobileHeight: 608,
  },
  {
    name: "lost-pet-preparation-20261002",
    source: "lost-pet-preparation-20261002.png",
    mobileHeight: 608,
  },
  {
    name: "lost-pet-approximate-area-20261002",
    source: "lost-pet-approximate-area-20261002.png",
    mobileHeight: 608,
  },
  {
    name: "lost-pet-contact-review-20261002",
    source: "lost-pet-contact-review-20261002.png",
    mobileHeight: 608,
  },
  {
    name: "business-clinics-20261002",
    source: "business-clinics-20261002.png",
  },
  {
    name: "business-shelters-20261002",
    source: "business-shelters-20261002.png",
  },
  {
    name: "shelter-profiles-20261002",
    source: "shelter-profiles-20261002.png",
    mobileHeight: 608,
  },
  {
    name: "shelter-adoptions-20261002",
    source: "shelter-adoptions-20261002.png",
    mobileHeight: 608,
  },
  {
    name: "shelter-partnerships-20261002",
    source: "shelter-partnerships-20261002.png",
    mobileHeight: 608,
  },
  {
    name: "business-municipalities-20261002",
    source: "business-municipalities-20261002.png",
  },
  {
    name: "clinic-identification-20261002",
    source: "clinic-identification-20261002.png",
  },
  {
    name: "clinic-records-20261002",
    source: "clinic-records-20261002.png",
  },
  {
    name: "clinic-appointments-20261002",
    source: "clinic-appointments-20261002.png",
  },
  {
    name: "clinic-scan-stats-20261002",
    source: "clinic-scan-stats-20261002.png",
  },
  {
    name: "municipal-profiles-captures-20261002",
    source: "municipal-profiles-captures-20261002.png",
  },
  {
    name: "municipal-internal-reports-20261002",
    source: "municipal-internal-reports-20261002.png",
  },
  {
    name: "municipal-integration-dialogue-20261002",
    source: "municipal-integration-dialogue-20261002.png",
  },
];
const variants = [
  { directory: "desktop", width: 1600, height: 900, position: "centre" },
  { directory: "mobile", width: 1080, height: 1350 },
];

for (const image of images) {
  const sourcePath = path.join(root, "source", image.source);
  await fs.access(sourcePath);

  const imageVariants = variants.map((variant) =>
    variant.directory === "mobile" && image.mobileHeight
      ? { ...variant, height: image.mobileHeight }
      : variant,
  );

  for (const variant of imageVariants) {
    const outputDirectory = path.join(root, variant.directory);
    const position = variant.position ?? image.mobilePosition;
    const resized = sharp(sourcePath).resize(variant.width, variant.height, {
      fit: "cover",
      position,
    });
    await fs.mkdir(outputDirectory, { recursive: true });

    for (const format of ["avif", "webp"]) {
      const outputPath = path.join(
        outputDirectory,
        `${image.name}-${variant.directory}.${format}`,
      );
      const output = resized.clone();

      if (format === "avif") {
        await output.avif({ quality: 55, effort: 6 }).toFile(outputPath);
      } else {
        await output.webp({ quality: 78, effort: 6 }).toFile(outputPath);
      }

      const { size } = await fs.stat(outputPath);
      console.log(
        `${outputPath}: ${variant.width}x${variant.height}, ${size} bytes`,
      );
    }
  }
}
