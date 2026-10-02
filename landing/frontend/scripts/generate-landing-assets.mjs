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
