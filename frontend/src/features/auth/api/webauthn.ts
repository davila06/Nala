import { authApi, type PublicKeyCredentialJSON } from "./authApi";

function decodeBase64Url(value: string): ArrayBuffer {
  const normalized = value.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "=");
  const bytes = Uint8Array.from(atob(padded), (char) => char.charCodeAt(0));
  return bytes.buffer;
}

function encodeBase64Url(value: ArrayBuffer): string {
  const bytes = new Uint8Array(value);
  let binary = "";
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });
  return btoa(binary)
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

function credentialToJson(credential: Credential): PublicKeyCredentialJSON {
  const publicKey = credential as PublicKeyCredential;
  const response = publicKey.response as
    | AuthenticatorAttestationResponse
    | AuthenticatorAssertionResponse;
  const json: PublicKeyCredentialJSON = {
    id: credential.id,
    rawId: encodeBase64Url(publicKey.rawId),
    type: credential.type,
    response: {
      clientDataJSON: encodeBase64Url(response.clientDataJSON),
    },
  };
  if ("attestationObject" in response) {
    json.response.attestationObject = encodeBase64Url(
      response.attestationObject,
    );
  } else {
    json.response.authenticatorData = encodeBase64Url(
      response.authenticatorData,
    );
    json.response.signature = encodeBase64Url(response.signature);
    if (response.userHandle)
      json.response.userHandle = encodeBase64Url(response.userHandle);
  }
  return json;
}

export async function registerPasskey(deviceName?: string) {
  if (!window.PublicKeyCredential)
    throw new Error("WebAuthn no está disponible en este navegador.");
  const { data } = await authApi.webauthnRegisterOptions();
  const rawOptions = data as unknown as {
    challenge: string;
    user: PublicKeyCredentialUserEntity & { id: string };
    excludeCredentials?: Array<PublicKeyCredentialDescriptor & { id: string }>;
    [key: string]: unknown;
  };
  const publicKey = {
    ...rawOptions,
    challenge: decodeBase64Url(rawOptions.challenge),
    user: { ...rawOptions.user, id: decodeBase64Url(rawOptions.user.id) },
    excludeCredentials: (rawOptions.excludeCredentials ?? []).map((item) => ({
      ...item,
      id: decodeBase64Url(item.id as unknown as string),
    })),
  } as PublicKeyCredentialCreationOptions;
  const credential = await navigator.credentials.create({ publicKey });
  if (!credential) throw new Error("No se pudo registrar la passkey.");
  return authApi.webauthnRegister({
    response: credentialToJson(credential),
    deviceName,
  });
}

export async function authenticateWithPasskey(email: string) {
  if (!window.PublicKeyCredential)
    throw new Error("WebAuthn no está disponible en este navegador.");
  const { data } = await authApi.webauthnAuthenticateOptions(email);
  const rawOptions = data as unknown as {
    challenge: string;
    allowCredentials?: Array<PublicKeyCredentialDescriptor & { id: string }>;
    [key: string]: unknown;
  };
  const publicKey = {
    ...rawOptions,
    challenge: decodeBase64Url(rawOptions.challenge),
    allowCredentials: (rawOptions.allowCredentials ?? []).map((item) => ({
      ...item,
      id: decodeBase64Url(item.id as unknown as string),
    })),
  } as PublicKeyCredentialRequestOptions;
  const credential = await navigator.credentials.get({ publicKey });
  if (!credential) throw new Error("No se pudo autenticar con la passkey.");
  return authApi.webauthnAuthenticate({
    email,
    response: credentialToJson(credential),
  });
}
