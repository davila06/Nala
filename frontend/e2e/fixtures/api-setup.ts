import type { APIRequestContext } from "@playwright/test";
import { API_URL } from "./env";

/** Logs a seeded test user in via the real API and returns the JWT access token. */
export async function apiLogin(
  request: APIRequestContext,
  email: string,
  password: string,
): Promise<string> {
  const res = await request.post(`${API_URL}/api/auth/login`, {
    data: { email, password },
  });
  if (!res.ok()) {
    throw new Error(
      `apiLogin failed for ${email}: ${res.status()} ${await res.text()}`,
    );
  }
  const body = (await res.json()) as { accessToken: string };
  return body.accessToken;
}

function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}` };
}

/** Creates a bare-minimum pet (no photo) owned by the given token's user. */
export async function createPet(
  request: APIRequestContext,
  ownerToken: string,
  name: string,
): Promise<string> {
  const res = await request.post(`${API_URL}/api/pets`, {
    headers: authHeaders(ownerToken),
    multipart: { name, species: "Dog" },
  });
  if (res.status() !== 201) {
    throw new Error(`createPet failed: ${res.status()} ${await res.text()}`);
  }
  const body = (await res.json()) as { petId: string };
  return body.petId;
}

/** Reports a pet lost and returns the real lost-event ID. */
export async function reportLostPet(
  request: APIRequestContext,
  ownerToken: string,
  petId: string,
): Promise<string> {
  const res = await request.post(`${API_URL}/api/lost-pets`, {
    headers: authHeaders(ownerToken),
    multipart: {
      petId,
      description: "E2E recovery flow",
      publicMessage: "E2E recovery flow",
      lastSeenAt: new Date().toISOString(),
      lastSeenLat: "9.935",
      lastSeenLng: "-84.091",
      contactName: "E2E Owner",
      contactPhone: "+50688880000",
    },
  });
  if (res.status() !== 201) {
    throw new Error(
      `reportLostPet failed: ${res.status()} ${await res.text()}`,
    );
  }
  const body = (await res.json()) as { id: string };
  return body.id;
}

export async function getPublicPetProfile(
  request: APIRequestContext,
  petId: string,
): Promise<{ id: string; status: string; name: string }> {
  const res = await request.get(`${API_URL}/api/public/pets/${petId}`);
  if (!res.ok()) {
    throw new Error(
      `getPublicPetProfile failed: ${res.status()} ${await res.text()}`,
    );
  }
  return (await res.json()) as { id: string; status: string; name: string };
}

export async function generateHandoverCode(
  request: APIRequestContext,
  ownerToken: string,
  lostEventId: string,
): Promise<string> {
  const res = await request.post(
    `${API_URL}/api/lost-pets/${lostEventId}/handover/code`,
    {
      headers: authHeaders(ownerToken),
    },
  );
  if (!res.ok()) {
    throw new Error(
      `generateHandoverCode failed: ${res.status()} ${await res.text()}`,
    );
  }
  return ((await res.json()) as { code: string }).code;
}

export async function verifyHandoverCode(
  request: APIRequestContext,
  rescuerToken: string,
  lostEventId: string,
  code: string,
): Promise<void> {
  const res = await request.post(
    `${API_URL}/api/lost-pets/${lostEventId}/handover/verify`,
    {
      headers: authHeaders(rescuerToken),
      data: { code },
    },
  );
  if (!res.ok()) {
    throw new Error(
      `verifyHandoverCode failed: ${res.status()} ${await res.text()}`,
    );
  }
}

export async function openMaskedChat(
  request: APIRequestContext,
  finderToken: string,
  lostEventId: string,
): Promise<string> {
  const res = await request.post(`${API_URL}/api/chat/threads`, {
    headers: authHeaders(finderToken),
    data: { lostPetEventId: lostEventId },
  });
  if (!res.ok()) {
    throw new Error(
      `openMaskedChat failed: ${res.status()} ${await res.text()}`,
    );
  }
  return ((await res.json()) as { threadId: string }).threadId;
}

export async function sendMaskedChatMessage(
  request: APIRequestContext,
  finderToken: string,
  threadId: string,
  body: string,
): Promise<void> {
  const res = await request.post(
    `${API_URL}/api/chat/threads/${threadId}/messages`,
    {
      headers: authHeaders(finderToken),
      data: { body },
    },
  );
  if (!res.ok()) {
    throw new Error(
      `sendMaskedChatMessage failed: ${res.status()} ${await res.text()}`,
    );
  }
}

export async function getMaskedChatMessages(
  request: APIRequestContext,
  ownerToken: string,
  threadId: string,
): Promise<Array<{ body: string; isFromMe: boolean }>> {
  const res = await request.get(
    `${API_URL}/api/chat/threads/${threadId}/messages`,
    {
      headers: authHeaders(ownerToken),
    },
  );
  if (!res.ok()) {
    throw new Error(
      `getMaskedChatMessages failed: ${res.status()} ${await res.text()}`,
    );
  }
  return (await res.json()) as Array<{ body: string; isFromMe: boolean }>;
}

export async function reuniteLostPet(
  request: APIRequestContext,
  ownerToken: string,
  lostEventId: string,
): Promise<void> {
  const res = await request.put(
    `${API_URL}/api/lost-pets/${lostEventId}/status`,
    {
      headers: authHeaders(ownerToken),
      data: { newStatus: "Reunited" },
    },
  );
  if (!res.ok()) {
    throw new Error(
      `reuniteLostPet failed: ${res.status()} ${await res.text()}`,
    );
  }
}

export async function triggerLostPetBroadcast(
  request: APIRequestContext,
  ownerToken: string,
  lostEventId: string,
): Promise<void> {
  const res = await request.post(
    `${API_URL}/api/broadcast/lost-pets/${lostEventId}`,
    {
      headers: authHeaders(ownerToken),
    },
  );
  if (!res.ok()) {
    throw new Error(
      `triggerLostPetBroadcast failed: ${res.status()} ${await res.text()}`,
    );
  }
}

export async function getLostPetBroadcastStatus(
  request: APIRequestContext,
  ownerToken: string,
  lostEventId: string,
): Promise<unknown> {
  const res = await request.get(
    `${API_URL}/api/broadcast/lost-pets/${lostEventId}`,
    {
      headers: authHeaders(ownerToken),
    },
  );
  if (!res.ok()) {
    throw new Error(
      `getLostPetBroadcastStatus failed: ${res.status()} ${await res.text()}`,
    );
  }
  return res.json();
}

export async function createClinicApiKey(
  request: APIRequestContext,
  clinicToken: string,
  label = `E2E ${Date.now()}`,
): Promise<{ id: string; key: string }> {
  const res = await request.post(`${API_URL}/api/clinics/me/api-keys`, {
    headers: authHeaders(clinicToken),
    data: { label, scopes: ["scan"] },
  });
  if (!res.ok())
    throw new Error(
      `createClinicApiKey failed: ${res.status()} ${await res.text()}`,
    );
  const body = (await res.json()) as { id: string; rawKey?: string };
  if (!body.rawKey)
    throw new Error("createClinicApiKey did not return the one-time raw key.");
  return { id: body.id, key: body.rawKey };
}

export async function rotateClinicApiKey(
  request: APIRequestContext,
  clinicToken: string,
  keyId: string,
): Promise<{ id: string; key: string }> {
  const res = await request.post(
    `${API_URL}/api/clinics/me/api-keys/${keyId}/rotate`,
    {
      headers: authHeaders(clinicToken),
    },
  );
  if (!res.ok())
    throw new Error(
      `rotateClinicApiKey failed: ${res.status()} ${await res.text()}`,
    );
  const body = (await res.json()) as { id: string; rawKey?: string };
  if (!body.rawKey)
    throw new Error("rotateClinicApiKey did not return the one-time raw key.");
  return { id: body.id, key: body.rawKey };
}

export async function lookupWithClinicApiKey(
  request: APIRequestContext,
  apiKey: string,
  chip = `E2E-NOT-FOUND-${Date.now()}`,
): Promise<number> {
  const res = await request.get(
    `${API_URL}/api/v1/pets/lookup?chip=${encodeURIComponent(chip)}`,
    {
      headers: { "X-PawTrack-Key": apiKey },
    },
  );
  return res.status();
}

export function getB2BSeedStatus(): {
  enabled: boolean;
  reason?: string;
} {
  if (!process.env.E2E_B2B_ENABLED) {
    return { enabled: false, reason: "E2E_B2B_ENABLED is not set." };
  }
  return { enabled: true };
}

/**
 * Grants the owner a UserFamilia subscription (unlimited pets, and satisfies the
 * "Plus" PlanGate too — UserFamilia is a superset tier) so the collar/GPS tab is
 * visible in the UI. UserFamilia (not UserPlus) is used specifically so repeated
 * local runs against a persistent dev database don't eventually hit the Plus
 * tier's 3-pet cap as E2E-created pets accumulate across runs.
 * Idempotent — skips if the owner already has an active Plus/Familia subscription.
 */
export async function grantPlusSubscription(
  request: APIRequestContext,
  ownerToken: string,
  adminToken: string,
): Promise<void> {
  const mine = await request.get(`${API_URL}/api/subscriptions/me`, {
    headers: authHeaders(ownerToken),
  });
  if (mine.ok()) {
    const raw = await mine.text();
    const current = raw
      ? (JSON.parse(raw) as { tier: string; isActive: boolean } | null)
      : null;
    if (
      current?.isActive &&
      (current.tier === "UserPlus" || current.tier === "UserFamilia")
    ) {
      return;
    }
  }

  const create = await request.post(`${API_URL}/api/subscriptions`, {
    headers: authHeaders(ownerToken),
    data: { tier: "UserFamilia" },
  });
  if (create.status() !== 201) {
    throw new Error(
      `create subscription failed: ${create.status()} ${await create.text()}`,
    );
  }
  const sub = (await create.json()) as { id: string };

  const activate = await request.put(
    `${API_URL}/api/subscriptions/admin/${sub.id}/activate`,
    { headers: authHeaders(adminToken), data: { billingMonths: 1 } },
  );
  if (!activate.ok()) {
    throw new Error(
      `activate subscription failed: ${activate.status()} ${await activate.text()}`,
    );
  }
}

/** Registers a fresh, unique collar serial in inventory (admin-only). */
export async function registerCollarSerial(
  request: APIRequestContext,
  adminToken: string,
  serial: string,
): Promise<void> {
  const res = await request.post(`${API_URL}/api/admin/collar-tags`, {
    headers: authHeaders(adminToken),
    data: { serial, firmwareVersion: "1.0.0" },
  });
  if (res.status() !== 201) {
    throw new Error(
      `registerCollarSerial failed: ${res.status()} ${await res.text()}`,
    );
  }
}

/** Activates a registered serial onto a pet and returns the resulting collarId. */
export async function activateCollar(
  request: APIRequestContext,
  ownerToken: string,
  serial: string,
  petId: string,
): Promise<string> {
  const res = await request.post(
    `${API_URL}/api/collars/tag/${serial}/activate`,
    { headers: authHeaders(ownerToken), data: { petId } },
  );
  if (!res.ok()) {
    throw new Error(
      `activateCollar failed: ${res.status()} ${await res.text()}`,
    );
  }

  const status = await request.get(`${API_URL}/api/collars/pet/${petId}`, {
    headers: authHeaders(ownerToken),
  });
  const dto = (await status.json()) as { id: string };
  return dto.id;
}

/** Records a manual GPS ping so `lastLat`/`lastLng` are populated (required to render map-based panels). */
export async function recordManualLocation(
  request: APIRequestContext,
  ownerToken: string,
  petId: string,
  lat: number,
  lng: number,
): Promise<void> {
  const res = await request.post(
    `${API_URL}/api/collars/pet/${petId}/location`,
    { headers: authHeaders(ownerToken), data: { lat, lng } },
  );
  if (!res.ok()) {
    throw new Error(
      `recordManualLocation failed: ${res.status()} ${await res.text()}`,
    );
  }
}

/** Creates a safe zone directly via API (bypasses map click-to-draw for deterministic setup). */
export async function createSafeZone(
  request: APIRequestContext,
  ownerToken: string,
  collarId: string,
  name: string,
  points: { lat: number; lng: number }[],
): Promise<void> {
  const res = await request.post(
    `${API_URL}/api/collars/${collarId}/safe-zones`,
    {
      headers: authHeaders(ownerToken),
      data: { name, polygonJson: JSON.stringify(points) },
    },
  );
  if (!res.ok()) {
    throw new Error(
      `createSafeZone failed: ${res.status()} ${await res.text()}`,
    );
  }
}

let serialCounter = 0;

/** Generates a unique serial matching the domain format PT-[4 hex]-[7 digits]. */
export function uniqueSerial(): string {
  serialCounter += 1;
  const hex = Math.floor(Math.random() * 0xffff)
    .toString(16)
    .toUpperCase()
    .padStart(4, "0");
  const digits = `${Date.now()}${serialCounter}`.slice(-7).padStart(7, "0");
  return `PT-${hex}-${digits}`;
}
