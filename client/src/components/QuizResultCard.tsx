import type { CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import BadgeIcon from './BadgeIcon'
import type { Award, QuizResult } from '../types/play'

/* Stars needed for the daily goal. The same number as PointRules.DailyGoalStarsNeeded in the API. */
const DAILY_GOAL_STARS_NEEDED = 5

interface QuizResultCardProps {
    result: QuizResult
    onTryAgain: () => void
}

/* Colour and message from the score. Green is only for a perfect score. */
function scoreLook(score: number): { color: string; message: string } {
    if (score >= 100) return { color: 'success', message: 'Perfect score!' }
    if (score >= 90) return { color: 'primary', message: 'Almost there!' }
    if (score >= 50) return { color: 'warning', message: 'Good effort!' }
    return { color: 'danger', message: 'Keep practicing!' }
}

/* The text next to each kind of points */
function reasonText(award: Award, result: QuizResult): string {
    switch (award.reason) {
        case 'QuizScore':
            return result.previousBest === null
                ? 'Quiz score'
                : `New best: ${result.previousBest}% → ${result.bestScore}%`
        case 'FirstCompletion':
            return 'First time completing this quiz'
        case 'DailyGoal':
            return `Daily goal: ${DAILY_GOAL_STARS_NEEDED} stars today`
        case 'Streak':
            return result.streakDay >= 5 ? `Streak day ${result.streakDay} (max bonus)` : `Streak day ${result.streakDay}`
        default:
            return 'Points'
    }
}

/* The result after a quiz, the same layout as QuizPlayer/Result in Subapp 1 */
export default function QuizResultCard({ result, onTryAgain }: QuizResultCardProps) {
    const score = result.scorePercentage
    const { color, message } = scoreLook(score)
    const showProgress = result.resultSaved && result.awardsPoints

    const starsToday = Math.min(result.dailyGoalStarsToday, DAILY_GOAL_STARS_NEEDED)
    const goalReached = result.dailyGoalStarsToday >= DAILY_GOAL_STARS_NEEDED
    const levelPercent = result.pointsForLevel === 0 ? 0 : Math.floor((result.pointsIntoLevel * 100) / result.pointsForLevel)

    /* The glow around the card takes its colour from the score */
    const glowStyle = { '--result-rgb': `var(--bs-${color}-rgb)` } as CSSProperties

    return (
        <div className="mx-auto py-4" style={{ maxWidth: 520 }}>
            <div className="card border-0 text-center result-glow" style={glowStyle}>
                <div className="card-body p-4 p-md-5">
                    <p className="text-secondary mb-1">Quiz completed</p>
                    <h1 className="h4 fw-bold mb-4">{result.quizTitle}</h1>

                    <div className={`display-3 fw-bold text-${color}`}>{score}%</div>
                    <p className="fs-5 fw-semibold mb-1">{message}</p>
                    <p className="text-secondary mb-4">
                        {result.correctAnswers} of {result.totalQuestions} correct answers
                    </p>

                    <div className="progress mb-4" role="progressbar" aria-label="Score" aria-valuenow={score} aria-valuemin={0} aria-valuemax={100}>
                        <div className={`progress-bar bg-${color}`} style={{ width: `${score}%` }}></div>
                    </div>

                    {/* Points from this attempt. Four cases: not saved, practice quiz, got points, no new points. */}
                    <section className="text-start border-top pt-4 mb-4" aria-labelledby="points-heading">
                        <h2 id="points-heading" className="h6 text-secondary text-uppercase fw-semibold mb-3">Points</h2>

                        {!result.resultSaved ? (
                            <div className="alert alert-warning mb-0" role="alert">
                                Your result could not be saved, so no points were given. Please try again.
                            </div>
                        ) : !result.awardsPoints ? (
                            <p className="text-secondary mb-0">
                                This is a practice quiz, so it does not give points. Only quizzes made by teachers count towards your points.
                            </p>
                        ) : result.totalPoints > 0 ? (
                            <>
                                <p className="display-6 fw-bold text-primary mb-3">+{result.totalPoints} points</p>
                                <ul className="list-unstyled mb-0">
                                    {result.awards.map((award) => (
                                        <li key={award.reason} className="d-flex justify-content-between gap-3 py-1">
                                            <span>{reasonText(award, result)}</span>
                                            <span className="fw-semibold text-nowrap">+{award.points}</span>
                                        </li>
                                    ))}
                                </ul>
                            </>
                        ) : (
                            <>
                                <p className="fw-semibold mb-1">No new points this time</p>
                                <p className="text-secondary mb-0">
                                    {result.bestScore >= 100
                                        ? 'You already have full points on this quiz.'
                                        : `Your best on this quiz is ${result.bestScore}%. Beat it to earn more points.`}
                                </p>
                            </>
                        )}

                        {/* Daily goal stars from this attempt, and how far the student is today */}
                        {showProgress && (
                            <div className="border-top mt-3 pt-3">
                                <div className="d-flex align-items-center justify-content-between gap-3">
                                    <span className="small">
                                        {goalReached ? (
                                            <>
                                                <i className="bi bi-check-circle-fill text-success me-1" aria-hidden="true"></i>
                                                <strong>Daily goal reached</strong>
                                            </>
                                        ) : result.dailyGoalStarsEarned > 0 ? (
                                            <>
                                                <i className="bi bi-star-fill text-warning me-1" aria-hidden="true"></i>
                                                <strong>
                                                    +{result.dailyGoalStarsEarned} {result.dailyGoalStarsEarned === 1 ? 'star' : 'stars'}
                                                </strong>{' '}
                                                towards the daily goal
                                            </>
                                        ) : (
                                            <>
                                                <i className="bi bi-star text-secondary me-1" aria-hidden="true"></i>
                                                No new stars from this quiz today
                                            </>
                                        )}
                                    </span>
                                    <span className="small text-secondary text-nowrap">
                                        {starsToday} of {DAILY_GOAL_STARS_NEEDED} today
                                    </span>
                                </div>
                                {/* The text above says the same, so the bar is hidden from screen readers */}
                                <div className="progress mt-2" style={{ height: '0.45rem' }} aria-hidden="true">
                                    <div
                                        className={`progress-bar ${goalReached ? 'bg-success' : 'bg-warning'}`}
                                        style={{ width: `${(starsToday * 100) / DAILY_GOAL_STARS_NEEDED}%` }}
                                    ></div>
                                </div>
                            </div>
                        )}
                    </section>

                    {/* Level after this attempt. Celebrates a new level. */}
                    {showProgress && (
                        <section className="text-start border-top pt-4 mb-4" aria-labelledby="level-heading">
                            {result.leveledUp ? (
                                <div className="level-up text-center rounded-3 p-3 mb-3">
                                    <h2 id="level-heading" className="h4 fw-bold mb-1">
                                        <span aria-hidden="true">🎉</span> Level up!
                                    </h2>
                                    <p className="mb-0">
                                        You reached <strong>level {result.level}</strong>.
                                    </p>
                                </div>
                            ) : (
                                <h2 id="level-heading" className="h6 text-secondary text-uppercase fw-semibold mb-3">
                                    Level {result.level}
                                </h2>
                            )}

                            <div
                                className="progress mb-1"
                                role="progressbar"
                                aria-label={`Progress to level ${result.level + 1}`}
                                aria-valuenow={levelPercent}
                                aria-valuemin={0}
                                aria-valuemax={100}
                                aria-valuetext={`${result.pointsIntoLevel} of ${result.pointsForLevel} points`}
                            >
                                <div className="progress-bar" style={{ width: `${levelPercent}%` }}></div>
                            </div>
                            <p className="small text-secondary mb-0">
                                {result.pointsIntoLevel} / {result.pointsForLevel} points to level {result.level + 1}
                            </p>
                        </section>
                    )}

                    {/* Badges unlocked or upgraded by this attempt */}
                    {result.newBadges.length > 0 && (
                        <section className="text-start border-top pt-4 mb-4" aria-labelledby="new-badges-heading">
                            <h2 id="new-badges-heading" className="h6 text-secondary text-uppercase fw-semibold mb-3">
                                {result.newBadges.length === 1 ? 'New badge' : 'New badges'}
                            </h2>
                            <ul className="list-unstyled d-grid gap-3 mb-0">
                                {result.newBadges.map((badge) => (
                                    <li key={badge.type} className={`d-flex align-items-center gap-3 new-badge achievement-${badge.tier.toLowerCase()}`}>
                                        <div className="achievement-icon" aria-hidden="true">
                                            <BadgeIcon type={badge.type} />
                                        </div>
                                        <div>
                                            <p className="fw-bold mb-0">{badge.name}</p>
                                            <p className="small text-secondary mb-0">
                                                {badge.tier === 'Bronze' ? 'Unlocked: Bronze' : `Upgraded to ${badge.tier}`}
                                            </p>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        </section>
                    )}

                    <div className="d-grid gap-2">
                        {/* Offered when there are mistakes in this quiz to practise */}
                        {result.reviewCount > 0 && (
                            <Link to={`/review?quizId=${result.quizId}`} className="btn btn-warning">
                                Review mistakes ({result.reviewCount})
                            </Link>
                        )}
                        <button type="button" className="btn btn-outline-primary" onClick={onTryAgain}>
                            Try again
                        </button>
                        <Link to="/quizzes" className="btn btn-primary btn-hero">Back to quizzes</Link>
                        <Link to="/leaderboard" className="btn btn-outline-primary">See leaderboard</Link>
                        <Link to="/" className="btn btn-outline-secondary">Home</Link>
                    </div>
                </div>
            </div>
        </div>
    )
}