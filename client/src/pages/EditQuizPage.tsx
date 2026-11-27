import { useCallback, useEffect, useState } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import ErrorAlert from '../components/ErrorAlert'
import LoadingSpinner from '../components/LoadingSpinner'
import QuizDetailsForm from '../components/QuizDetailsForm'
import SuccessToast from '../components/SuccessToast'
import TagEditor from '../components/TagEditor'
import { useAuth } from '../hooks/useAuth'
import { ApiError } from '../services/apiClient'
import { deleteQuestion } from '../services/questionService'
import { getModuleOptions, getQuiz, getTags, setQuizVisibility, updateQuiz } from '../services/quizService'
import type { ModuleOption, Question, QuizDetail, Tag } from '../types/quiz'
import { toQuizRequest, type QuizFormValues } from '../utils/quizValidation'

/* The quiz loaded for one quiz ID */
interface LoadedQuiz {
    id: number | null
    quiz: QuizDetail | null
    error: string | null
}

function loadErrorMessage(err: unknown): string {
    if (err instanceof ApiError && err.status === 404) return 'This quiz was not found.'
    return err instanceof ApiError ? err.message : 'The quiz could not be loaded. Please try again.'
}

/* Edit quiz: the details, the tags and the questions. Same sections as Quiz/Edit in Subapp 1. */
export default function EditQuizPage() {
    const quizId = Number(useParams().id)
    const location = useLocation()
    const { hasRole } = useAuth()

    const [loaded, setLoaded] = useState<LoadedQuiz>({ id: null, quiz: null, error: null })
    const [catalog, setCatalog] = useState<Tag[]>([])
    const [modules, setModules] = useState<ModuleOption[]>([])

    /* A message from the page that sent the user here, e.g. "Quiz created" */
    const [message, setMessage] = useState<string | null>((location.state as { message?: string } | null)?.message ?? null)
    const [actionError, setActionError] = useState<string | null>(null)

    /* Changing the key resets the details form to the saved values */
    const [formKey, setFormKey] = useState(0)

    const [questionToDelete, setQuestionToDelete] = useState<{ question: Question; number: number } | null>(null)
    const [deleting, setDeleting] = useState(false)
    const [changingVisibility, setChangingVisibility] = useState(false)

    const isLoading = loaded.id !== quizId

    /* Loads the quiz and the tag catalog again after a change */
    const reload = useCallback(async () => {
        const [quiz, tags] = await Promise.all([getQuiz(quizId), getTags()])
        setLoaded({ id: quizId, quiz, error: null })
        setCatalog(tags)
    }, [quizId])

    useEffect(() => {
        let active = true

        getQuiz(quizId)
            .then((quiz) => {
                if (active) setLoaded({ id: quizId, quiz, error: null })
            })
            .catch((err: unknown) => {
                if (active) setLoaded({ id: quizId, quiz: null, error: loadErrorMessage(err) })
            })

        /* The dropdowns. If they fail, they are just empty. */
        getTags()
            .then((tags) => {
                if (active) setCatalog(tags)
            })
            .catch(() => {})

        getModuleOptions()
            .then((result) => {
                if (active) setModules(result)
            })
            .catch(() => {})

        return () => {
            active = false
        }
    }, [quizId])

    const closeMessage = useCallback(() => setMessage(null), [])

    if (isLoading) {
        return <LoadingSpinner message="Loading quiz..." />
    }

    const quiz = loaded.quiz

    if (!quiz) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <ErrorAlert message={loaded.error} />
                <Link to="/quizzes" className="btn btn-outline-primary">Back to quizzes</Link>
            </div>
        )
    }

    /* Only the creator or an administrator may edit. The API checks this on every change too. */
    if (!quiz.canManage) {
        return (
            <div className="mx-auto text-center py-5" style={{ maxWidth: 520 }}>
                <h1 className="h4 fw-bold mb-2">Access denied</h1>
                <p className="text-secondary mb-4">Only the creator of this quiz or an administrator can edit it.</p>
                <Link to="/quizzes" className="btn btn-outline-primary">Back to quizzes</Link>
            </div>
        )
    }

    const initialValues: QuizFormValues = {
        title: quiz.title,
        description: quiz.description ?? '',
        moduleId: String(quiz.moduleId),
        difficulty: quiz.difficulty,
    }

    async function saveDetails(values: QuizFormValues) {
        /* Errors are shown by the form */
        await updateQuiz(quizId, toQuizRequest(values))
        await reload()
        setFormKey((key) => key + 1)
        setMessage('Quiz updated.')
    }

    async function afterTagChange(text: string) {
        await reload()
        setMessage(text)
    }

    /* Publish or unpublish. The API refuses students and empty quizzes too. */
    async function changeVisibility(makePublic: boolean) {
        setChangingVisibility(true)
        setActionError(null)
        try {
            await setQuizVisibility(quizId, makePublic)
            await reload()
            setMessage(makePublic ? 'Quiz published. Students can now see it.' : 'Quiz unpublished. It is now only visible to you.')
        } catch (err) {
            setActionError(err instanceof ApiError ? err.message : 'The visibility could not be changed. Please try again.')
        } finally {
            setChangingVisibility(false)
        }
    }

    async function confirmDeleteQuestion() {
        if (!questionToDelete) return

        setDeleting(true)
        setActionError(null)
        try {
            await deleteQuestion(questionToDelete.question.id)
            await reload()
            setMessage('Question deleted.')
        } catch (err) {
            setActionError(err instanceof ApiError ? err.message : 'The question could not be deleted. Please try again.')
        } finally {
            setDeleting(false)
            setQuestionToDelete(null)
        }
    }

    return (
        <>
            <SuccessToast message={message} onClose={closeMessage} />

            <div className="mx-auto" style={{ maxWidth: 700 }}>
                <Link to="/quizzes" className="btn btn-outline-secondary d-inline-flex align-items-center gap-2">
                    <i className="bi bi-arrow-left btn-icon icon-back" aria-hidden="true"></i>
                    Back to quizzes
                </Link>

                <h1 className="landing-title mt-3 mb-4">Edit quiz</h1>

                <ErrorAlert message={actionError} onClose={() => setActionError(null)} />

                {/* Quiz details. Saved on their own, separate from the questions below. */}
                <section className="card shadow-sm mb-5" aria-labelledby="details-heading">
                    <div className="card-body p-4">
                        <h2 id="details-heading" className="h4 fw-bold mb-3">Quiz details</h2>

                        <QuizDetailsForm key={formKey} initial={initialValues} modules={modules} mode="edit" onSubmit={saveDetails} />

                        <hr className="my-4" />

                        <TagEditor
                            quizId={quiz.id}
                            tags={quiz.tags}
                            catalog={catalog}
                            canShare={hasRole('Teacher') || hasRole('Admin')}
                            onChanged={afterTagChange}
                        />
                    </div>
                </section>

                {/*
                    Visibility, only for teachers and administrators, and only on quizzes made by a teacher
                    or administrator (awardsPoints). A student's own quiz is always private. The API checks both.
                */}
                {(hasRole('Teacher') || hasRole('Admin')) && quiz.awardsPoints && (
                    <section className="card shadow-sm mb-5" aria-labelledby="visibility-heading">
                        <div className="card-body p-4 d-flex flex-wrap justify-content-between align-items-center gap-3">
                            <div>
                                <h2 id="visibility-heading" className="h4 fw-bold mb-1">Visibility</h2>
                                {quiz.isPublic ? (
                                    <>
                                        <span className="badge bg-success">PUBLIC</span>
                                        <span className="text-secondary small ms-1">Students can see and take this quiz.</span>
                                    </>
                                ) : (
                                    <>
                                        <span className="badge bg-secondary">DRAFT</span>
                                        <span className="text-secondary small ms-1">Only you can see this quiz.</span>
                                    </>
                                )}
                            </div>

                            {quiz.isPublic ? (
                                <button
                                    type="button"
                                    className="btn btn-outline-secondary"
                                    disabled={changingVisibility}
                                    onClick={() => void changeVisibility(false)}
                                >
                                    Unpublish
                                </button>
                            ) : (
                                /* An empty quiz cannot be taken, so it cannot be published yet */
                                <button
                                    type="button"
                                    className="btn btn-success"
                                    disabled={changingVisibility || quiz.questions.length === 0}
                                    title={quiz.questions.length === 0 ? 'Add at least one question first' : undefined}
                                    onClick={() => void changeVisibility(true)}
                                >
                                    Publish
                                </button>
                            )}
                        </div>
                    </section>
                )}

                <section className="mt-5" aria-labelledby="questions-heading">
                    <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
                        <h2 id="questions-heading" className="h4 fw-bold mb-0">Questions ({quiz.questions.length})</h2>
                        <Link className="btn btn-primary d-inline-flex align-items-center gap-2" to={`/quizzes/${quiz.id}/questions/new`}>
                            <i className="bi bi-plus-lg icon-plus" aria-hidden="true"></i>
                            Add question
                        </Link>
                    </div>

                    {quiz.questions.length === 0 ? (
                        <p className="text-secondary fst-italic">No questions yet. Add the first one to make the quiz playable.</p>
                    ) : (
                        <ol className="list-unstyled d-flex flex-column gap-2">
                            {quiz.questions.map((question, index) => {
                                const number = index + 1
                                const correct = question.options.find((option) => option.isCorrect)

                                return (
                                    <li key={question.id} className="card border-0 shadow-sm">
                                        <div className="card-body">
                                            <p className="text-secondary small mb-1">Question {number}</p>
                                            <p className="fw-semibold mb-1">{question.text}</p>
                                            <p className="text-secondary small mb-0">
                                                {question.options.length} answers · Correct: {correct?.text ?? '–'}
                                            </p>

                                            <div className="d-flex flex-wrap gap-2 mt-3">
                                                <Link
                                                    className="btn btn-secondary btn-sm d-inline-flex align-items-center gap-2"
                                                    to={`/quizzes/${quiz.id}/questions/${question.id}/edit`}
                                                    aria-label={`Edit question ${number}`}
                                                >
                                                    <i className="bi bi-pencil-square btn-icon icon-edit" aria-hidden="true"></i>
                                                    Edit
                                                </Link>
                                                <button
                                                    type="button"
                                                    className="btn btn-outline-danger btn-sm d-inline-flex align-items-center gap-2"
                                                    aria-label={`Delete question ${number}`}
                                                    onClick={() => setQuestionToDelete({ question, number })}
                                                >
                                                    <i className="bi bi-trash3 btn-icon" aria-hidden="true"></i>
                                                    Delete
                                                </button>
                                            </div>
                                        </div>
                                    </li>
                                )
                            })}
                        </ol>
                    )}
                </section>
            </div>

            <ConfirmDialog
                show={questionToDelete !== null}
                title="Delete question?"
                message={
                    <>
                        <p className="mb-2">This question and its answers will be permanently deleted.</p>
                        <p className="fw-semibold mb-0">
                            {questionToDelete && `${questionToDelete.number}. ${questionToDelete.question.text}`}
                        </p>
                    </>
                }
                confirmText="Delete question"
                busy={deleting}
                onConfirm={confirmDeleteQuestion}
                onCancel={() => setQuestionToDelete(null)}
            />
        </>
    )
}