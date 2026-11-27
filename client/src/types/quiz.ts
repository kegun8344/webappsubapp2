/* What the quiz endpoints send and receive (same names as the DTOs in api/DTOs) */

export type Difficulty = 'Easy' | 'Medium' | 'Hard'

export const difficultyLevels: Difficulty[] = ['Easy', 'Medium', 'Hard']

/* All/Public/My quizzes tabs on the quiz list */
export type QuizView = 'all' | 'public' | 'mine'

export interface Tag {
    id: number
    name: string
    /* A Bootstrap colour name, e.g. "danger", used as text-bg-{color} */
    color: string
    isShared: boolean
}

/* The colours a tag can have, the same as Tag.AllowedColors in the API, with the names shown to the user */
export const tagColors: { value: string; label: string }[] = [
    { value: 'primary', label: 'Blue' },
    { value: 'secondary', label: 'Gray' },
    { value: 'success', label: 'Green' },
    { value: 'danger', label: 'Red' },
    { value: 'warning', label: 'Yellow' },
    { value: 'info', label: 'Light blue' },
    { value: 'dark', label: 'Black' },
]

/* One card on the quiz list (QuizListItemDto) */
export interface QuizListItem {
    id: number
    title: string
    description: string | null
    difficulty: Difficulty
    moduleId: number
    moduleName: string
    isPublic: boolean
    awardsPoints: boolean
    questionCount: number
    isMine: boolean
    /* The part of the creator's email before @, null if the account was deleted */
    createdByName: string | null
    /* Decided by the API. Only used to show or hide buttons: the API checks again. */
    canManage: boolean
    /* 0-100, null if the user has not taken the quiz */
    bestScore: number | null
    tags: Tag[]
}

/* The filters on the quiz list. Missing values mean "no filter". */
export interface QuizFilters {
    view: QuizView
    moduleId?: number
    search?: string
    difficulty?: Difficulty
    tagId?: number
}

/* A module in the dropdowns. Only the fields these pages need from GET /api/modules. */
export interface ModuleOption {
    id: number
    name: string
}

export interface AnswerOption {
    id: number
    text: string
    /* Only sent to people who may edit the quiz, otherwise null */
    isCorrect: boolean | null
}

export interface Question {
    id: number
    text: string
    /* Optional scenario or explanation shown above the question */
    context: string | null
    options: AnswerOption[]
}

/* One quiz with its questions, for the edit page (QuizDetailDto) */
export interface QuizDetail {
    id: number
    title: string
    description: string | null
    difficulty: Difficulty
    moduleId: number
    isPublic: boolean
    awardsPoints: boolean
    canManage: boolean
    tags: Tag[]
    questions: Question[]
}

/* The body when creating or updating a quiz (QuizFormDto) */
export interface QuizFormRequest {
    title: string
    description: string | null
    difficulty: Difficulty
    moduleId: number
}

/* The body when adding or changing a question (QuestionFormDto) */
export interface QuestionFormRequest {
    text: string
    answers: string[]
    context: string | null
    /* Position (starting at 0) of the correct answer */
    correctAnswerIndex: number
}

/* The body when creating a tag on a quiz (CreateTagDto) */
/* One of the user's own tags and how many quizzes it is on (TagUsage in the API) */
export interface MyTag {
    id: number
    name: string
    color: string
    quizCount: number
}
export interface CreateTagRequest {
    name: string
    color: string
    /* Only used for teachers and administrators. The server ignores it for students. */
    shared: boolean
}