function parsePublicOrigin(
  value: string | undefined,
  variableName: string,
): URL {
  if (!value) {
    throw new Error(
      `${variableName} must be configured before building or serving the landing.`,
    );
  }

  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`${variableName} must be a valid absolute URL.`);
  }

  const isLocalHttp =
    url.protocol === "http:" &&
    ["localhost", "127.0.0.1"].includes(url.hostname);
  if (
    (url.protocol !== "https:" && !isLocalHttp) ||
    url.username ||
    url.password ||
    url.pathname !== "/" ||
    url.search ||
    url.hash
  ) {
    throw new Error(
      `${variableName} must be an HTTPS origin (HTTP is allowed only for localhost).`,
    );
  }

  return url;
}

export function getSiteUrl(): URL {
  return parsePublicOrigin(
    process.env.NEXT_PUBLIC_SITE_URL,
    "NEXT_PUBLIC_SITE_URL",
  );
}

export function getProductUrl(path: "/register" | "/encontre-mascota"): string {
  const appUrl = parsePublicOrigin(
    process.env.NEXT_PUBLIC_APP_URL,
    "NEXT_PUBLIC_APP_URL",
  );
  return new URL(path, appUrl).toString();
}
