import { createContext, useCallback, useContext, useRef, useState } from 'react'

const ToastContext = createContext(() => { })

// Every user action ends with a visible message: success, error or info.
export function ToastProvider({ children }) {
    const [toasts, setToasts] = useState([])
    const nextId = useRef(0)

    const notify = useCallback((message, type = 'info') => {
        const id = nextId.current++
        setToasts(list => [...list, { id, message, type }])
        setTimeout(() => setToasts(list => list.filter(t => t.id !== id)), 4000)
    }, [])

    return (
        <ToastContext.Provider value={notify}>
            {children}
            <div className="toasts" aria-live="polite">
                {toasts.map(t => (
                    <div key={t.id} className={`toast toast-${t.type}`}>{t.message}</div>
                ))}
            </div>
        </ToastContext.Provider>
    )
}

export const useToast = () => useContext(ToastContext)