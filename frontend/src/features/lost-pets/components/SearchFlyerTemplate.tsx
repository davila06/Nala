import { forwardRef } from 'react'
import type { PetDetail } from '@/features/pets/api/petsApi'

//  Props 

export interface SearchFlyerData {
  pet: Pick<PetDetail, 'id' | 'name' | 'species' | 'breed' | 'photoUrl'>
  lastSeenAt: string
  description: string | null
  petPhotoDataUrl: string | null
  recentPhotoDataUrl: string | null
  qrCodeDataUrl: string | null
  baseUrl: string
}

interface SearchFlyerTemplateProps {
  data: SearchFlyerData
}

//  Helpers 

const SPECIES_LABEL: Record<string, string> = {
  Dog: 'Perro',
  Cat: 'Gato',
  Bird: 'Ave',
  Rabbit: 'Conejo',
  Other: 'Otro',
}

const SPECIES_EMOJI: Record<string, string> = {
  Dog: '',
  Cat: '',
  Bird: '',
  Rabbit: '',
  Other: '',
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('es-CR', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

//  Scoped styles 

const FLYER_STYLES = `.paw-flyer{position:fixed;top:0;left:-9999px;z-index:-1;width:600px;height:840px;background-color:#fffbf5;font-family:Georgia,"Times New Roman",serif;overflow:hidden;display:flex;flex-direction:column}.paw-flyer__header{background-color:#b91c1c;padding:18px 24px 14px;text-align:center;flex-shrink:0}.paw-flyer__eyebrow{color:#fef2f2;font-family:"Arial Black",Arial,sans-serif;font-size:11px;font-weight:700;letter-spacing:4px;text-transform:uppercase;margin:0 0 6px}.paw-flyer__title{color:#ffffff;font-family:"Arial Black",Arial,sans-serif;font-size:34px;font-weight:900;line-height:1.1;margin:0;text-shadow:0 2px 8px rgba(0,0,0,0.35)}.paw-flyer__title-name{color:#fcd34d}.paw-flyer__photo-wrap{width:100%;height:280px;flex-shrink:0;overflow:hidden;background-color:#fef3c7;display:flex;align-items:center;justify-content:center;position:relative}.paw-flyer__photo{width:100%;height:100%;object-fit:cover;display:block}.paw-flyer__photo-emoji{font-size:96px}.paw-flyer__photo-gradient{position:absolute;bottom:0;left:0;right:0;height:70px;background:linear-gradient(to bottom,transparent,#fffbf5)}.paw-flyer__body{display:flex;flex:1;padding:16px 22px;gap:18px;overflow:hidden}.paw-flyer__info{flex:1;display:flex;flex-direction:column;gap:10px}.paw-flyer__card{border-radius:10px;padding:10px 14px}.paw-flyer__card--desc{background-color:#fef2f2;border-left:4px solid #b91c1c}.paw-flyer__card--time{background-color:#fff7ed;border-left:4px solid #ea580c}.paw-flyer__card--notes{background-color:#f0fdf4;border-left:4px solid #16a34a}.paw-flyer__card-label{margin:0 0 3px;font-size:11px;font-family:Arial,sans-serif;font-weight:700;text-transform:uppercase;letter-spacing:1.5px}.paw-flyer__card-label--desc{color:#9f1239}.paw-flyer__card-label--time{color:#9a3412}.paw-flyer__card-label--notes{color:#166534}.paw-flyer__card-value{margin:0;font-family:Arial,sans-serif}.paw-flyer__card-value--desc{font-size:15px;color:#1c1917;font-weight:600;line-height:1.4}.paw-flyer__card-value--time{font-size:13px;color:#1c1917;font-weight:500;line-height:1.5}.paw-flyer__card-value--notes{font-size:13px;color:#1c1917;font-weight:400;line-height:1.5}.paw-flyer__qr-col{width:145px;flex-shrink:0;display:flex;flex-direction:column;align-items:center;gap:8px}.paw-flyer__qr-box{background-color:#fff;border-radius:12px;padding:8px;border:2px solid #b91c1c;box-shadow:0 2px 12px rgba(185,28,28,0.15)}.paw-flyer__qr-img{width:112px;height:112px;display:block}.paw-flyer__qr-placeholder{width:112px;height:112px;background-color:#f4f4f5;border-radius:4px;display:flex;align-items:center;justify-content:center;font-size:10px;color:#a1a1aa;font-family:Arial,sans-serif;text-align:center}.paw-flyer__qr-caption{margin:0;font-size:10px;color:#78716c;font-family:Arial,sans-serif;text-align:center;line-height:1.4}.paw-flyer__footer{background-color:#1c1917;padding:10px 24px;display:flex;align-items:center;justify-content:space-between;flex-shrink:0}.paw-flyer__footer-brand{margin:0;font-size:12px;color:#a8a29e;font-family:Arial,sans-serif}.paw-flyer__footer-url{margin:0;font-size:11px;color:#78716c;font-family:Arial,sans-serif}`

//  Component 

export const SearchFlyerTemplate = forwardRef<HTMLDivElement, SearchFlyerTemplateProps>(
  ({ data }, ref) => {
    const { pet, lastSeenAt, description, petPhotoDataUrl, recentPhotoDataUrl, qrCodeDataUrl } = data
    const speciesLabel = SPECIES_LABEL[pet.species] ?? pet.species
    const speciesEmoji = SPECIES_EMOJI[pet.species] ?? ''
    // Recent photo takes priority over the profile photo in the flyer
    const photoSrc = (recentPhotoDataUrl ?? petPhotoDataUrl) ?? undefined

    return (
      <>
        <style>{FLYER_STYLES}</style>
        <div aria-hidden="true" className="paw-flyer" ref={ref}>
          <div className="paw-flyer__header">
            <p className="paw-flyer__eyebrow">MASCOTA PERDIDA</p>
            <p className="paw-flyer__title">
              HAS VISTO A{' '}
              <span className="paw-flyer__title-name">{pet.name.toUpperCase()}</span>?
            </p>
          </div>

          <div className="paw-flyer__photo-wrap">
            {photoSrc ? (
              <img src={photoSrc} alt={pet.name} className="paw-flyer__photo" />
            ) : (
              <span className="paw-flyer__photo-emoji">{speciesEmoji}</span>
            )}
            <div className="paw-flyer__photo-gradient" />
          </div>

          <div className="paw-flyer__body">
            <div className="paw-flyer__info">
              <div className="paw-flyer__card paw-flyer__card--desc">
                <p className="paw-flyer__card-label paw-flyer__card-label--desc">Descripcion</p>
                <p className="paw-flyer__card-value paw-flyer__card-value--desc">
                  {speciesLabel}
                  {pet.breed ? ` - ${pet.breed}` : ''}
                </p>
              </div>

              <div className="paw-flyer__card paw-flyer__card--time">
                <p className="paw-flyer__card-label paw-flyer__card-label--time">
                  Visto por ultima vez
                </p>
                <p className="paw-flyer__card-value paw-flyer__card-value--time">
                  {formatDate(lastSeenAt)}
                </p>
              </div>

              {description && (
                <div className="paw-flyer__card paw-flyer__card--notes">
                  <p className="paw-flyer__card-label paw-flyer__card-label--notes">
                    Senas particulares
                  </p>
                  <p className="paw-flyer__card-value paw-flyer__card-value--notes">
                    {description}
                  </p>
                </div>
              )}
            </div>

            <div className="paw-flyer__qr-col">
              <div className="paw-flyer__qr-box">
                {qrCodeDataUrl ? (
                  <img src={qrCodeDataUrl} alt="QR code" className="paw-flyer__qr-img" />
                ) : (
                  <div className="paw-flyer__qr-placeholder">QR no disponible</div>
                )}
              </div>
              <p className="paw-flyer__qr-caption">
                Escanea para ver el perfil de {pet.name}
              </p>
            </div>
          </div>

          <div className="paw-flyer__footer">
            <p className="paw-flyer__footer-brand">PawTrack CR</p>
            <p className="paw-flyer__footer-url">pawtrack.cr/p/{pet.id}</p>
          </div>
        </div>
      </>
    )
  },
)

SearchFlyerTemplate.displayName = 'SearchFlyerTemplate'
