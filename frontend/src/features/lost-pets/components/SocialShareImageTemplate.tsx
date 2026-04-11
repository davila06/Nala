import { forwardRef } from 'react'
import type { PetDetail } from '@/features/pets/api/petsApi'

// ── Types ─────────────────────────────────────────────────────────────────────

/** Data required to render the 1200×630 social-share image. */
export interface SocialShareImageData {
  pet: Pick<PetDetail, 'id' | 'name' | 'species' | 'breed' | 'photoUrl'>
  /**
   * Pre-fetched pet photo as a base-64 data URL.
   * Must be a data URL (not a remote URL) so html2canvas can render it
   * without CORS restrictions.
   */
  petPhotoDataUrl: string | null
  /** Pre-fetched QR code image as a base-64 data URL. */
  qrCodeDataUrl: string | null
  baseUrl: string
}

interface SocialShareImageTemplateProps {
  data: SocialShareImageData
}

// ── Constants ─────────────────────────────────────────────────────────────────

/** High-contrast accent colour per species – drives the banner, CTA link, and QR border. */
const SPECIES_ACCENT: Record<string, string> = {
  Dog: '#FF6B35',
  Cat: '#7C3AED',
  Rabbit: '#059669',
  Bird: '#0284C7',
}
const DEFAULT_ACCENT = '#DC2626'

const SPECIES_LABEL: Record<string, string> = {
  Dog: 'Perro',
  Cat: 'Gato',
  Bird: 'Ave',
  Rabbit: 'Conejo',
  Other: 'Otro',
}

const SPECIES_EMOJI: Record<string, string> = {
  Dog: '🐶',
  Cat: '🐱',
  Bird: '🐦',
  Rabbit: '🐰',
  Other: '🐾',
}

// ── Scoped styles ─────────────────────────────────────────────────────────────
// Prefixed with `.paw-social__` to avoid collisions with the print flyer styles
// (.paw-flyer__*). All styles are expressed in plain CSS usable by html2canvas.
// Dynamic accent colours are applied via inline `style` props on the relevant
// elements so they are always written into the computed style.

const SOCIAL_STYLES = `.paw-social{position:fixed;top:0;left:-9999px;z-index:-1;width:1200px;height:630px;display:flex;overflow:hidden;font-family:Arial,sans-serif;}.paw-social__photo{width:720px;height:630px;flex-shrink:0;overflow:hidden;position:relative;background-color:#e8e8e8;}.paw-social__photo img{width:100%;height:100%;object-fit:cover;display:block;}.paw-social__photo-placeholder{width:100%;height:100%;display:flex;align-items:center;justify-content:center;font-size:150px;}.paw-social__photo-edge{position:absolute;top:0;right:0;bottom:0;width:100px;background:linear-gradient(to right,transparent,#ffffff);}.paw-social__panel{width:480px;flex-shrink:0;background-color:#ffffff;display:flex;flex-direction:column;justify-content:space-between;position:relative;padding:52px 44px 40px;box-sizing:border-box;height:630px;}.paw-social__accent-bar{position:absolute;top:0;left:0;right:0;height:8px;}.paw-social__top{}.paw-social__eyebrow{margin:0 0 10px;font-size:11px;font-weight:700;letter-spacing:4px;text-transform:uppercase;}.paw-social__name{margin:0 0 10px;font-family:"Arial Black",Arial,sans-serif;font-size:52px;font-weight:900;line-height:1.0;color:#111111;word-break:break-word;}.paw-social__breed{margin:0;font-size:21px;color:#555555;font-weight:500;}.paw-social__cta-group{}.paw-social__cta{margin:0 0 7px;font-family:"Arial Black",Arial,sans-serif;font-size:28px;font-weight:900;color:#111111;}.paw-social__url{margin:0;font-size:17px;font-weight:700;}.paw-social__qr-row{display:flex;align-items:center;gap:16px;}.paw-social__qr-box{border-radius:10px;padding:6px;background:#fff;flex-shrink:0;}.paw-social__qr-img{width:80px;height:80px;display:block;}.paw-social__qr-placeholder{width:80px;height:80px;background:#f0f0f0;border-radius:6px;display:flex;align-items:center;justify-content:center;font-size:10px;color:#999;text-align:center;}.paw-social__qr-copy p{margin:0;}.paw-social__qr-scan{font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:1px;color:#777;margin-bottom:4px !important;}.paw-social__qr-brand{font-size:16px;font-weight:700;}`

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Off-screen 1200×630 template for the "viral" social-media share image.
 *
 * This component is *always* mounted (not conditionally rendered) so that
 * `flyerRef` / `socialRef` is never `null` when html2canvas captures it:
 *
 * - Before `assets` are ready → renders with placeholder colours / empty QR.
 * - After `assets` are ready → renders with real data URLs; html2canvas
 *   captures this version after the DOM commit.
 *
 * The component is hidden via `position:fixed; left:-9999px` and marked
 * `aria-hidden="true"` so it is invisible and skipped by screen readers.
 *
 * @see useGenerateFlyer – `downloadSocialImage` / `buildSocialImageBlob`
 */
