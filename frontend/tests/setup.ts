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
console.error = (...args: unknown[]) => {
  const isAggregateNetworkNoise = args.some((arg) => {
    if (arg instanceof AggregateError) return true;
    if (typeof arg === "string") return arg.includes("AggregateError");
    return arg instanceof Error && arg.name === "AggregateError";
  });
  if (!isAggregateNetworkNoise) originalConsoleError(...args);
};

const originalStderrWrite = process.stderr.write.bind(process.stderr);
process.stderr.write = ((chunk: string | Uint8Array, ...args: unknown[]) => {
  const text = typeof chunk === "string" ? chunk : Buffer.from(chunk).toString("utf8");
  if (text.includes("AggregateError")) return true;
  return originalStderrWrite(chunk, ...(args as []));
}) as typeof process.stderr.write;

beforeAll(() => {
  server.listen({ onUnhandledRequest: "bypass" });

  // jsdom surfaces rejected requests as noisy AggregateError events. Tests still
  // assert request outcomes through MSW; suppress only the browser-level noise.
  window.addEventListener("error", (event) => {
    if (event.error instanceof AggregateError || event.error?.name === "AggregateError") {
      event.preventDefault();
    }
  });
  window.addEventListener("unhandledrejection", (event) => {
    if (event.reason instanceof AggregateError || event.reason?.name === "AggregateError") {
      event.preventDefault();
    }
  });
});
afterEach(() => {
  server.resetHandlers();
  window.localStorage.clear();
});
afterAll(() => server.close());
