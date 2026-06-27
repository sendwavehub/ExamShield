import { useState } from 'react'

const STEP = 0.25
const MIN_SCALE = 0.5
const MAX_SCALE = 4

interface Props {
  src: string
  alt: string
}

export default function ImageViewer({ src, alt }: Props) {
  const [scale, setScale] = useState(1)
  const [rotation, setRotation] = useState(0)
  const [brightness, setBrightness] = useState(100)
  const [contrast, setContrast] = useState(100)

  const zoomIn = () => setScale(s => Math.min(MAX_SCALE, +(s + STEP).toFixed(2)))
  const zoomOut = () => setScale(s => Math.max(MIN_SCALE, +(s - STEP).toFixed(2)))
  const rotate = () => setRotation(r => (r + 90) % 360)
  const reset = () => { setScale(1); setRotation(0); setBrightness(100); setContrast(100) }

  const transform = `scale(${scale}) rotate(${rotation}deg)`
  const filter = `brightness(${brightness}%) contrast(${contrast}%)`

  return (
    <div className="flex flex-col gap-3 select-none">
      {/* Controls */}
      <div className="flex flex-wrap items-center gap-2 p-3 bg-[#161B22] rounded-xl border border-[#30363D]">
        <button
          title="Zoom in"
          onClick={zoomIn}
          className="px-2.5 py-1.5 bg-[#21262D] hover:bg-[#30363D] text-white rounded text-sm font-mono"
        >
          +
        </button>
        <button
          title="Zoom out"
          onClick={zoomOut}
          className="px-2.5 py-1.5 bg-[#21262D] hover:bg-[#30363D] text-white rounded text-sm font-mono"
        >
          −
        </button>
        <span className="text-[#8B949E] text-xs w-10 text-center">{Math.round(scale * 100)}%</span>

        <div className="w-px h-5 bg-[#30363D] mx-1" />

        <button
          title="Rotate 90°"
          onClick={rotate}
          className="px-2.5 py-1.5 bg-[#21262D] hover:bg-[#30363D] text-white rounded text-sm"
        >
          ↻
        </button>

        <div className="w-px h-5 bg-[#30363D] mx-1" />

        <label htmlFor="brightness" className="text-[#8B949E] text-xs">Brightness</label>
        <input
          id="brightness"
          type="range"
          min={50}
          max={200}
          value={brightness}
          onChange={e => setBrightness(Number(e.target.value))}
          className="w-20 accent-[#00BFFF]"
          aria-label="Brightness"
        />
        <span className="text-[#8B949E] text-xs w-8">{brightness}%</span>

        <label htmlFor="contrast" className="text-[#8B949E] text-xs">Contrast</label>
        <input
          id="contrast"
          type="range"
          min={50}
          max={200}
          value={contrast}
          onChange={e => setContrast(Number(e.target.value))}
          className="w-20 accent-[#00BFFF]"
          aria-label="Contrast"
        />
        <span className="text-[#8B949E] text-xs w-8">{contrast}%</span>

        <div className="w-px h-5 bg-[#30363D] mx-1" />

        <button
          title="Reset"
          onClick={reset}
          className="px-2.5 py-1.5 bg-[#21262D] hover:bg-[#30363D] text-[#8B949E] rounded text-xs"
        >
          Reset
        </button>
      </div>

      {/* Image canvas */}
      <div className="overflow-auto bg-[#0D1117] rounded-xl border border-[#30363D] min-h-64 flex items-center justify-center p-4">
        <img
          src={src}
          alt={alt}
          draggable={false}
          style={{ transform, filter, transition: 'transform 0.15s ease, filter 0.15s ease', maxWidth: 'none' }}
          className="block"
        />
      </div>
    </div>
  )
}