export const SocialShareImageTemplate = forwardRef<HTMLDivElement, SocialShareImageTemplateProps>(
  ({ data }, ref) => {
    const { pet, petPhotoDataUrl, qrCodeDataUrl } = data

    const accent      = SPECIES_ACCENT[pet.species] ?? DEFAULT_ACCENT
    const speciesLabel = SPECIES_LABEL[pet.species] ?? pet.species
    const speciesEmoji = SPECIES_EMOJI[pet.species] ?? '🐾'
    const photoSrc    = petPhotoDataUrl ?? undefined

    return (
      <>
        <style>{SOCIAL_STYLES}</style>
        <div aria-hidden="true" className="paw-social" ref={ref}>

          {/* ── Left: full-bleed pet photo ──────────────────────────────── */}
          <div className="paw-social__photo">
            {photoSrc ? (
              <img src={photoSrc} alt={pet.name} />
            ) : (
              <div
                className="paw-social__photo-placeholder"
                style={{ backgroundColor: `${accent}18` }}
              >
                {speciesEmoji}
              </div>
            )}
            {/* Right-edge gradient for smooth transition to the white panel */}
            <div className="paw-social__photo-edge" />
          </div>

          {/* ── Right: content panel ────────────────────────────────────── */}
          <div className="paw-social__panel">
            {/* Species-accent top stripe */}
            <div className="paw-social__accent-bar" style={{ backgroundColor: accent }} />

            {/* TOP GROUP — identity */}
            <div className="paw-social__top">
              <p className="paw-social__eyebrow" style={{ color: accent }}>
                MASCOTA PERDIDA
              </p>
              <h1 className="paw-social__name">{pet.name.toUpperCase()}</h1>
              <p className="paw-social__breed">
                {speciesLabel}
                {pet.breed ? ` · ${pet.breed}` : ''}
              </p>
            </div>

            {/* MIDDLE GROUP — call to action */}
            <div className="paw-social__cta-group">
              <p className="paw-social__cta">¿Lo has visto? 👀</p>
              <p className="paw-social__url" style={{ color: accent }}>
                pawtrack.cr/p/{pet.id}
              </p>
            </div>

            {/* BOTTOM GROUP — QR code */}
            <div className="paw-social__qr-row">
              <div
                className="paw-social__qr-box"
                style={{ border: `3px solid ${accent}` }}
              >
                {qrCodeDataUrl ? (
                  <img src={qrCodeDataUrl} alt="QR code" className="paw-social__qr-img" />
                ) : (
                  <div className="paw-social__qr-placeholder">QR</div>
                )}
              </div>
              <div className="paw-social__qr-copy">
                <p className="paw-social__qr-scan">Escanea para contactar al dueño</p>
                <p className="paw-social__qr-brand" style={{ color: accent }}>
                  PawTrack CR
                </p>
              </div>
            </div>
          </div>
        </div>
      </>
    )
  },
)

SocialShareImageTemplate.displayName = 'SocialShareImageTemplate'
