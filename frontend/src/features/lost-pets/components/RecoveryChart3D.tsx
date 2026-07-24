import { useRef, useMemo } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { Text, RoundedBox } from '@react-three/drei'
import * as THREE from 'three'

// ── Single 3D bar ─────────────────────────────────────────────────────────────

interface Bar3DProps {
  x: number
  height: number
  maxHeight: number
  color: string
  label: string
  value: string
  index: number
}

function Bar3D({ x, height, maxHeight, color, label, value, index }: Bar3DProps) {
  const meshRef = useRef<THREE.Mesh>(null)
  const groupRef = useRef<THREE.Group>(null)
  const timeRef = useRef(-index * 0.15) // stagger

  // Animate height on mount + subtle oscillation
  const targetHeight = (height / maxHeight) * 3.5
  const currentHeight = useRef(0)

  useFrame((_, delta) => {
    timeRef.current += delta
    if (!meshRef.current || !groupRef.current) return

    // Animate height growth on mount
    if (currentHeight.current < targetHeight) {
      currentHeight.current = Math.min(
        targetHeight,
        currentHeight.current + delta * 3.5 * (timeRef.current > 0 ? 1 : 0),
      )
      meshRef.current.scale.y = currentHeight.current / targetHeight
      groupRef.current.position.y = -(targetHeight - currentHeight.current) / 2
    }

    // Gentle idle oscillation after growth
    if (currentHeight.current >= targetHeight * 0.99) {
      const idle = Math.sin(timeRef.current * 0.8 + index * 0.5) * 0.02
      groupRef.current.position.y = idle
    }
  })

  return (
    <group position={[x, 0, 0]}>
      {/* Bar group that animates */}
      <group ref={groupRef}>
        <RoundedBox
          ref={meshRef}
          args={[0.5, targetHeight, 0.3]}
          radius={0.06}
          position={[0, targetHeight / 2, 0]}
        >
          <meshStandardMaterial
            color={color}
            emissive={color}
            emissiveIntensity={0.1}
            metalness={0.3}
            roughness={0.5}
          />
        </RoundedBox>

        {/* Value label on top of bar */}
        <Text
          position={[0, targetHeight + 0.35, 0]}
          fontSize={0.22}
          color={color}
          anchorX="center"
          anchorY="bottom"
          fontWeight={700}
        >
          {value}
        </Text>
      </group>

      {/* Canton label below bar (static) */}
      <Text
        position={[0, -0.3, 0]}
        fontSize={0.14}
        color="#8e7059"
        anchorX="center"
        anchorY="top"
        maxWidth={0.7}
      >
        {label}
      </Text>
    </group>
  )
}

// ── Floor grid ────────────────────────────────────────────────────────────────

function FloorGrid({ count }: { count: number }) {
  const width = count * 0.9 + 0.4
  return (
    <mesh position={[0, 0, 0]} rotation={[-Math.PI / 2, 0, 0]}>
      <planeGeometry args={[width, 0.02]} />
      <meshBasicMaterial color="#e2d3c4" transparent opacity={0.5} />
    </mesh>
  )
}

// ── Camera slow orbit ─────────────────────────────────────────────────────────

function OrbitCamera() {
  useFrame((state) => {
    const t = state.clock.elapsedTime
    state.camera.position.x = Math.sin(t * 0.08) * 0.6
    state.camera.position.z = 8 + Math.cos(t * 0.08) * 0.4
    state.camera.lookAt(0, 1.5, 0)
  })
  return null
}

// ── Public API ────────────────────────────────────────────────────────────────

export interface ChartBar {
  label: string
  value: number  // e.g. recovery rate 0..1
  displayValue: string
}

interface RecoveryChart3DProps {
  bars: ChartBar[]
  height?: number
}

/**
 * RecoveryChart3D — animated 3D bar chart for the Recovery Stats page.
 * Bars grow on mount with stagger, camera slowly orbits for depth effect.
 */
export function RecoveryChart3D({ bars, height = 320 }: RecoveryChart3DProps) {
  const maxValue = useMemo(() => Math.max(...bars.map((b) => b.value), 0.01), [bars])

  // Color scale: low → orange, high → green
  const getColor = (value: number) => {
    const ratio = value / maxValue
    if (ratio > 0.75) return '#17a26d'  // rescue-500
    if (ratio > 0.5)  return '#3056c2'  // trust-500
    if (ratio > 0.25) return '#f0b800'  // warn-400
    return '#d42020'                      // danger-500
  }

  const spacing = 0.85
  const totalWidth = (bars.length - 1) * spacing
  const startX = -totalWidth / 2

  return (
    <div style={{ width: '100%', height }} aria-hidden="true">
      <Canvas
        camera={{ position: [0, 3, 8], fov: 40 }}
        dpr={[1, 1.5]}
        gl={{ antialias: true, alpha: true }}
        style={{ background: 'transparent' }}
      >
        <ambientLight intensity={0.7} />
        <directionalLight position={[4, 6, 4]} intensity={1.0} />
        <directionalLight position={[-3, 4, -3]} intensity={0.4} />

        <OrbitCamera />
        <FloorGrid count={bars.length} />

        {bars.map((bar, i) => (
          <Bar3D
            key={bar.label}
            x={startX + i * spacing}
            height={bar.value}
            maxHeight={maxValue}
            color={getColor(bar.value)}
            label={bar.label}
            value={bar.displayValue}
            index={i}
          />
        ))}
      </Canvas>
    </div>
  )
}
