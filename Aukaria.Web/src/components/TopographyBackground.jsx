import { useEffect, useMemo, useState } from "react"

const TWO_PI = Math.PI * 2

function seededRandom(seed) {
  let s = seed
  return () => {
    s = (s * 9301 + 49297) % 233280
    return s / 233280
  }
}

function clamp01(v) {
  return Math.max(0, Math.min(1, v))
}

function smoothstep(e0, e1, v) {
  const t = clamp01((v - e0) / (e1 - e0))
  return t * t * (3 - 2 * t)
}

function angDiff(a, b) {
  let d = (a - b) % TWO_PI
  if (d > Math.PI) d -= TWO_PI
  if (d < -Math.PI) d += TWO_PI
  return d
}

function buildVolcano(cx, cy, Rb, seed = 7) {
  const rand = seededRandom(seed)
  const valleys = []
  const baseA = 0.42
  let angle = baseA
  let mainValley = { a: baseA, d: 0 }
  for (let i = 0; i < 7; i++) {
    angle += (TWO_PI / 7) * (0.9 + rand() * 0.22)
    const valley = {
      a: angle,
      w: 0.14 + rand() * 0.08,
      d: 0.55 + rand() * 0.45,
    }
    valleys.push(valley)
    if (valley.d > mainValley.d) mainValley = valley
  }
  return { cx, cy, Rb, valleys, mainValley }
}

function radiusAt(v, theta) {
  return v.Rb * (1 + 0.07 * Math.cos(theta - 0.9) + 0.05 * Math.cos(2 * theta - 2.9))
}

function heightAt(v, d, theta) {
  const radius = Math.max(0.001, radiusAt(v, theta))
  let s = (d / radius) * (1 + 0.05 * Math.cos(theta - 4.2))
  const scr = 0.05
  let h
  if (s <= scr) {
    h = 0.965 + 0.035 * Math.pow(s / scr, 2)
  } else {
    const q = (s - scr) / (1 - scr)
    h = Math.pow(Math.max(0, 1 - q), 1.9)
  }
  if (s > 0.12 && s < 1.15 && h > 0) {
    const flank = Math.sin(Math.PI * clamp01((s - 0.15) / 0.6))
    let valley = 0
    for (const val of v.valleys) {
      const a = angDiff(theta, val.a)
      valley += Math.exp(-(a * a) / (2 * val.w * val.w)) * val.d * flank
    }
    h = Math.max(0, h - valley * 0.11)
  }
  return h
}

function peakField(x, y, v) {
  const dx = x - v.cx
  const dy = y - v.cy
  const d = Math.hypot(dx, dy)
  const theta = Math.atan2(dy, dx)
  const h = heightAt(v, d, theta)
  return { h, theta, d }
}

function ridges(seed) {
  const rand = seededRandom(seed)
  const count = 9
  const list = []
  for (let i = 0; i < count; i++) {
    const x0 = rand()
    const y0 = rand()
    const x1 = x0 + (rand() - 0.5) * 0.9
    const y1 = y0 + (rand() - 0.5) * 0.9
    const width = 0.06 + rand() * 0.1
    const strength = 0.15 + rand() * 0.28
    const angle = rand() * Math.PI
    list.push({ x0, y0, x1, y1, width, strength, angle })
  }
  return list
}

function noise2(x, y) {
  const value = Math.sin(x * 127.1 + y * 311.7) * 43758.5453123
  return value - Math.floor(value)
}

function valueNoise(x, y) {
  const ix = Math.floor(x)
  const iy = Math.floor(y)
  const fx = x - ix
  const fy = y - iy
  const ux = fx * fx * (3 - 2 * fx)
  const uy = fy * fy * (3 - 2 * fy)
  const a = noise2(ix, iy)
  const b = noise2(ix + 1, iy)
  const c = noise2(ix, iy + 1)
  const d = noise2(ix + 1, iy + 1)
  return (
    a * (1 - ux) * (1 - uy) +
    b * ux * (1 - uy) +
    c * (1 - ux) * uy +
    d * ux * uy
  )
}

