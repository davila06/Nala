import { test, expect } from "@playwright/test";
import {
  apiLogin,
  createPet,
  generateHandoverCode,
  getPublicPetProfile,
  getMaskedChatMessages,
  getLostPetBroadcastStatus,
  openMaskedChat,
  reportLostPet,
  reuniteLostPet,
  sendMaskedChatMessage,
  triggerLostPetBroadcast,
  verifyHandoverCode,
} from "./fixtures/api-setup";
import { TEST_USERS } from "./fixtures/env";

test.describe("Recovery cycle — QR to reunification", () => {
  test("owner reports loss, public QR shows alert, and secure handover reunites pet", async ({
    page,
    request,
  }) => {
    const ownerToken = await apiLogin(
      request,
      TEST_USERS.owner.email,
      TEST_USERS.owner.password,
    );
    const rescuerToken = await apiLogin(
      request,
      TEST_USERS.provider.email,
      TEST_USERS.provider.password,
    );
    const petId = await createPet(
      request,
      ownerToken,
      `E2E Recovery ${Date.now()}`,
    );
    const lostEventId = await reportLostPet(request, ownerToken, petId);
    await triggerLostPetBroadcast(request, ownerToken, lostEventId);
    expect(
      await getLostPetBroadcastStatus(request, ownerToken, lostEventId),
    ).toBeTruthy();

    const profile = await getPublicPetProfile(request, petId);
    expect(profile.id).toBe(petId);
    expect(profile.status).toBe("Lost");

    const threadId = await openMaskedChat(request, rescuerToken, lostEventId);
    await sendMaskedChatMessage(
      request,
      rescuerToken,
      threadId,
      "Vi a la mascota cerca del parque.",
    );
    const messages = await getMaskedChatMessages(request, ownerToken, threadId);
    expect(
      messages.some((message) => message.body.includes("cerca del parque")),
    ).toBe(true);
    expect(
      messages.some((message) => message.body.includes("+50688880000")),
    ).toBe(false);

    await page.goto(`/p/${petId}`);
    await expect(page.getByText(/mascota perdida/i)).toBeVisible();
    await expect(page).not.toHaveURL(/contactPhone|contactName/);
    await expect(page.locator("body")).not.toContainText("+50688880000");

    const code = await generateHandoverCode(request, ownerToken, lostEventId);
    expect(code).toMatch(/^\d{4}$/);
    await verifyHandoverCode(request, rescuerToken, lostEventId, code);
    await reuniteLostPet(request, ownerToken, lostEventId);

    const finalProfile = await getPublicPetProfile(request, petId);
    expect(finalProfile.status).toBe("Reunited");
  });
});
