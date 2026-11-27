/* What the module endpoints send and receive (ModuleDto, ModuleSummaryDto, ModuleOptionsDto and ModuleFormDto in the API) */

/* One module card */
export interface Module {
    id: number
    name: string
    description: string | null
    /* A Bootstrap Icons class, e.g. "bi-database" */
    iconKey: string
    /* 0-7, used as module-color-{colorIndex} */
    colorIndex: number
    /* Quizzes the user may see in this module */
    quizCount: number
    /* Quizzes from teachers in this module, and how many the user has taken */
    officialQuizCount: number
    completedOfficialQuizCount: number
    progressPercent: number
    isCompleted: boolean
    /* null if the user has not taken any quizzes in the module */
    averageBestScore: number | null
    /* 0-3 */
    masteryStars: number
    nextQuizId: number | null
    nextQuizTitle: string | null
}

/* The "learning journey" at the top of the module page */
export interface ModuleSummary {
    displayName: string
    totalOfficialQuizzes: number
    totalCompletedQuizzes: number
    progressPercent: number
    modulesWithQuizzes: number
    modulesCompleted: number
    averageScore: number | null
    message: string
    continueQuizId: number | null
    continueQuizTitle: string | null
    continueModuleName: string | null
}

/* The icons and colours a module can have */
export interface ModuleOptions {
    icons: { key: string; label: string }[]
    colors: { index: number; name: string }[]
}

/* The body when creating or editing a module */
export interface ModuleFormRequest {
    name: string
    description: string | null
    iconKey: string
    /* null means the colour comes from the name */
    colorIndex: number | null
}