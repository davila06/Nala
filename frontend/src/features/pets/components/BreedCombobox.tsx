import { useEffect, useRef, useState } from 'react'
import { BREEDS_BY_SPECIES } from '../data/breeds'
import type { PetSpecies } from '../api/petsApi'

interface BreedComboboxProps {
  species: PetSpecies
  defaultValue?: string
  disabled?: boolean
  id?: string
}

function highlight(text: string, query: string): React.ReactNode {
  if (!query) return text
  const idx = text.toLowerCase().indexOf(query.toLowerCase())
  if (idx === -1) return text
  return (
    <>
      {text.slice(0, idx)}
      <mark className="bg-brand-100 text-brand-800 rounded-sm not-italic font-semibold">
        {text.slice(idx, idx + query.length)}
      </mark>
      {text.slice(idx + query.length)}
    </>
  )
}

export const BreedCombobox = ({ species, defaultValue = '', disabled, id }: BreedComboboxProps) => {
  const [query, setQuery] = useState(defaultValue)
  const [isOpen, setIsOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(-1)

  const containerRef = useRef<HTMLDivElement>(null)
  const listRef = useRef<HTMLUListElement>(null)
  const prevSpecies = useRef<PetSpecies>(species)

  const allBreeds = BREEDS_BY_SPECIES[species] ?? []
  const filtered = query.trim()
    ? allBreeds.filter((b) => b.toLowerCase().includes(query.toLowerCase()))
    : allBreeds

  // Reset breed value when species changes (but not on initial mount)
  useEffect(() => {
    if (prevSpecies.current !== species) {
      setQuery('')
      setActiveIndex(-1)
      setIsOpen(false)
      prevSpecies.current = species
    }
  }, [species])

  // Close dropdown on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false)
        setActiveIndex(-1)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  // Scroll active item into view
  useEffect(() => {
    if (activeIndex >= 0 && listRef.current) {
      const item = listRef.current.children[activeIndex] as HTMLElement | undefined
      item?.scrollIntoView({ block: 'nearest' })
    }
  }, [activeIndex])

  const select = (breed: string) => {
    setQuery(breed)
    setIsOpen(false)
    setActiveIndex(-1)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!isOpen && (e.key === 'ArrowDown' || e.key === 'Enter')) {
      e.preventDefault()
      setIsOpen(true)
      setActiveIndex(0)
      return
    }
    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault()
        setActiveIndex((i) => Math.min(i + 1, filtered.length - 1))
        break
      case 'ArrowUp':
        e.preventDefault()
        setActiveIndex((i) => Math.max(i - 1, -1))
        break
      case 'Enter':
        if (activeIndex >= 0 && filtered[activeIndex]) {
          e.preventDefault()
          select(filtered[activeIndex])
        }
        break
      case 'Escape':
        setIsOpen(false)
        setActiveIndex(-1)
        break
      case 'Tab':
        setIsOpen(false)
        break
    }
  }

  const listboxId = 'breed-listbox'

  return (
    <div ref={containerRef} className="relative">
      {/* Hidden input consumed by FormData */}
      <input type="hidden" name="breed" value={query} />

      {/* Visible search input */}
      <div className="relative">
        <input
          id={id}
          type="text"
          role="combobox"
          aria-autocomplete="list"
          // eslint-disable-next-line jsx-a11y/aria-proptypes
          aria-expanded={isOpen ? 'true' : 'false'}
          aria-controls={listboxId}
          aria-activedescendant={activeIndex >= 0 ? `breed-option-${activeIndex}` : undefined}
          value={query}
          onChange={(e) => {
            setQuery(e.target.value)
            setIsOpen(true)
            setActiveIndex(-1)
          }}
          onFocus={() => setIsOpen(true)}
          onKeyDown={handleKeyDown}
          disabled={disabled}
          maxLength={100}
          autoComplete="off"
          placeholder={
            allBreeds.length
              ? `Buscar entre ${allBreeds.length} razas…`
              : 'Ej. Mestizo'
          }
          className="block w-full rounded-xl border border-sand-300 px-3.5 py-2.5 pr-9 text-sm shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200 disabled:bg-sand-50 disabled:text-sand-400"
        />

        {/* Chevron / clear button */}
        {query ? (
          <button
            type="button"
            aria-label="Limpiar raza"
            onClick={() => { setQuery(''); setIsOpen(true) }}
            className="absolute inset-y-0 right-2.5 flex items-center rounded-full px-1 text-sand-400 hover:text-sand-600 transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            <svg className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" />
            </svg>
          </button>
        ) : (
          <span
            aria-hidden="true"
            className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sand-400"
          >
            <svg className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path
                fillRule="evenodd"
                d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z"
                clipRule="evenodd"
              />
            </svg>
          </span>
        )}
      </div>

      {/* Dropdown list */}
      {isOpen && (
        <ul
          ref={listRef}
          id={listboxId}
          role="listbox"
          aria-label="Razas"
          className="absolute z-50 mt-1.5 max-h-56 w-full overflow-auto rounded-xl border border-sand-200 bg-white py-1 shadow-lg ring-1 ring-black/5 text-sm"
        >
          {filtered.length === 0 ? (
            <li
              role="option"
              aria-selected="false"
              aria-disabled="true"
              className="px-3.5 py-2.5 text-sand-400 italic select-none"
            >
              Sin resultados — se guardará &ldquo;{query}&rdquo;
            </li>
          ) : (
            filtered.map((breed, i) => (
              <li
                key={breed}
                id={`breed-option-${i}`}
                role="option"
                // eslint-disable-next-line jsx-a11y/aria-proptypes
              aria-selected={i === activeIndex ? 'true' : 'false'}
                onMouseDown={(e) => { e.preventDefault(); select(breed) }}
                onMouseEnter={() => setActiveIndex(i)}
                className={`cursor-pointer px-3.5 py-2 transition-colors ${
                  i === activeIndex
                    ? 'bg-brand-50 text-brand-800'
                    : 'text-sand-700 hover:bg-sand-50'
                }`}
              >
                {highlight(breed, query)}
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  )
}

