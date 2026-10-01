import type { MetadataRoute } from "next";
import { editorialArticles } from "../lib/blog-content";
import { getSiteUrl } from "../lib/site-config";

export const dynamic = "force-static";

const publicPaths = [
  "/",
  "/about",
  "/lost-pets",
  "/found-pets",
  "/plans",
  "/services",
  "/features",
  "/pet-id",
  "/qr",
  "/nfc",
  "/telemedicine",
  "/marketplace",
  "/community",
  "/clinics",
  "/shelters",
  "/municipalities",
  "/blog",
  "/business",
  "/contact",
];

export default function sitemap(): MetadataRoute.Sitemap {
  const siteUrl = getSiteUrl();
  const toCanonicalUrl = (path: string) => new URL(path === "/" ? path : `${path}/`, siteUrl).toString();

  const pages: MetadataRoute.Sitemap = publicPaths.map((path) => ({
    url: toCanonicalUrl(path),
    changeFrequency: path === "/" ? "weekly" : "monthly",
    priority: path === "/" ? 1 : 0.7,
  }));

  const articles: MetadataRoute.Sitemap = editorialArticles.map((article) => ({
    url: toCanonicalUrl(`/blog/${article.slug}`),
    changeFrequency: "monthly",
    priority: 0.6,
  }));

  return [...pages, ...articles];
}