function ridgeGradient(px, py, ridge, w, h) {
  const { x0, y0, x1, y1, width, strength, angle } = ridge
  const ux = x0 * w
  const uy = y0 * h
  const vx = x1 * w
  const vy = y1 * h
  const dist = Math.hypot(vx - ux, vy - uy) || 1
  const ex = (vx - ux) / dist
  const ey = (vy - uy) / dist
  const t = clamp01(((px - ux) * ex + (py - uy) * ey) / dist)
  const bx = ux + ex * t * dist
  const by = uy + ey * t * dist
  const perp = Math.hypot(px - bx, py - by)
  const sigma = width * w
  const env = Math.exp(-(perp * perp) / (2 * sigma * sigma))
  const k = -Math.cos(angle)
  const waveEnv = smoothstep(-1, 1, 2 * perp * k * 0.3)
  const falloff = smoothstep(0, 1, 1 - Math.abs(2 * t - 1))
  const g = (1 - env) * (0.6 + 0.4 * waveEnv) + env * 0.25
  return g * strength * falloff
}

function elevation(x, y, w, h, volcano, ridgeList) {
  let e = 0.06
  e += 0.6 * (1 - smoothstep(0.02, 0.55, Math.hypot((x / w - 0.62) * 1.7, (y / h - 0.7) * 1.3)))
  e += 0.4 * valueNoise(x * 0.006, y * 0.006)
  e += 0.26 * valueNoise(x * 0.014 + 31, y * 0.014 - 11)
  e += 0.14 * valueNoise(x * 0.03 + 71, y * 0.03 - 41)
  for (const rg of ridgeList) e += ridgeGradient(x, y, rg, w, h)
  return e
}

function fieldAt(x, y, w, h, volcano, ridgeList) {
  const base = elevation(x, y, w, h, volcano, ridgeList)
  const pk = peakField(x, y, volcano)
  const e = base + pk.h * 0.85
  const edge = 0.5 * smoothstep(1.4, 1.0, Math.hypot(x / w - 0.5, y / h - 0.5) * 2)
  return Math.max(0, e - edge)
}

function marchingContour(cols, w, h, volcano, ridgeList, z) {
  const paths = []
  const step = w / cols
  const rows = Math.ceil(h / step) + 1

  const val = (ix, iy) => {
    const x = (ix + 0.5) * step
    const y = (iy + 0.5) * step
    return fieldAt(x, y, w, h, volcano, ridgeList) - z
  }

  const edgeOf = (a, b) => {
    const va = a.v
    const vb = b.v
    if (Math.abs(va) < 1e-6 && Math.abs(vb) < 1e-6) return { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }
    const t = va / (va - vb)
    if (!isFinite(t) || t < 0 || t > 1) return { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }
    return { x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t }
  }

  const grid = new Map()
  const vertAt = (ix, iy) => {
    const key = `${ix},${iy}`
    if (!grid.has(key)) {
      grid.set(key, { x: (ix + 0.5) * step, y: (iy + 0.5) * step, v: val(ix, iy) })
    }
    return grid.get(key)
  }

  const processCell = (ix, iy) => {
    const a = vertAt(ix, iy)
    const b = vertAt(ix + 1, iy)
    const c = vertAt(ix + 1, iy + 1)
    const d = vertAt(ix, iy + 1)
    const segs = []

    const check = (p1, p2) => {
      if ((p1.v >= 0) === (p2.v >= 0)) return
      const e = edgeOf(p1, p2)
      segs.push([e.x, e.y])
    }

    check(a, b)
    check(b, c)
    check(c, d)
    check(d, a)

    if (segs.length === 2) {
      paths.push({ pts: segs, z })
      return
    }
    if (segs.length === 4) {
      const midX = (a.x + b.x + c.x + d.x) / 4
      const midY = (a.y + b.y + c.y + d.y) / 4
      const midV = valAt(midX, midY)
      if ((a.v >= 0) === (midV >= 0)) {
        paths.push({ pts: [segs[0], segs[1]], z })
        paths.push({ pts: [segs[2], segs[3]], z })
      } else {
        paths.push({ pts: [segs[0], segs[3]], z })
        paths.push({ pts: [segs[1], segs[2]], z })
      }
    }
  }

  function valAt(px, py) {
    return fieldAt(px, py, w, h, volcano, ridgeList) - z
  }

  grid.clear()

  for (let iy = 0; iy < rows - 1; iy++) {
    for (let ix = 0; ix < cols - 1; ix++) {
      processCell(ix, iy)
    }
  }

  return paths
}

