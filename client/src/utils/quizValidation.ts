import { difficultyLevels, type Difficulty, type QuizFormRequest } from '../types/quiz'

/*
 * The same rules as QuizFormDto and QuestionRules in the API.
 * Checked here first for quick feedback, and always again on the server.
 */

export const MIN_ANSWERS = 2
export const MAX_ANSWERS = 10

/* The values in the quiz form, as text from the inputs */
export interface QuizFormValues {
    title: string
    description: string
    moduleId: string
    difficulty: string
}

export type QuizFormErrors = Partial<Record<keyof QuizFormValues, string>>

export function validateQuiz(values: QuizFormValues): QuizFormErrors {
    const errors: QuizFormErrors = {}
    const title = values.title.trim()

    if (!title) errors.title = 'Please write a title.'
    else if (title.length > 100) errors.title = 'The title cannot exceed 100 characters.'

    if (values.description.trim().length > 500) errors.description = 'The description cannot exceed 500 characters.'

    if (!values.moduleId) errors.moduleId = 'Choose a module.'

    if (!difficultyLevels.some((level) => level === values.difficulty)) errors.difficulty = 'Difficulty must be Easy, Medium, or Hard.'

    return errors
}

export interface QuestionFormErrors {
    text?: string
    /* One message per answer field, in the same order */
    answerFields: (string | undefined)[]
    /* Problems with the answers as a whole, e.g. two answers with the same text */
    answers?: string
    correctAnswerIndex?: string
}

export function validateQuestion(text: string, answers: string[], correctAnswerIndex: number | null): QuestionFormErrors {
    const trimmed = answers.map((answer) => answer.trim())

    const errors: QuestionFormErrors = {
        answerFields: trimmed.map((answer) => {
            if (!answer) return 'Write an answer or remove this field.'
            if (answer.length > 300) return 'An answer cannot be longer than 300 characters.'
            return undefined
        }),
    }

    const question = text.trim()
    if (!question) errors.text = 'Please write a question.'
    else if (question.length > 500) errors.text = 'The question cannot exceed 500 characters.'

    if (answers.length < MIN_ANSWERS || answers.length > MAX_ANSWERS) {
        errors.answers = `A question must have between ${MIN_ANSWERS} and ${MAX_ANSWERS} answers.`
    } else {
        /* "Controller" and "controller " count as the same answer */
        const filled = trimmed.filter(Boolean).map((answer) => answer.toLowerCase())
        if (new Set(filled).size !== filled.length) errors.answers = 'Two answers cannot have the same text.'
    }

    if (correctAnswerIndex === null || correctAnswerIndex < 0 || correctAnswerIndex >= answers.length) {
        errors.correctAnswerIndex = 'Choose which answer is correct.'
    }

    return errors
}

export function hasQuestionErrors(errors: QuestionFormErrors): boolean {
    return Boolean(errors.text || errors.answers || errors.correctAnswerIndex || errors.answerFields.some(Boolean))
}

/* Turns the form values into the body the API expects */
export function toQuizRequest(values: QuizFormValues): QuizFormRequest {
    return {
        title: values.title.trim(),
        description: values.description.trim() || null,
        difficulty: values.difficulty as Difficulty,
        moduleId: Number(values.moduleId),
    }
}