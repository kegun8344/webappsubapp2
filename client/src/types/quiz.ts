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

/* A module in the filter dropdown. Only the fields this page needs from GET /api/modules. */
export interface ModuleOption {
    id: number
    name: string
}