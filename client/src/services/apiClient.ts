/* Base address of the API, set in .env.development */
const API_URL = import.meta.env.VITE_API_URL as string

/* An error from the API. The status lets pages react differently, e.g. to 404 or 401. 0 means the API could not be reached. */
export class ApiError extends Error {
    status: number

    /*
     * Errors per form field from the API's validation, e.g. { email: ['Please enter a valid email address.'] }.
     * The key "" holds errors that belong to no field, e.g. "Email is already taken".
     */
    fieldErrors: Record<string, string[]>

    constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
        super(message)
        this.status = status
        this.fieldErrors = fieldErrors
    }
}

/* The error format ASP.NET sends (ProblemDetails) */
interface ProblemDetails {
    detail?: string
    errors?: Record<string, string[]>
}

/* The API names fields like the C# properties ("Email"), the React forms use camelCase ("email") */
function toCamelCase(key: string): string {
    return key.charAt(0).toLowerCase() + key.slice(1)
}

/* Turns an error answer from the API into an ApiError with a readable message */
async function readError(response: Response): Promise<ApiError> {
    let problem: ProblemDetails = {}

    try {
        problem = (await response.json()) as ProblemDetails
    } catch {
        /* No JSON body, e.g. a plain 401 */
    }

    const fieldErrors: Record<string, string[]> = {}
    for (const [key, messages] of Object.entries(problem.errors ?? {})) {
        fieldErrors[toCamelCase(key)] = messages
    }

    const message = problem.detail
        ?? (problem.errors ? 'Please check the form.' : `The request failed (${response.status}).`)

    return new ApiError(response.status, message, fieldErrors)
}

/*
 * Sends a request to the API and returns the JSON answer.
 * All HTTP calls go through here, so the pages never call fetch themselves.
 */
export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
    let response: Response

    try {
        response = await fetch(`${API_URL}${path}`, {
            ...options,
            /* Sends the HttpOnly login cookie with every request */
            credentials: 'include',
            headers: { 'Content-Type': 'application/json', ...options.headers },
        })
    } catch {
        throw new ApiError(0, 'Could not reach the server. Please try again.')
    }

    if (!response.ok) {
        throw await readError(response)
    }

    /* 204 No Content has no body to read */
    if (response.status === 204) {
        return undefined as T
    }

    return (await response.json()) as T
}