import fs from "node:fs/promises";
import path from "node:path";
import QRCode from "qrcode";
import sharp from "sharp";

const root = path.join(process.cwd(), "public", "assets", "landing");
const source = path.join(root, "source", "nala-qr-scan-identity-20261001.png");
const output = path.join(
  root,
  "source",
  "nala-qr-scan-identity-tagged-20261001.png",
);
const qr = await QRCode.toBuffer("NALA DEMO", {
  type: "png",
  errorCorrectionLevel: "H",
  margin: 1,
  width: 84,
  color: { dark: "#14251d", light: "#ffffff" },
});

await fs.access(source);
await sharp(source)
  .composite([{ input: qr, left: 1016, top: 759 }])
  .png()
  .toFile(output);

const { size } = await fs.stat(output);
console.log(
  `${output}: QR payload "NALA DEMO" (no profile URL), ${size} bytes`,
);
