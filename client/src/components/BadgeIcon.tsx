/* Badge icons from Bootstrap Icons, the same as _BadgeIcon.cshtml in Subapp 1. Decorative only: the badge name is always shown as text. */
const icons: Record<string, string> = {
    Explorer: 'bi-compass',
    Perfectionist: 'bi-bullseye',
    OnFire: 'bi-fire',
    Comeback: 'bi-graph-up-arrow',
    BusyBee: 'bi-calendar-check',
    PracticeMakesPerfect: 'bi-arrow-repeat',
}

export default function BadgeIcon({ type }: { type: string }) {
    return <i className={`bi ${icons[type] ?? 'bi-award'}`} aria-hidden="true"></i>
}