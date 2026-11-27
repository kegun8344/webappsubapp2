import { useState, type ReactNode } from 'react'

interface InfoPopoverProps {
    /* Read by screen readers, e.g. "How the streak works" */
    label: string
    title: string
    children: ReactNode
}

/*
 * The little (i) button with an explanation, like the Bootstrap popovers in Subapp 1.
 * Opened with React state, so no Bootstrap JavaScript is needed. Escape or a click outside closes it.
 */
export default function InfoPopover({ label, title, children }: InfoPopoverProps) {
    const [open, setOpen] = useState(false)

    return (
        <span className="ms-auto align-self-start position-relative">
            <button
                type="button"
                className="info-btn"
                aria-label={label}
                aria-expanded={open}
                onClick={() => setOpen((current) => !current)}
                onBlur={() => setOpen(false)}
                onKeyDown={(event) => {
                    if (event.key === 'Escape') setOpen(false)
                }}
            >
                <i className="bi bi-info-circle" aria-hidden="true"></i>
            </button>

            {open && (
                <div className="card shadow position-absolute end-0 mt-2 text-start" style={{ width: 290, zIndex: 20 }} role="dialog" aria-label={title}>
                    <div className="card-header fw-semibold small">{title}</div>
                    <div className="card-body small p-3">{children}</div>
                </div>
            )}
        </span>
    )
}