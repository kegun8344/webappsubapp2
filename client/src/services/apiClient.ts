/* Base address of the API, set in .env.development */
const API_URL = import.meta.env.VITE_API_URL as string

/* An error from the API. The status lets pages react differently, e.g. to 404 or 401. 0 means the API could not be reached. */
export class ApiError extends Error {
    status: number

    constructor(status: number, message: string) {
        super(message)
        this.status = status
    }
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
            headers: { 'Content-Type': 'application/json', ...options.headers },
        })
    } catch {
        throw new ApiError(0, 'Could not reach the server. Please try again.')
    }

    if (!response.ok) {
        throw new ApiError(response.status, `The request failed (${response.status}).`)
    }

    /* 204 No Content has no body to read */
    if (response.status === 204) {
        return undefined as T
    }

    return (await response.json()) as T
}