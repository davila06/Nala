import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { SiteFooter, SiteHeader } from "@/components/site-chrome";
import { editorialArticles } from "@/lib/blog-content";

type BlogArticlePageProps = {
  params: Promise<{ slug: string }>;
};

export function generateStaticParams() {
  return editorialArticles.map(({ slug }) => ({ slug }));
}

export const dynamicParams = false;

export async function generateMetadata({ params }: BlogArticlePageProps): Promise<Metadata> {
  const { slug } = await params;
  const article = editorialArticles.find((entry) => entry.slug === slug);

  if (!article) return { title: "Guía no encontrada" };

  return {
    title: article.title,
    description: article.summary,
    openGraph: {
      title: `${article.title} | Guías NALA`,
      description: article.summary,
      type: "article",
    },
  };
}

export default async function BlogArticlePage({ params }: BlogArticlePageProps) {
  const { slug } = await params;
  const article = editorialArticles.find((entry) => entry.slug === slug);

  if (!article) notFound();

  return (
    <>
      <SiteHeader />
      <main className="blog-page" id="main">
        <article className="blog-article-page section-shell">
          <nav aria-label="Ruta de navegación" className="breadcrumbs">
            <Link href="/">Inicio</Link><span aria-hidden="true">/</span>
            <Link href="/blog">Guías NALA</Link><span aria-hidden="true">/</span>
            <span>{article.category}</span>
          </nav>
          <p className="eyebrow"><span /> {article.category}</p>
          <h1>{article.title}</h1>
          <p className="blog-article-summary">{article.summary}</p>
          <p className="blog-editorial-note" role="note">Contenido educativo general. No sustituye la orientación veterinaria profesional; las funciones y servicios de NALA aún están en desarrollo.</p>
          <div className="blog-article-body">
            {article.sections.map((section) => (
              <section key={section.heading}>
                <h2>{section.heading}</h2>
                {section.paragraphs.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}
              </section>
            ))}
          </div>
          <div className="blog-article-actions">
            <Link className="text-link" href={article.relatedHref}>{article.relatedLabel} <span aria-hidden="true">→</span></Link>
            <Link className="text-link" href="/blog">Volver a todas las guías <span aria-hidden="true">↗</span></Link>
          </div>
        </article>
      </main>
      <SiteFooter />
    </>
  );
}