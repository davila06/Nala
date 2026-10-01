"use client";

import { useEffect, useRef } from "react";
import type { Material } from "three";

export function Hero3DScene() {
  const hostRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let disposed = false;
    let cleanup: (() => void) | undefined;

    const mount = async () => {
      const host = hostRef.current;
      if (!host) return;

      const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
      const THREE = await import("three");
      if (disposed) return;

      const scene = new THREE.Scene();
      const camera = new THREE.PerspectiveCamera(35, 1, 0.1, 100);
      camera.position.set(0, 0, 5.2);

      const renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: "high-performance" });
      renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.75));
      renderer.setClearColor(0x000000, 0);
      host.appendChild(renderer.domElement);

      const ambient = new THREE.HemisphereLight(0xfff8e4, 0x173c34, 2.4);
      scene.add(ambient);
      const keyLight = new THREE.DirectionalLight(0xffffff, 3.2);
      keyLight.position.set(3, 4, 5);
      scene.add(keyLight);

      const identityGroup = new THREE.Group();
      scene.add(identityGroup);

      const tag = new THREE.Mesh(
        new THREE.CylinderGeometry(1.08, 1.08, 0.22, 64),
        new THREE.MeshStandardMaterial({ color: 0xd9664b, metalness: 0.28, roughness: 0.3 }),
      );
      tag.rotation.x = Math.PI / 2;
      identityGroup.add(tag);

      const ring = new THREE.Mesh(
        new THREE.TorusGeometry(1.3, 0.035, 12, 96),
        new THREE.MeshStandardMaterial({ color: 0xbfd9c2, emissive: 0x315f4f, emissiveIntensity: 0.35 }),
      );
      ring.rotation.x = Math.PI / 2;
      identityGroup.add(ring);

      const nodeMaterial = new THREE.MeshStandardMaterial({
        color: 0xfffefa,
        emissive: 0xd9664b,
        emissiveIntensity: 0.5,
      });
      const nodeGeometry = new THREE.SphereGeometry(0.075, 16, 16);
      const nodePositions = [
        [-1.55, 0.7, 0.25],
        [1.45, 0.82, -0.1],
        [1.55, -0.85, 0.2],
        [-1.4, -0.95, -0.2],
      ] as const;
      for (const [x, y, z] of nodePositions) {
        const node = new THREE.Mesh(nodeGeometry, nodeMaterial);
        node.position.set(x, y, z);
        identityGroup.add(node);
      }

      const pointer = { x: 0, y: 0 };
      const handlePointerMove = (event: PointerEvent) => {
        const bounds = host.getBoundingClientRect();
        pointer.x = ((event.clientX - bounds.left) / bounds.width - 0.5) * 0.35;
        pointer.y = ((event.clientY - bounds.top) / bounds.height - 0.5) * -0.35;
      };
      host.addEventListener("pointermove", handlePointerMove, { passive: true });

      const resize = () => {
        const width = Math.max(host.clientWidth, 1);
        const height = Math.max(host.clientHeight, 1);
        camera.aspect = width / height;
        camera.updateProjectionMatrix();
        renderer.setSize(width, height, false);
      };
      const resizeObserver = new ResizeObserver(resize);
      resizeObserver.observe(host);
      resize();

      let visible = true;
      const visibilityObserver = new IntersectionObserver(([entry]) => {
        visible = entry.isIntersecting;
      });
      visibilityObserver.observe(host);

      let animationFrame = 0;
      const startedAt = performance.now();
      const render = (now: number) => {
        if (disposed) return;
        if (visible) {
          const elapsed = (now - startedAt) * 0.001;
          if (!prefersReducedMotion) {
            identityGroup.rotation.y += (pointer.x + identityGroup.rotation.y * -0.02) * 0.035;
            identityGroup.rotation.x += (pointer.y + identityGroup.rotation.x * -0.02) * 0.035;
            identityGroup.position.y = Math.sin(elapsed * 1.2) * 0.08;
            ring.rotation.z += 0.002;
          }
          renderer.render(scene, camera);
        }
        animationFrame = window.requestAnimationFrame(render);
      };
      animationFrame = window.requestAnimationFrame(render);

      cleanup = () => {
        window.cancelAnimationFrame(animationFrame);
        visibilityObserver.disconnect();
        resizeObserver.disconnect();
        host.removeEventListener("pointermove", handlePointerMove);
        tag.geometry.dispose();
        (tag.material as Material).dispose();
        ring.geometry.dispose();
        (ring.material as Material).dispose();
        nodeGeometry.dispose();
        nodeMaterial.dispose();
        renderer.dispose();
        renderer.domElement.remove();
      };
    };

    void mount();
    return () => {
      disposed = true;
      cleanup?.();
    };
  }, []);

  return <div aria-hidden="true" className="hero-3d-scene" ref={hostRef} />;
}
