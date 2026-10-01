import type { Metadata } from "next";
import Link from "next/link";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";
import { editorialArticles, editorialPillars } from "@/lib/blog-content";

export const metadata: Metadata = {
  title: "Guías para tutores de mascotas",
  description: `${editorialArticles.length} guías prácticas sobre identificación, privacidad, cuidado y recuperación responsable de mascotas en Latinoamérica.`,
  openGraph: {
    title: "Guías NALA para cuidar y ayudar mejor",
    description: "Información clara sobre identificación, privacidad y recuperación de mascotas.",
  },
};

export default function BlogPage() {
  return (
    <>
      <SiteHeader />
      <main className="blog-page" id="main">
        <div className="blog-hero section-shell">
          <nav aria-label="Ruta de navegación" className="breadcrumbs">
            <Link href="/">Inicio</Link><span aria-hidden="true">/</span><span>Guías NALA</span>
          </nav>
          <p className="eyebrow"><span /> RECURSOS PARA TUTORES</p>
          <h1>Ideas claras para cuidar y ayudar <em>mejor.</em></h1>
          <p className="blog-lead">{editorialArticles.length} guías sobre identificación, privacidad, cuidado y recuperación para tomar decisiones con más información en Latinoamérica.</p>
          <p className="blog-editorial-note" role="note">Contenido educativo general. No sustituye la orientación veterinaria profesional; la disponibilidad de servicios NALA aún está en desarrollo.</p>
        </div>

        <nav aria-label="Temas de las guías" className="blog-pillar-nav section-shell">
          {editorialPillars.map((pillar) => (
            <Link href={`#${pillar.id}`} key={pillar.id}>{pillar.title} <span aria-hidden="true">↓</span></Link>
          ))}
        </nav>

        {editorialPillars.map((pillar) => {
          const articles = editorialArticles.filter((article) => pillar.categories.includes(article.category));

          return (
            <section aria-labelledby={`${pillar.id}-title`} className="blog-pillar section-shell" id={pillar.id} key={pillar.id}>
              <div className="blog-pillar-heading">
                <div>
                  <p className="eyebrow">{articles.length} {articles.length === 1 ? "GUÍA" : "GUÍAS"}</p>
                  <h2 id={`${pillar.id}-title`}>{pillar.title}</h2>
                </div>
                <p>{pillar.description}</p>
              </div>
              <div aria-label={`Guías de ${pillar.title}`} className="blog-pillar-grid">
                {articles.map((article) => (
                  <article className="blog-index-entry" key={article.slug}>
                    <p className="guide-category">{article.category}</p>
                    <h3><Link href={`/blog/${article.slug}`}>{article.title}</Link></h3>
                    <p>{article.summary}</p>
                    <Link aria-label={`Leer guía: ${article.title}`} className="guide-read-link" href={`/blog/${article.slug}`}>Leer guía <span aria-hidden="true">↗</span></Link>
                  </article>
                ))}
              </div>
            </section>
          );
        })}
      </main>
      <SiteFooter />
    </>
  );
}