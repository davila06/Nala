import type { MetadataRoute } from "next";
import { getSiteUrl } from "../lib/site-config";

export const dynamic = "force-static";

export default function robots(): MetadataRoute.Robots {
  const siteUrl = getSiteUrl();

  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/dashboard/", "/register/", "/lost-pets/report/", "/found-pets/report/"],
    },
    sitemap: new URL("/sitemap.xml", siteUrl).toString(),
  };
}
