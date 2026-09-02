import "@testing-library/jest-dom";
import { afterAll, afterEach, beforeAll, vi } from "vitest";
import { server } from "./mocks/server";

class MockIntersectionObserver {
  root = null;
  rootMargin = "";
  thresholds = [];

  observe() {}
  unobserve() {}
  disconnect() {}
  takeRecords() {
    return [];
  }
}

if (!("IntersectionObserver" in window)) {
  (
    window as Window &
      typeof globalThis & {
        IntersectionObserver: typeof MockIntersectionObserver;
      }
  ).IntersectionObserver =
    MockIntersectionObserver as unknown as typeof IntersectionObserver;
}

if (!("IntersectionObserver" in globalThis)) {
  (
    globalThis as typeof globalThis & {
      IntersectionObserver: typeof MockIntersectionObserver;
    }
  ).IntersectionObserver =
    MockIntersectionObserver as unknown as typeof IntersectionObserver;
}

if (!HTMLElement.prototype.scrollIntoView) {
  HTMLElement.prototype.scrollIntoView = vi.fn();
}

beforeAll(() => server.listen({ onUnhandledRequest: "warn" }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
