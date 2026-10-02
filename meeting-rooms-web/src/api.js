export class ApiError extends Error {
    constructor(message, status) {
        super(message)
        this.status = status
    }
}

// fetch wrapper: sends JSON, returns parsed JSON (or null) and turns error
// responses into an ApiError with a readable message taken from ProblemDetails.
export async function api(url, { method = 'GET', body } = {}) {
    const response = await fetch(url, {
        method,
        headers: body ? { 'Content-Type': 'application/json' } : undefined,
        body: body ? JSON.stringify(body) : undefined
    })

    if (!response.ok) {
        if (response.status === 401) window.dispatchEvent(new Event('session-expired'))
        throw new ApiError(await readError(response), response.status)
    }

    const text = await response.text()
    return text ? JSON.parse(text) : null
}

async function readError(response) {
    try {
        const problem = await response.json()
        if (problem.errors) return Object.values(problem.errors).flat().join(' ')
        if (problem.title) return problem.title
    } catch {
        // empty or non-JSON body
    }
    return `Request failed (${response.status}).`
}

export function slotLabel(startHour) {
    const pad = (h) => String(h).padStart(2, '0')
    return `${pad(startHour)}:00–${pad(startHour + 1)}:00`
}

export function todayIso() {
    const now = new Date()
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset())
    return now.toISOString().slice(0, 10)
}