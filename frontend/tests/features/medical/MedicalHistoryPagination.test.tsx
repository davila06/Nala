import { act, renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { describe, expect, it, vi } from "vitest";
import { medicalApi } from "@/features/medical/api/medicalApi";
import { useMedicalHistory } from "@/features/medical/hooks/useMedical";

vi.mock("@/features/medical/api/medicalApi", () => ({ medicalApi: { getHistoryPage: vi.fn() } }));

describe("useMedicalHistory", () => {
  it("loads the next medical record page only on request", async () => {
    vi.mocked(medicalApi.getHistoryPage)
      .mockResolvedValueOnce({
        records: [{ id: "first" }] as never,
        totalCount: 2,
        accessTier: "familia",
        isLimited: false,
        previewLimit: null,
        hasMore: true,
      })
      .mockResolvedValueOnce({
        records: [{ id: "second" }] as never,
        totalCount: 2,
        accessTier: "familia",
        isLimited: false,
        previewLimit: null,
        hasMore: false,
      });
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const wrapper = ({ children }: PropsWithChildren) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const { result } = renderHook(() => useMedicalHistory("pet-1"), { wrapper });
    await waitFor(() => expect(result.current.data?.pages[0].records[0].id).toBe("first"));
    expect(medicalApi.getHistoryPage).toHaveBeenCalledTimes(1);

    await act(async () => {
      await result.current.fetchNextPage();
    });
    await waitFor(() =>
      expect(result.current.data?.pages.flatMap((page) => page.records).map((record) => record.id)).toEqual([
        "first",
        "second",
      ]),
    );
    expect(medicalApi.getHistoryPage).toHaveBeenNthCalledWith(2, "pet-1", 2);
  });
});