export default function TopographyBackground() {
  const [size, setSize] = useState({ w: 1, h: 1 })

  useEffect(() => {
    const update = () => setSize({ w: window.innerWidth, h: window.innerHeight })
    update()
    window.addEventListener("resize", update)
    return () => window.removeEventListener("resize", update)
  }, [])

  const { layers, river, summitText, peak } = useMemo(() => {
    const { w, h } = size
    const mn = Math.min(w, h)
    const Rb = Math.min(mn * 0.34, w * 0.3)
    const volcano = buildVolcano(w * 0.62, h * 0.7, Rb, 7)
    const ridgeList = ridges(97)
    const cols = 110

    const layers = []
    const zLevels = [0.08, 0.16, 0.24, 0.32, 0.4, 0.48, 0.56, 0.64, 0.72, 0.8, 0.87, 0.93, 0.98, 1.0]
    for (const [i, z] of zLevels.entries()) {
      const t = i / (zLevels.length - 1)
      const opacity = 0.08 + 0.14 * t
      const pts = marchingContour(cols, w, h, volcano, ridgeList, z)
      layers.push({ z, opacity, pts })
    }

    const valleyPt = contourValleyStart(volcano)
    const river = buildRiver(volcano, valleyPt, w, h)

    const summitText = { x: volcano.cx, y: volcano.cy - Rb * 0.1 - 12 }

    return { layers, river, summitText, peak: { x: volcano.cx, y: volcano.cy } }
  }, [size])

  return (
    <div aria-hidden className="pointer-events-none fixed inset-0 z-0 overflow-hidden">
      <svg
        className="h-full w-full"
        viewBox={`0 0 ${size.w} ${size.h}`}
        preserveAspectRatio="xMidYMid slice"
      >
        {layers.map((layer, li) => (
          <g key={`layer-${li}`} stroke="#3f3f46" strokeOpacity={layer.opacity} strokeWidth={1.1} fill="none" vectorEffect="non-scaling-stroke">
            {layer.pts.map((seg, si) => (
              <path key={`${li}-${si}`} d={`M${seg.pts[0][0].toFixed(1)} ${seg.pts[0][1].toFixed(1)} L${seg.pts[1][0].toFixed(1)} ${seg.pts[1][1].toFixed(1)}`} />
            ))}
          </g>
        ))}
        {river && (
          <path
            d={river}
            fill="none"
            stroke="#3f3f46"
            strokeOpacity={0.2}
            strokeWidth={1.2}
            strokeDasharray="3 6"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
        )}
        {peak && (
          <circle cx={peak.x} cy={peak.y} r={3} fill="#3f3f46" fillOpacity={0.5} />
        )}
        {summitText && (
          <text
            x={summitText.x}
            y={summitText.y}
            textAnchor="middle"
            fill="#3f3f46"
            fillOpacity={0.55}
            fontSize={11}
            fontWeight={600}
            letterSpacing={0.5}
          >
            5321 m
          </text>
        )}
      </svg>
    </div>
  )
}

function contourValleyStart(volcano) {
  const a = volcano.mainValley.a
  const radius = radiusAt(volcano, a)
  return { x: volcano.cx + Math.cos(a) * radius * 0.98, y: volcano.cy + Math.sin(a) * radius * 0.98 }
}

function buildRiver(volcano, start, w, h) {
  const sdx = Math.sign(start.x - volcano.cx) || 1
  const sdy = Math.sign(start.y - volcano.cy) || 1
  let x = start.x
  let y = start.y
  let i = 0
  const pts = [[x, y]]

  while (i < 120 && x > -20 && x < w + 20 && y > -20 && y < h + 20) {
    i++
    const n1 = Math.sin(i * 0.11 + Math.cos(start.x * 0.013)) * 1.1
    const n2 = Math.sin(i * 1.7 + 3.1) * 1.6 + Math.sin(i * 0.53 + 0.4) * 1.1
    x += -sdx * 1.4 + n1 * 0.9
    y += sdy * 1.4 + n2 * 0.5
    pts.push([x, y])
  }
  return pts.map((p, k) => `${k === 0 ? "M" : "L"}${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join(" ")
}