import { useState, type FormEvent, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import ErrorAlert from './ErrorAlert'
import { ApiError } from '../services/apiClient'
import { moveAllWithoutTeacher } from '../services/adminService'
import type { TeacherOption } from '../types/admin'
import type { StaffHome as StaffHomeData } from '../types/home'

interface StaffHomeProps {
    home: StaffHomeData
    isAdmin: boolean
    /* Called after "Move all", so the Home page can show the message and load again */
    onQuizzesMoved: (message: string) => void
}

interface MoveAllFormProps {
    count: number
    teachers: TeacherOption[]
    onMoved: (message: string) => void
}

/* Administrators: give every quiz without a teacher to one teacher in one go (QuizTeacher/MoveAll in Subapp 1) */
function MoveAllForm({ count, teachers, onMoved }: MoveAllFormProps) {
    const [teacherId, setTeacherId] = useState('')
    const [busy, setBusy] = useState(false)
    const [error, setError] = useState<string | null>(null)

    async function onSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        if (!teacherId) {
            setError('Choose a teacher to move the quizzes to.')
            return
        }

        setBusy(true)
        setError(null)

        try {
            const result = await moveAllWithoutTeacher(teacherId)
            onMoved(`${result.moved} ${result.moved === 1 ? 'quiz' : 'quizzes'} moved to ${result.teacherEmail}.`)
        } catch (err) {
            setError(err instanceof ApiError ? err.message : 'The quizzes could not be moved. Please try again.')
        } finally {
            setBusy(false)
        }
    }

    return (
        <>
            <ErrorAlert message={error} onClose={() => setError(null)} />
            <form onSubmit={(event) => void onSubmit(event)} className="d-flex flex-wrap align-items-center gap-2 mb-3" noValidate>
                <label htmlFor="move-all-teacher" className="small text-secondary">Move all {count} to</label>
                <select
                    id="move-all-teacher"
                    className="form-select form-select-sm w-auto"
                    value={teacherId}
                    onChange={(event) => setTeacherId(event.target.value)}
                    required
                >
                    <option value="">Choose a teacher</option>
                    {teachers.map((teacher) => (
                        <option key={teacher.id} value={teacher.id}>{teacher.email}</option>
                    ))}
                </select>
                <button type="submit" className="btn btn-primary btn-sm" disabled={busy}>
                    {busy ? 'Moving...' : 'Move all'}
                </button>
            </form>
        </>
    )
}

/* One of the small number cards */
function StatCard({ title, children }: { title: string; children: ReactNode }) {
    return (
        <div className="col-6 col-lg-3">
            <div className="card border-0 shadow-sm h-100">
                <div className="card-body">
                    <h3 className="h6 text-secondary mb-2">{title}</h3>
                    <p className="fs-2 fw-bold mb-0">{children}</p>
                </div>
            </div>
        </div>
    )
}

/*
 * The Home page for teachers and administrators, the same sections as Home/Index in Subapp 1:
 * the numbers, quizzes that need attention, locked accounts (admin), recent activity,
 * the teacher's quizzes, and for administrators the popular quizzes and new users.
 */
