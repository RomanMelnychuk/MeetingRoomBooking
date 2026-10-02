import { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'

// Keeps one SignalR connection open while the user is logged in.
// The hub only sends signals ("something changed"); data is always re-read through REST.
export function useScheduleHub(enabled) {
    const [connection, setConnection] = useState(null)
    const [live, setLive] = useState(false)
    const [reconnects, setReconnects] = useState(0) // bumps after a reconnect so groups are re-joined

    useEffect(() => {
        if (!enabled) return

        const hub = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/schedule')
            .withAutomaticReconnect()
            .build()

        hub.onreconnecting(() => setLive(false))
        hub.onreconnected(() => {
            setLive(true)
            setReconnects(n => n + 1)
        })
        hub.onclose(() => setLive(false))

        let cancelled = false
        hub.start()
            .then(() => {
                if (cancelled) return
                setConnection(hub)
                setLive(true)
            })
            .catch(() => setLive(false))

        return () => {
            cancelled = true
            setConnection(null)
            setLive(false)
            hub.stop()
        }
    }, [enabled])

    return { connection, live, reconnects }
}