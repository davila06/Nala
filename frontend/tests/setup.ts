import "@testing-library/jest-dom";
import { afterAll, afterEach, beforeAll, vi } from "vitest";
import { server } from "./mocks/server";

class MockIntersectionObserver implements IntersectionObserver {
  readonly root = null;
  readonly rootMargin = "";
  readonly thresholds: readonly number[] = [];

  constructor() {}

  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }
}

window.IntersectionObserver = MockIntersectionObserver;
globalThis.IntersectionObserver = MockIntersectionObserver;

if (!HTMLElement.prototype.scrollIntoView) {
  HTMLElement.prototype.scrollIntoView = vi.fn();
}

const originalConsoleError = console.error;
const isAggregateNetworkError = (value: unknown): boolean =>
  value instanceof AggregateError || (value instanceof Error && value.name === "AggregateError");

console.error = (...args: unknown[]) => {
  const isAggregateNetworkNoise = args.some((arg) => {
    if (typeof arg === "string") return arg.includes("AggregateError");
    return isAggregateNetworkError(arg);
  });
  if (!isAggregateNetworkNoise) originalConsoleError(...args);
};

type StderrWriteArgs = [
  encoding?: BufferEncoding | ((error?: Error | null) => void),
  callback?: (error?: Error | null) => void,
];

const originalStderrWrite = process.stderr.write.bind(process.stderr) as (
  chunk: string | Uint8Array,
  ...args: StderrWriteArgs
) => boolean;
process.stderr.write = (chunk: string | Uint8Array, ...args: StderrWriteArgs) => {
  const text = typeof chunk === "string" ? chunk : Buffer.from(chunk).toString("utf8");
  if (text.includes("AggregateError")) return true;
  return originalStderrWrite(chunk, ...args);
};

beforeAll(() => {
  server.listen({ onUnhandledRequest: "bypass" });

  // jsdom surfaces rejected requests as noisy AggregateError events. Tests still
  // assert request outcomes through MSW; suppress only the browser-level noise.
  window.addEventListener("error", (event) => {
    if (isAggregateNetworkError(event.error)) {
      event.preventDefault();
    }
  });
  window.addEventListener("unhandledrejection", (event) => {
    if (isAggregateNetworkError(event.reason)) {
      event.preventDefault();
    }
  });
});
afterEach(() => {
  server.resetHandlers();
  window.localStorage.clear();
});
afterAll(() => server.close());
