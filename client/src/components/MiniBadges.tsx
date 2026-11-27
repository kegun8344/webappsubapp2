import BadgeIcon from './BadgeIcon'
import type { MiniBadge } from '../types/stats'

/*
 * A row of small badge icons in their tier colour (Bronze to Diamond), like _MiniBadges.cshtml in Subapp 1.
 * The name is in the title and in hidden text, so the icons never carry meaning alone.
 */
export default function MiniBadges({ badges }: { badges: MiniBadge[] }) {
    return (
        <>
            {badges.map((badge) => (
                <span key={badge.type} className={`achievement-${badge.tier.toLowerCase()} lb-mini`} title={`${badge.name} (${badge.tier})`}>
                    <span className="achievement-icon">
                        <BadgeIcon type={badge.type} />
                    </span>
                    <span className="visually-hidden">
                        {badge.name}, {badge.tier}
                    </span>
                </span>
            ))}
        </>
    )
}