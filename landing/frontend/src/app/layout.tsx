import type { Metadata } from "next";
import Link from "next/link";
import { getSiteUrl } from "@/lib/site-config";
import "./globals.css";

export const metadata: Metadata = {
  metadataBase: getSiteUrl(),
  title: {
    default: "PawTrack CR | NALA, identidad y cuidado animal",
    template: "%s | PawTrack CR · NALA",
  },
  description:
    "PawTrack CR, NALA: identidad digital, información y flujos de recuperación y cuidado animal para Costa Rica.",
  applicationName: "NALA",
  category: "Pets and animal welfare",
  openGraph: {
    type: "website",
    locale: "es_CR",
    siteName: "PawTrack CR · NALA",
    title: "PawTrack CR | NALA, identidad y cuidado animal",
    description: "Una identidad digital para cuidar, reconocer y ayudar a que cada mascota vuelva a casa.",
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
        <Link className="skip-link" href="#main">
          Saltar al contenido principal
        </Link>
        {children}
      </body>
    </html>
  );
}
