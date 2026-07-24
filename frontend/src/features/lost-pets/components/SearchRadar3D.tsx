import { useRef } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { Line } from '@react-three/drei'
import * as THREE from 'three'

// ── Expanding pulse ring ──────────────────────────────────────────────────────

function PulseRing({ delay = 0, color = '#d42020' }: { delay?: number; color?: string }) {
  const meshRef = useRef<THREE.Mesh>(null)
  const timeRef = useRef(-delay)

  useFrame((_, delta) => {
    timeRef.current += delta
    const t = (timeRef.current % 3.0) / 3.0  // normalize to 0..1 every 3s
    if (!meshRef.current) return
    meshRef.current.scale.setScalar(0.3 + t * 2.5)
    ;(meshRef.current.material as THREE.MeshBasicMaterial).opacity = (1 - t) * 0.6
  })

  return (
    <mesh ref={meshRef}>
      <ringGeometry args={[0.95, 1.0, 64]} />
      <meshBasicMaterial color={color} transparent opacity={0.6} side={THREE.DoubleSide} />
    </mesh>
  )
}

// ── Rotating scanner beam ─────────────────────────────────────────────────────

function ScannerBeam({ color = '#d42020' }: { color?: string }) {
  const groupRef = useRef<THREE.Group>(null)

  useFrame((_, delta) => {
    if (groupRef.current) {
      groupRef.current.rotation.z -= delta * 1.2  // counterclockwise sweep
    }
  })

  // Gradient cone triangle
  const points = [
    new THREE.Vector3(0, 0, 0),
    new THREE.Vector3(2.8, 0.8, 0),
    new THREE.Vector3(2.8, -0.8, 0),
  ]

  return (
    <group ref={groupRef}>
      {/* Beam fill */}
      <mesh>
        <bufferGeometry>
          <bufferAttribute
            attach="attributes-position"
            args={[new Float32Array(points.flatMap((p) => [p.x, p.y, p.z])), 3]}
          />
        </bufferGeometry>
        <meshBasicMaterial color={color} transparent opacity={0.12} side={THREE.DoubleSide} />
      </mesh>
      {/* Beam edge line */}
      <Line
        points={[points[0], points[1]]}
        color={color}
        lineWidth={1}
        transparent
        opacity={0.4}
      />
    </group>
  )
}

// ── Sighting dots ─────────────────────────────────────────────────────────────

interface SightingDot {
  x: number
  y: number
  label?: string
}

function SightingDots({ dots }: { dots: SightingDot[] }) {
  return (
    <>
      {dots.map((dot, i) => (
        <mesh key={i} position={[dot.x, dot.y, 0.01]}>
          <circleGeometry args={[0.08, 16]} />
          <meshBasicMaterial color="#3056c2" />
        </mesh>
      ))}
    </>
  )
}

// ── Radar grid rings ──────────────────────────────────────────────────────────

function RadarGrid({ color = '#17a26d' }: { color?: string }) {
  return (
    <>
      {[1.0, 2.0, 3.0].map((r) => (
        <mesh key={r}>
          <ringGeometry args={[r - 0.01, r, 64]} />
          <meshBasicMaterial color={color} transparent opacity={0.12} />
        </mesh>
      ))}
      {/* Crosshairs */}
      <Line points={[[-3.1, 0, 0], [3.1, 0, 0]]} color={color} lineWidth={0.5} transparent opacity={0.15} />
      <Line points={[[0, -3.1, 0], [0, 3.1, 0]]} color={color} lineWidth={0.5} transparent opacity={0.15} />
    </>
  )
}

// ── Center marker (last known position) ──────────────────────────────────────

function CenterMarker({ isLost }: { isLost: boolean }) {
  const meshRef = useRef<THREE.Mesh>(null)

  useFrame((state) => {
    if (meshRef.current) {
      ;(meshRef.current.material as THREE.MeshBasicMaterial).opacity =
        0.6 + Math.sin(state.clock.elapsedTime * 3) * 0.3
    }
  })

  return (
    <group>
      {/* Outer glow */}
      <mesh>
        <circleGeometry args={[0.22, 32]} />
        <meshBasicMaterial color={isLost ? '#d42020' : '#17a26d'} transparent opacity={0.2} />
      </mesh>
      {/* Inner dot */}
      <mesh ref={meshRef} position={[0, 0, 0.01]}>
        <circleGeometry args={[0.1, 32]} />
        <meshBasicMaterial color={isLost ? '#d42020' : '#17a26d'} transparent opacity={0.8} />
      </mesh>
    </group>
  )
}

// ── Public API ────────────────────────────────────────────────────────────────

interface SearchRadar3DProps {
  isLost?: boolean
  /** Relative sighting positions normalized to -3..3 range */
  sightingDots?: SightingDot[]
  height?: number
}

/**
 * SearchRadar3D — an animated radar/sonar visualization for CaseRoom.
 * Shows the last known position at center with expanding search pulse rings
 * and sighting dots, with a rotating scanner beam.
 */
export function SearchRadar3D({
  isLost = true,
  sightingDots = [],
  height = 280,
}: SearchRadar3DProps) {
  const radarColor = isLost ? '#d42020' : '#17a26d'
  const gridColor  = '#17a26d'

  return (
    <div style={{ width: '100%', height }} aria-hidden="true">
      <Canvas
        camera={{ position: [0, 0, 5], fov: 50, near: 0.1, far: 50 }}
        dpr={[1, 1.5]}
        gl={{ antialias: true, alpha: true }}
        style={{ background: 'transparent' }}
      >
        <ambientLight intensity={0.3} />

        {/* Dark radar background */}
        <mesh position={[0, 0, -0.1]}>
          <circleGeometry args={[3.1, 64]} />
          <meshBasicMaterial color="#0a1a0f" transparent opacity={0.85} />
        </mesh>

        <RadarGrid color={gridColor} />
        <CenterMarker isLost={isLost} />
        <ScannerBeam color={radarColor} />

        {/* Three expanding pulse rings with offsets */}
        <PulseRing delay={0}   color={radarColor} />
        <PulseRing delay={1.0} color={radarColor} />
        <PulseRing delay={2.0} color={radarColor} />

        {/* Sighting dots */}
        <SightingDots dots={sightingDots} />

        {/* Boundary circle */}
        <mesh>
          <ringGeometry args={[3.0, 3.1, 64]} />
          <meshBasicMaterial color={gridColor} transparent opacity={0.35} />
        </mesh>
      </Canvas>
    </div>
  )
}
