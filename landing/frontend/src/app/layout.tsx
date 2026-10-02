import type { Metadata } from "next";
import { AnalyticsBridge } from "@/components/analytics-bridge";
import { DepthInteractions } from "@/components/depth-interactions";
import Link from "next/link";
import { getSiteUrl } from "@/lib/site-config";
import "./globals.css";

export const metadata: Metadata = {
  metadataBase: getSiteUrl(),
  title: {
    default: "NALA | Red de protección para mascotas · PawTrack CR",
    template: "%s | PawTrack CR · NALA",
  },
  description:
    "NALA reúne identidad digital, herramientas de recuperación y recursos para organizar el cuidado de mascotas en PawTrack CR, Costa Rica.",
  alternates: {
    canonical: "/",
  },
  applicationName: "PawTrack CR · NALA",
  category: "Pets and animal welfare",
  openGraph: {
    type: "website",
    locale: "es_CR",
    siteName: "PawTrack CR · NALA",
    title: "NALA | Red de protección para mascotas · PawTrack CR",
    description:
      "El hogar digital de tu mascota: identidad, herramientas de recuperación e información para organizar su cuidado.",
  },
  robots: {
    index: true,
    follow: true,
  },
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es-CR">
      <body>
        <link rel="preconnect" href="https://images.unsplash.com" />
        <Link className="skip-link" href="#main">
          Saltar al contenido principal
        </Link>
        <AnalyticsBridge />
        <DepthInteractions />
        {children}
      </body>
    </html>
  );
}
