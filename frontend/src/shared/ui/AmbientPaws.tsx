import type { PawConfig } from "./ambientPawsConfig";

interface AmbientPawsProps {
  paws: PawConfig[];
}

/** Decorative floating paw prints for auth pages. */
export function AmbientPaws({ paws }: AmbientPawsProps) {
  return (
    <>
      {paws.map((p, i) => (
        <span
          key={i}
          aria-hidden="true"
          style={{
            position: "absolute",
            left: p.left,
            bottom: "-2rem",
            fontSize: p.size,
            opacity: p.opacity,
            animation: `float-bob ${p.dur} ease-in-out ${p.delay} infinite`,
            userSelect: "none",
            pointerEvents: "none",
          }}
        >
          🐾
        </span>
      ))}
    </>
  );
}
