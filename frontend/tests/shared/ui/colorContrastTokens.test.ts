import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const styles = readFileSync(resolve(process.cwd(), "src/styles/globals.css"), "utf8");

function tokenValues(name: string): string[] {
  return [...styles.matchAll(new RegExp(`--color-${name}:\\s*(#[0-9a-fA-F]{6})`, "g"))].map((match) => match[1]);
}

function luminance(hex: string): number {
  const channels = [1, 3, 5].map((offset) => Number.parseInt(hex.slice(offset, offset + 2), 16) / 255);
  const linear = channels.map((value) => (value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4));
  return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
}

function contrastRatio(first: string, second: string): number {
  const values = [luminance(first), luminance(second)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}

describe("semantic text and action color tokens", () => {
  it.each([
    ["copy-muted", "#ffffff", 0],
    ["copy-secondary", "#ffffff", 0],
    ["copy-muted", "#27272f", 1],
    ["copy-secondary", "#27272f", 1],
    ["copy-muted", "#231e1a", 2],
    ["copy-secondary", "#231e1a", 2],
    ["copy-brand", "#ffffff", 0],
    ["copy-brand", "#27272f", 1],
    ["copy-brand", "#231e1a", 2],
  ])("%s meets WCAG AA on its %s surface", (token, background, modeIndex) => {
    const values = tokenValues(token);
    expect(values.length).toBeGreaterThan(modeIndex);
    expect(contrastRatio(values[modeIndex], background)).toBeGreaterThanOrEqual(4.5);
  });

  it("brand-500 supports normal white button text", () => {
    const [brandAction] = tokenValues("brand-500");
    expect(brandAction).toBeDefined();
    expect(contrastRatio(brandAction, "#ffffff")).toBeGreaterThanOrEqual(4.5);
  });

  it("maps native input placeholder text to the accessible muted token", () => {
    expect(styles).toMatch(/--color-ink-tertiary:\s*var\(--color-copy-muted\)/);
  });
});