export default function StaffHome({ home, isAdmin, onQuizzesMoved }: StaffHomeProps) {
    return (
        <>
            {!isAdmin && (
                <section className="mt-5" aria-labelledby="teacher-stats-heading">
                    <h2 id="teacher-stats-heading" className="visually-hidden">Your numbers</h2>
                    <div className="row g-3">
                        <StatCard title="Your quizzes">{home.yourQuizzes.length}</StatCard>
                        <StatCard title="Attempts">{home.totalAttempts}</StatCard>
                        <StatCard title="Average score">{home.averageScore !== null ? `${home.averageScore}%` : '–'}</StatCard>
                        <StatCard title="Students">{home.studentsReached}</StatCard>
                    </div>
                </section>
            )}

            {isAdmin && home.platform && (
                <section className="mt-5" aria-labelledby="platform-stats-heading">
                    <h2 id="platform-stats-heading" className="visually-hidden">Platform numbers</h2>
                    <div className="row g-3">
                        <StatCard title="Students">{home.platform.students}</StatCard>
                        <StatCard title="Teachers">{home.platform.teachers}</StatCard>
                        <StatCard title="Quizzes">
                            {home.platform.officialQuizzes} <span className="fs-6 fw-normal text-secondary">+ {home.platform.practiceQuizzes} practice</span>
                        </StatCard>
                        <StatCard title="Attempts this week">{home.platform.attemptsThisWeek}</StatCard>
                    </div>
                </section>
            )}

            {/* Teachers always see the section (with a "looks good" message), administrators only when something is wrong */}
            {(!isAdmin || home.needsAttention.length > 0) && (
                <section className="mt-5" aria-labelledby="attention-heading">
                    <h2 id="attention-heading" className="h5 fw-bold mb-3">Needs attention</h2>

                    {isAdmin && home.needsAttention.length > 1 && home.teacherOptions.length > 0 && (
                        <MoveAllForm count={home.needsAttention.length} teachers={home.teacherOptions} onMoved={onQuizzesMoved} />
                    )}

                    {home.needsAttention.length === 0 ? (
                        <div className="card border-0 shadow-sm">
                            <div className="card-body d-flex align-items-center gap-3 p-4">
                                <i className="bi bi-check-circle-fill text-success fs-4" aria-hidden="true"></i>
                                <p className="mb-0">All your quizzes look good.</p>
                            </div>
                        </div>
                    ) : (
                        <ul className="list-group shadow-sm">
                            {home.needsAttention.map((quiz) => (
                                <li key={quiz.quizId} className="list-group-item d-flex flex-wrap align-items-center gap-3 py-3">
                                    <div className="flex-grow-1">
                                        <p className="fw-semibold mb-0">{quiz.title}</p>
                                        <p className="small text-secondary mb-0">{quiz.moduleName}</p>
                                    </div>
                                    <div className="d-flex flex-wrap gap-1">
                                        {quiz.problems.map((problem) => (
                                            <span key={problem} className="badge bg-warning-subtle text-warning-emphasis border border-warning-subtle">{problem}</span>
                                        ))}
                                    </div>
                                    <Link className="btn btn-outline-primary btn-sm" to={`/quizzes/${quiz.quizId}/edit`} aria-label={`Fix ${quiz.title}`}>
                                        Fix
                                    </Link>
                                    {isAdmin && (
                                        <Link className="btn btn-outline-secondary btn-sm" to={`/quizzes/${quiz.quizId}/move`} aria-label={`Move ${quiz.title} to another teacher`}>
                                            Move
                                        </Link>
                                    )}
                                </li>
                            ))}
                        </ul>
                    )}
                </section>
            )}

            {isAdmin && home.lockedUsers.length > 0 && (
                <section className="mt-5" aria-labelledby="locked-heading">
                    <h2 id="locked-heading" className="h5 fw-bold mb-3">Locked accounts</h2>
                    <ul className="list-group shadow-sm">
                        {home.lockedUsers.map((locked) => (
                            <li key={locked.id} className="list-group-item d-flex flex-wrap align-items-center gap-3 py-3">
                                <div className="flex-grow-1 text-break">
                                    <p className="fw-semibold mb-0">{locked.email}</p>
                                    <p className="small text-secondary mb-0">
                                        {locked.lockedByAdmin
                                            ? 'Locked by an administrator'
                                            : `Too many wrong passwords, locked until ${new Date(locked.lockedUntil).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}`}
                                    </p>
                                </div>
                                <Link className="btn btn-outline-primary btn-sm" to={`/users?search=${encodeURIComponent(locked.email)}`} aria-label={`Manage ${locked.email}`}>
                                    Manage
                                </Link>
                            </li>
                        ))}
                    </ul>
                </section>
            )}

            <div className="row g-4 mt-1">
                <section className="col-lg-5" aria-labelledby="activity-heading">
                    <h2 id="activity-heading" className="h5 fw-bold mb-3 mt-4">Recent activity</h2>
                    {home.recentActivity.length === 0 ? (
                        <p className="text-secondary">No students have taken {isAdmin ? 'any' : 'your'} quizzes yet.</p>
                    ) : (
                        <ul className="list-group shadow-sm">
                            {home.recentActivity.map((attempt, index) => (
                                <li key={`${attempt.quizId}-${index}`} className="list-group-item d-flex align-items-center gap-3 py-2">
                                    <div className="flex-grow-1 text-break">
                                        <span className="fw-semibold">{attempt.displayName}</span>
                                        <span className="d-block small text-secondary">{attempt.quizTitle}</span>
                                    </div>
                                    <span className="fw-semibold">{attempt.score}%</span>
                                    <span className="small text-secondary text-nowrap">{attempt.timeAgo}</span>
                                </li>
                            ))}
                        </ul>
                    )}
                </section>

                {!isAdmin && (
                    <section className="col-lg-7" aria-labelledby="your-quizzes-heading">
                        <h2 id="your-quizzes-heading" className="h5 fw-bold mb-3 mt-4">Your quizzes</h2>
                        {home.yourQuizzes.length === 0 ? (
                            <div className="card border-0 shadow-sm">
                                <div className="card-body">
                                    <p className="text-secondary mb-3">You have not made any quizzes yet.</p>
                                    <Link to="/quizzes/new" className="btn btn-primary">Create a quiz</Link>
                                </div>
                            </div>
                        ) : (
                            <div className="card border-0 shadow-sm">
                                <div className="table-responsive">
                                    <table className="table align-middle mb-0">
                                        <thead>
                                            <tr>
                                                <th scope="col" className="ps-3">Quiz</th>
                                                <th scope="col" className="text-end">Attempts</th>
                                                <th scope="col" className="text-end">Average</th>
                                                <th scope="col" className="pe-3"><span className="visually-hidden">Analysis</span></th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {home.yourQuizzes.map((quiz) => (
                                                <tr key={quiz.quizId}>
                                                    <td className="ps-3 text-break">
                                                        {quiz.title}
                                                        <span className="d-block small text-secondary">
                                                            {quiz.moduleName} · {quiz.questionCount} {quiz.questionCount === 1 ? 'question' : 'questions'}
                                                        </span>
                                                    </td>
                                                    <td className="text-end">{quiz.attempts}</td>
                                                    <td className="text-end pe-3">{quiz.averageScore !== null ? `${quiz.averageScore}%` : '–'}</td>
                                                    <td className="pe-3 text-end">
                                                        <Link className="btn btn-outline-primary btn-sm" to={`/quizzes/${quiz.quizId}/analysis`} aria-label={`Analysis of ${quiz.title}`}>
                                                            Analysis
                                                        </Link>
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        )}
                    </section>
                )}

                {isAdmin && (
                    <div className="col-lg-7">
                        <section aria-labelledby="popular-heading">
                            <h2 id="popular-heading" className="h5 fw-bold mb-3 mt-4">Popular this week</h2>
                            {home.popularThisWeek.length === 0 ? (
                                <p className="text-secondary">No quizzes have been taken this week.</p>
                            ) : (
                                <ul className="list-group shadow-sm">
                                    {home.popularThisWeek.map((quiz) => (
                                        <li key={quiz.quizId} className="list-group-item d-flex align-items-center gap-3 py-2">
                                            <div className="flex-grow-1 text-break">
                                                <span className="fw-semibold">{quiz.title}</span>
                                                <span className="d-block small text-secondary">{quiz.moduleName}</span>
                                            </div>
                                            <span className="text-end text-nowrap">
                                                <span className="fw-semibold">{quiz.attempts}</span>
                                                <span className="d-block small text-secondary">{quiz.attempts === 1 ? 'attempt' : 'attempts'}</span>
                                            </span>
                                            <span className="text-end text-nowrap" style={{ minWidth: '3.5rem' }}>
                                                <span className="fw-semibold">{quiz.averageScore !== null ? `${quiz.averageScore}%` : '–'}</span>
                                                <span className="d-block small text-secondary">average</span>
                                            </span>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </section>

                        <section aria-labelledby="new-users-heading">
                            <h2 id="new-users-heading" className="h5 fw-bold mb-3 mt-4">New users</h2>
                            {home.newUsers.length === 0 ? (
                                <p className="text-secondary">No users have signed up yet.</p>
                            ) : (
                                <ul className="list-group shadow-sm">
                                    {home.newUsers.map((newUser) => (
                                        <li key={newUser.id} className="list-group-item d-flex flex-wrap align-items-center gap-2 py-2">
                                            <span className="flex-grow-1 text-break">
                                                {newUser.email}
                                                <span className={`badge ${newUser.isTeacher ? 'bg-primary' : 'bg-secondary'} ms-1`}>{newUser.isTeacher ? 'Teacher' : 'Student'}</span>
                                            </span>
                                            <span className="small text-secondary text-nowrap">{newUser.timeAgo}</span>
                                            <Link className="btn btn-outline-secondary btn-sm" to={`/users?search=${encodeURIComponent(newUser.email)}`} aria-label={`Manage ${newUser.email}`}>
                                                Manage
                                            </Link>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </section>
                    </div>
                )}
            </div>
        </>
    )
}