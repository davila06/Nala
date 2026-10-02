import fs from "node:fs/promises";
import path from "node:path";
import sharp from "sharp";

const projectRoot = process.cwd();
const sourcePath = path.join(
  projectRoot,
  "scripts",
  "assets",
  "paw-favicon.svg",
);
const outputPath = path.join(projectRoot, "src", "app", "favicon.ico");
const png = await sharp(sourcePath).resize(48, 48).png().toBuffer();
const header = Buffer.alloc(22);

header.writeUInt16LE(0, 0);
header.writeUInt16LE(1, 2);
header.writeUInt16LE(1, 4);
header.writeUInt8(48, 6);
header.writeUInt8(48, 7);
header.writeUInt16LE(1, 10);
header.writeUInt16LE(32, 12);
header.writeUInt32LE(png.length, 14);
header.writeUInt32LE(header.length, 18);

await fs.writeFile(outputPath, Buffer.concat([header, png]));
console.log(`Generated ${outputPath} (48x48)`);
