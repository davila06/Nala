import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { describe, expect, it, vi } from "vitest";
import { certificateApi } from "@/features/clinics/api/certificateApi";
import { useCertificatesForPet } from "@/features/clinics/hooks/useCertificates";

vi.mock("@/features/clinics/api/certificateApi", () => ({ certificateApi: { getForPetPage: vi.fn() } }));

describe("useCertificatesForPet", () => {
  it("fetches additional certificates only on request", async () => {
    vi.mocked(certificateApi.getForPetPage)
      .mockResolvedValueOnce({ items: [{ id: "first" }] as never, hasMore: true })
      .mockResolvedValueOnce({ items: [{ id: "second" }] as never, hasMore: false });
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const wrapper = ({ children }: PropsWithChildren) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const { result } = renderHook(() => useCertificatesForPet("pet-1"), { wrapper });
    await waitFor(() => expect(result.current.data?.pages[0].items[0].id).toBe("first"));
    expect(certificateApi.getForPetPage).toHaveBeenCalledTimes(1);
    await act(async () => {
      await result.current.fetchNextPage();
    });
    await waitFor(() =>
      expect(result.current.data?.pages.flatMap((page) => page.items).map((item) => item.id)).toEqual([
        "first",
        "second",
      ]),
    );
  });
});
