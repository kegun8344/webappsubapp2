import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import QuizDetailsForm from '../components/QuizDetailsForm'
import { createQuiz, getModuleOptions } from '../services/quizService'
import type { ModuleOption } from '../types/quiz'
import { toQuizRequest, type QuizFormValues } from '../utils/quizValidation'

const emptyQuiz: QuizFormValues = { title: '', description: '', moduleId: '', difficulty: 'Easy' }

/* Create quiz, same page as Quiz/Create in Subapp 1. Afterwards the user goes straight on to adding questions. */
export default function CreateQuizPage() {
    const navigate = useNavigate()
    const [modules, setModules] = useState<ModuleOption[]>([])
    const [modulesError, setModulesError] = useState<string | null>(null)

    useEffect(() => {
        let active = true

        getModuleOptions()
            .then((result) => {
                if (active) setModules(result)
            })
            .catch(() => {
                if (active) setModulesError('The modules could not be loaded. Please refresh the page.')
            })

        return () => {
            active = false
        }
    }, [])

    async function handleSubmit(values: QuizFormValues) {
        /* Errors are shown by the form */
        const quiz = await createQuiz(toQuizRequest(values))
        navigate(`/quizzes/${quiz.id}/edit`, { state: { message: 'Quiz created. Now add some questions.' } })
    }

    return (
        <div className="mx-auto" style={{ maxWidth: 700 }}>
            <h1 className="landing-title mb-2">Create quiz</h1>
            <p className="lead text-secondary mb-4">Give the quiz a title, a module and a difficulty. You add the questions next.</p>

            {modulesError && <div className="alert alert-warning" role="alert">{modulesError}</div>}

            <QuizDetailsForm initial={emptyQuiz} modules={modules} mode="create" onSubmit={handleSubmit} />
        </div>
    )
}