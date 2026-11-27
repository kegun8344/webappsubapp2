import { useEffect, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import BadgeIcon from '../components/BadgeIcon'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import { getDashboard } from '../services/statsService'
import type { Dashboard } from '../types/stats'
import { formatDate } from '../utils/dates'

/* The student's own dashboard: level, stats, badges and recent attempts. Same layout as Dashboard/Index in Subapp 1. */
export default function DashboardPage() {
    const [dashboard, setDashboard] = useState<Dashboard | null>(null)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        let active = true

        getDashboard()
            .then((data) => {
                if (active) setDashboard(data)
            })
            .catch(() => {
                if (active) setError('Your dashboard could not be loaded. Please try again later.')
            })

        return () => {
            active = false
        }
    }, [])

    if (error) {
        return (
            <>
                <h1 className="landing-title mb-2">Dashboard</h1>
                <ErrorAlert message={error} />
            </>
        )
    }

    if (!dashboard) {
        return <LoadingSpinner message="Loading your dashboard..." />
    }

    const progressPercent = dashboard.pointsForLevel === 0 ? 0 : Math.floor((dashboard.pointsIntoLevel * 100) / dashboard.pointsForLevel)

    /* Total points needed for the next level, e.g. 2560 / 3000 */
    const nextLevelAt = dashboard.totalPoints - dashboard.pointsIntoLevel + dashboard.pointsForLevel

    return (
        <>
            <h1 className="landing-title mb-2">Dashboard</h1>
            <p className="lead text-secondary mb-5">Here is how you are doing.</p>

            <div className="row g-3 mb-5">
                {/* Level: a ring showing the progress to the next level */}
                <section className="col-md-5" aria-labelledby="level-heading">
                    <div className="card border-0 shadow-sm h-100">
                        <div className="card-body d-flex flex-column align-items-center justify-content-center text-center p-4">
                            <h2 id="level-heading" className="h5 text-secondary fw-semibold mb-0">Level</h2>
                            <div
                                className="level-ring my-3"
                                role="progressbar"
                                aria-label={`Progress to level ${dashboard.level + 1}`}
                                aria-valuenow={progressPercent}
                                aria-valuemin={0}
                                aria-valuemax={100}
                                aria-valuetext={`Level ${dashboard.level}, ${dashboard.totalPoints} of ${nextLevelAt} points to the next level`}
                            >
                                <svg viewBox="0 0 120 120" aria-hidden="true">
                                    <circle className="level-ring-track" cx="60" cy="60" r="52" />
                                    {/* pathLength="100" lets the dash length be the percentage directly */}
                                    {progressPercent > 0 && (
                                        <circle className="level-ring-fill" cx="60" cy="60" r="52" pathLength={100} strokeDasharray={`${progressPercent} 100`} />
                                    )}
                                </svg>
                                <span className="level-ring-number" aria-hidden="true">{dashboard.level}</span>
                            </div>
                            <p className="fs-5 fw-semibold mb-0">
                                {dashboard.totalPoints} / {nextLevelAt}
                            </p>
                            <p className="small text-secondary mb-0">points to level {dashboard.level + 1}</p>
                        </div>
                    </div>
                </section>

                <section className="col-md-7" aria-labelledby="stats-heading">
                    <h2 id="stats-heading" className="visually-hidden">Your stats</h2>
                    <div className="dashboard-stats">
                        <StatCard title="Quizzes completed">
                            <p className="fs-2 fw-bold mb-0">{dashboard.quizzesCompleted}</p>
                        </StatCard>

                        <StatCard title="Streak">
                            <p className="fs-2 fw-bold mb-0">
                                {dashboard.streakDays} <span className="fs-6 fw-normal text-secondary">{dashboard.streakDays === 1 ? 'day' : 'days'}</span>
                            </p>
                        </StatCard>

                        <StatCard title="Average score">
                            <p className="fs-2 fw-bold mb-0">
                                {dashboard.averageScore !== null ? (
                                    `${dashboard.averageScore}%`
                                ) : (
                                    <>
                                        <span aria-hidden="true">–</span>
                                        <span className="visually-hidden">No quizzes yet</span>
                                    </>
                                )}
                            </p>
                        </StatCard>

                        <div>
                            <div className="card border-0 shadow-sm h-100">
                                <div className="card-body d-flex flex-column">
                                    <h3 className="h6 text-secondary mb-2">Leaderboard</h3>
                                    <p className="mb-2">
                                        <span className="fs-2 fw-bold">#{dashboard.leaderboardRank}</span>{' '}
                                        <span className="text-secondary">
                                            of {dashboard.studentCount} {dashboard.studentCount === 1 ? 'student' : 'students'}
                                        </span>
                                    </p>
                                    <Link to="/leaderboard" className="small mt-auto stretched-link">See leaderboard</Link>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>
            </div>

            {/* Every badge is shown, locked ones in grey with what is needed to unlock them */}
            {dashboard.badges.length > 0 && (
                <section className="mb-5" aria-labelledby="badges-heading">
                    <h2 id="badges-heading" className="h4 fw-bold mb-3">Badges</h2>
                    <ul className="row g-3 list-unstyled mb-0">
                        {dashboard.badges.map((badge) => (
                            <li className="col-12 col-sm-6 col-lg-4" key={badge.type}>
                                <div className={`card border-0 shadow-sm h-100 achievement-${badge.tier.toLowerCase()}`}>
                                    <div className="card-body d-flex flex-column">
                                        <div className="d-flex align-items-center gap-3 mb-2">
                                            <div className="achievement-icon" aria-hidden="true">
                                                <BadgeIcon type={badge.type} />
                                            </div>
                                            <div className="achievement-text">
                                                <h3 className="h6 fw-bold mb-0 text-break">{badge.name}</h3>
                                                <p className="small fw-semibold mb-0 achievement-tier">{badge.isLocked ? 'Locked' : badge.tier}</p>
                                            </div>
                                        </div>
                                        <p className="small text-secondary mb-3">{badge.description}</p>
                                        <div className="mt-auto">
                                            {badge.nextThreshold === null ? (
                                                <p className="small fw-semibold mb-0">Max level reached</p>
                                            ) : (
                                                <>
                                                    <div
                                                        className="progress mb-1"
                                                        style={{ height: '0.4rem' }}
                                                        role="progressbar"
                                                        aria-label={`${badge.name} progress to ${badge.nextTier}`}
                                                        aria-valuenow={badge.progressPercent}
                                                        aria-valuemin={0}
                                                        aria-valuemax={100}
                                                        aria-valuetext={`${badge.value} of ${badge.nextThreshold} ${badge.unit}`}
                                                    >
                                                        <div className="progress-bar" style={{ width: `${badge.progressPercent}%` }}></div>
                                                    </div>
                                                    <p className="small text-secondary mb-0">
                                                        {badge.value} / {badge.nextThreshold} {badge.unit} to {badge.nextTier}
                                                    </p>
                                                </>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            </li>
                        ))}
                    </ul>
                </section>
            )}

            <section className="mb-5" aria-labelledby="recent-heading">
                <h2 id="recent-heading" className="h4 fw-bold mb-3">Recent attempts</h2>

                {dashboard.recentAttempts.length === 0 ? (
                    <div className="card border-0 shadow-sm">
                        <div className="card-body">
                            <p className="text-secondary mb-3">You have not taken any quizzes yet.</p>
                            <Link to="/quizzes" className="btn btn-primary">Find a quiz</Link>
                        </div>
                    </div>
                ) : (
                    <div className="card border-0 shadow-sm">
                        <div className="table-responsive">
                            <table className="table align-middle mb-0">
                                <thead>
                                    <tr>
                                        <th scope="col" className="ps-3">Quiz</th>
                                        <th scope="col">Score</th>
                                        <th scope="col" className="pe-3 d-none d-sm-table-cell">Date</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {dashboard.recentAttempts.map((attempt, index) => (
                                        <tr key={`${attempt.quizId}-${index}`}>
                                            <td className="ps-3 text-break">
                                                {attempt.quizTitle}
                                                {!attempt.awardsPoints && <span className="badge text-bg-light border ms-1">Practice</span>}
                                                {/* On phones the date column is hidden, so the date is shown here instead */}
                                                <div className="small text-secondary d-sm-none">{formatDate(attempt.completedAt)}</div>
                                            </td>
                                            <td className="fw-semibold">{attempt.score}%</td>
                                            <td className="pe-3 text-secondary text-nowrap d-none d-sm-table-cell">{formatDate(attempt.completedAt)}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
            </section>
        </>
    )
}

/* One of the small stat cards */
function StatCard({ title, children }: { title: string; children: ReactNode }) {
    return (
        <div>
            <div className="card border-0 shadow-sm h-100">
                <div className="card-body">
                    <h3 className="h6 text-secondary mb-2">{title}</h3>
                    {children}
                </div>
            </div>
        </div>
    )
}