import { Suspense, useRef } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { RoundedBox, Text, MeshReflectorMaterial } from '@react-three/drei'
import * as THREE from 'three'

// ── Inner 3D scene ────────────────────────────────────────────────────────────

interface TagMeshProps {
  petName: string
  isLost: boolean
  species: string
}

const SPECIES_CHAR: Record<string, string> = {
  Dog: '🐕', Cat: '🐈', Bird: '🐦', Rabbit: '🐇', Other: '🐾',
}

function TagMesh({ petName, isLost, species }: TagMeshProps) {
  const groupRef = useRef<THREE.Group>(null)
  const timeRef = useRef(0)

  useFrame((_, delta) => {
    timeRef.current += delta
    if (!groupRef.current) return

    // Gentle floating + slight oscillating rotation
    groupRef.current.position.y = Math.sin(timeRef.current * 0.8) * 0.06
    groupRef.current.rotation.y = Math.sin(timeRef.current * 0.4) * 0.12
    groupRef.current.rotation.x = Math.sin(timeRef.current * 0.6) * 0.04
  })

  // Tag color based on status
  const tagColor    = isLost ? '#c43f10' : '#c9b49e'  // brand-600 or sand-400
  const tagEmissive = isLost ? '#7a240a' : '#7a5a40'
  const textColor   = isLost ? '#ffffff' : '#352823'

  return (
    <group ref={groupRef}>
      {/* ── Tag body (rounded rectangle) ──────────────────────────────── */}
      <RoundedBox args={[1.8, 1.0, 0.12]} radius={0.14} smoothness={4}>
        <meshStandardMaterial
          color={tagColor}
          emissive={tagEmissive}
          emissiveIntensity={0.15}
          metalness={0.85}
          roughness={0.15}
        />
      </RoundedBox>

      {/* ── Hole at top ───────────────────────────────────────────────── */}
      <mesh position={[0, 0.55, 0]}>
        <torusGeometry args={[0.1, 0.025, 12, 32]} />
        <meshStandardMaterial color="#a07050" metalness={0.9} roughness={0.1} />
      </mesh>

      {/* ── Engraved paw symbol ───────────────────────────────────────── */}
      <Text
        position={[-0.55, 0.15, 0.07]}
        fontSize={0.28}
        color={textColor}
        anchorX="center"
        anchorY="middle"
      >
        {SPECIES_CHAR[species] ?? '🐾'}
      </Text>

      {/* ── Pet name ──────────────────────────────────────────────────── */}
      <Text
        position={[0.15, 0.18, 0.07]}
        fontSize={0.18}
        fontWeight={700}
        color={textColor}
        anchorX="center"
        anchorY="middle"
        maxWidth={1.0}
      >
        {petName.toUpperCase()}
      </Text>

      {/* ── Status line ───────────────────────────────────────────────── */}
      <Text
        position={[0.15, -0.05, 0.07]}
        fontSize={0.10}
        color={isLost ? '#ffd0b4' : '#8e7059'}
        anchorX="center"
        anchorY="middle"
        letterSpacing={0.08}
      >
        {isLost ? '⚠ MASCOTA PERDIDA' : '● PAWTRACK CR'}
      </Text>

      {/* ── QR hint line ──────────────────────────────────────────────── */}
      <Text
        position={[0, -0.3, 0.07]}
        fontSize={0.075}
        color={isLost ? '#ffb088' : '#ae9077'}
        anchorX="center"
        anchorY="middle"
        letterSpacing={0.04}
      >
        Escanea el QR para más info
      </Text>

      {/* ── Back face: subtle texture ─────────────────────────────────── */}
      <RoundedBox args={[1.8, 1.0, 0.001]} radius={0.14} position={[0, 0, -0.065]}>
        <MeshReflectorMaterial
          blur={[200, 100]}
          resolution={512}
          mixBlur={0.9}
          mixStrength={40}
          roughness={0.3}
          metalness={0.8}
          color={tagColor}
          mirror={0.2}
        />
      </RoundedBox>

      {/* ── Lost pulse ring (only when lost) ─────────────────────────── */}
      {isLost && <LostPulseRing />}
    </group>
  )
}

function LostPulseRing() {
  const ringRef = useRef<THREE.Mesh>(null)
  const timeRef = useRef(0)

  useFrame((_, delta) => {
    timeRef.current += delta
    if (!ringRef.current) return
    const scale = 1 + (Math.sin(timeRef.current * 2.5) + 1) * 0.15
    ringRef.current.scale.setScalar(scale)
    ;(ringRef.current.material as THREE.MeshBasicMaterial).opacity =
      0.6 - (Math.sin(timeRef.current * 2.5) + 1) * 0.25
  })

  return (
    <mesh ref={ringRef} position={[0, 0, -0.02]}>
      <ringGeometry args={[1.05, 1.15, 64]} />
      <meshBasicMaterial color="#d42020" transparent opacity={0.6} side={THREE.DoubleSide} />
    </mesh>
  )
}

// ── Camera + lighting ─────────────────────────────────────────────────────────

function SceneLights({ isLost }: { isLost: boolean }) {
  return (
    <>
      <ambientLight intensity={0.6} />
      <directionalLight position={[3, 4, 3]} intensity={1.2} castShadow />
      <directionalLight position={[-2, 2, -2]} intensity={0.4} />
      <pointLight
        position={[0, 0, 2]}
        intensity={isLost ? 1.5 : 0.8}
        color={isLost ? '#ff6030' : '#ffd0a0'}
      />
    </>
  )
}

// ── Public API ────────────────────────────────────────────────────────────────

interface PetTag3DProps {
  petName: string
  isLost?: boolean
  species?: string
  /** Canvas height in px (default 240) */
  height?: number
}

/**
 * PetTag3D — a metallic 3D pet identification tag that floats in a WebGL canvas.
 * Lazy-loaded by the consumer via React.lazy().
 * Uses @react-three/fiber + @react-three/drei.
 */
export function PetTag3D({ petName, isLost = false, species = 'Other', height = 240 }: PetTag3DProps) {
  return (
    <div style={{ width: '100%', height }} aria-hidden="true">
      <Canvas
        camera={{ position: [0, 0, 3], fov: 45 }}
        dpr={[1, 2]}
        gl={{ antialias: true, alpha: true }}
        style={{ background: 'transparent' }}
      >
        <SceneLights isLost={isLost} />
        <Suspense fallback={null}>
          <TagMesh petName={petName} isLost={isLost} species={species} />
        </Suspense>
      </Canvas>
    </div>
  )
}
