export default function ColombiaFlag({ className = "h-3.5 w-[1.15rem]", ariaLabel = "Colombia" }) {
  return (
    <span className={`inline-flex shrink-0 overflow-hidden rounded-[3px] ring-1 ring-black/15 ${className}`}>
      <svg
        viewBox="0 0 16 12"
        className="h-full w-full"
        role="img"
        aria-label={ariaLabel}
        focusable="false"
      >
        <rect x="0" y="0" width="16" height="6" fill="#FCD116" />
        <rect x="0" y="6" width="16" height="3" fill="#003893" />
        <rect x="0" y="9" width="16" height="3" fill="#CE1126" />
      </svg>
    </span>
  )
}