interface PawConfig {
  left: string;
  dur: string;
  size: string;
  delay: string;
  opacity: number;
}

interface AmbientPawsProps {
  paws: PawConfig[];
}

/** Decorative floating paw prints for auth pages — screen-reader hidden. */
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

export const LOGIN_PAWS: PawConfig[] = [
  { left: "8%", dur: "7s", size: "1.4rem", delay: "0s", opacity: 0.12 },
  { left: "22%", dur: "9s", size: "1rem", delay: "1.2s", opacity: 0.08 },
  { left: "50%", dur: "11s", size: "1.8rem", delay: "0.5s", opacity: 0.1 },
  { left: "70%", dur: "8s", size: "1.2rem", delay: "2.1s", opacity: 0.09 },
  { left: "88%", dur: "10s", size: "1.5rem", delay: "3.4s", opacity: 0.07 },
];

export const REGISTER_PAWS: PawConfig[] = [
  { left: "7%", dur: "6s", size: "1.3rem", delay: "0s", opacity: 0.1 },
  { left: "25%", dur: "9s", size: "0.9rem", delay: "1.4s", opacity: 0.07 },
  { left: "55%", dur: "8s", size: "1.6rem", delay: "0.6s", opacity: 0.09 },
  { left: "75%", dur: "7s", size: "1.1rem", delay: "2.2s", opacity: 0.08 },
  { left: "90%", dur: "10s", size: "1.4rem", delay: "3.1s", opacity: 0.06 },
];
