"use client";

import { useEffect } from "react";

export function DepthInteractions() {
  useEffect(() => {
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    const finePointer = window.matchMedia("(pointer: fine)");
    if (reduceMotion.matches || !finePointer.matches) return;

    let frame = 0;
    let active: HTMLElement | null = null;
    let nextX = 0;
    let nextY = 0;

    const flush = () => {
      frame = 0;
      if (!active) return;
      active.style.setProperty("--depth-pointer-x", `${nextX}deg`);
      active.style.setProperty("--depth-pointer-y", `${nextY}deg`);
    };

    const handlePointerMove = (event: PointerEvent) => {
      const target = event.target;
      if (!(target instanceof Element)) return;
      const surface = target.closest<HTMLElement>("[data-3d-depth]");
      if (!surface) return;

      if (active && active !== surface) {
        active.style.setProperty("--depth-pointer-x", "0deg");
        active.style.setProperty("--depth-pointer-y", "0deg");
      }
      active = surface;

      const bounds = surface.getBoundingClientRect();
      const normalizedX = (event.clientX - bounds.left) / bounds.width - 0.5;
      const normalizedY = (event.clientY - bounds.top) / bounds.height - 0.5;
      const strength = Number(surface.dataset.depthStrength ?? 4);
      nextX = Math.max(-strength, Math.min(strength, normalizedX * strength * 2));
      nextY = Math.max(-strength, Math.min(strength, normalizedY * -strength * 2));

      if (!frame) frame = window.requestAnimationFrame(flush);
    };

    const reset = (event: PointerEvent) => {
      const target = event.target;
      if (!(target instanceof HTMLElement) || !target.matches("[data-3d-depth]")) return;
      target.style.setProperty("--depth-pointer-x", "0deg");
      target.style.setProperty("--depth-pointer-y", "0deg");
      if (active === target) active = null;
    };

    document.addEventListener("pointermove", handlePointerMove, { passive: true });
    document.addEventListener("pointerleave", reset, true);

    return () => {
      if (frame) window.cancelAnimationFrame(frame);
      document.removeEventListener("pointermove", handlePointerMove);
      document.removeEventListener("pointerleave", reset, true);
    };
  }, []);

  return null;
}
