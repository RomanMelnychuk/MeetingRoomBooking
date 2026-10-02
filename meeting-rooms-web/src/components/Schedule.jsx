import { useCallback, useEffect, useState } from 'react'
import { api, slotLabel, todayIso } from '../api.js'
import { useToast } from '../toast.jsx'

export default function Schedule({ room, connection, reconnects, onChanged }) {
    const notify = useToast()
    const [date, setDate] = useState(todayIso())
    const [slots, setSlots] = useState([])
    const [loading, setLoading] = useState(false)
    const [bookingHour, setBookingHour] = useState(null)

    const roomId = room?.id ?? null

    const loadSchedule = useCallback(async () => {
        if (!roomId) {
            setSlots([])
            return
        }
        setLoading(true)
        try {
            setSlots(await api(`/api/rooms/${roomId}/schedule?date=${date}`))
        } catch (error) {
            notify(error.message, 'error')
        } finally {
            setLoading(false)
        }
    }, [roomId, date, notify])

    useEffect(() => {
        loadSchedule()
    }, [loadSchedule])

    // Live updates: join the SignalR group of the room on screen. The event carries
    // no schedule data — on "SlotBooked" we re-read the schedule through REST.
    useEffect(() => {
        if (!connection || !roomId) return

        const onSlotBooked = (event) => {
            if (event.roomId === roomId && event.date === date) loadSchedule()
            onChanged()
        }

        connection.on('SlotBooked', onSlotBooked)
        connection.invoke('JoinRoom', roomId).catch(() => { })
        if (reconnects > 0) loadSchedule() // catch up on anything missed while offline

        return () => {
            connection.off('SlotBooked', onSlotBooked)
            connection.invoke('LeaveRoom', roomId).catch(() => { })
        }
    }, [connection, roomId, date, reconnects, loadSchedule, onChanged])

    async function book(startHour) {
        setBookingHour(startHour)
        try {
            await api('/api/bookings', { method: 'POST', body: { roomId, date, startHour } })
            notify(`Booked ${room.name}, ${slotLabel(startHour)}.`, 'success')
        } catch (error) {
            notify(error.message, 'error') // a lost race shows the server's 409 message
        } finally {
            setBookingHour(null)
            loadSchedule()
            onChanged()
        }
    }

    return (
        <section className="card">
            <div className="schedule-head">
                <h2>{room ? `Schedule · ${room.name}` : 'Schedule'}</h2>
                <input type="date" value={date} min={todayIso()} onChange={e => setDate(e.target.value || todayIso())} />
            </div>

            <p className="hint status">
                {!room ? 'Select a room to see its schedule.' : loading ? 'Loading…' : '\u00a0'}
            </p>

            <div className="slots">
                {slots.map(slot => {
                    const time = slotLabel(slot.startHour)
                    if (slot.isMine) {
                        return <button key={slot.startHour} className="slot slot-mine" disabled>{time} · Your booking</button>
                    }
                    if (slot.isBooked) {
                        return <button key={slot.startHour} className="slot slot-booked" disabled>{time} · Booked</button>
                    }
                    return (
                        <button
                            key={slot.startHour}
                            className="slot slot-free"
                            disabled={bookingHour !== null}
                            onClick={() => book(slot.startHour)}
                        >
                            {time} · {bookingHour === slot.startHour ? 'Booking…' : 'Free'}
                        </button>
                    )
                })}
            </div>

            <p className="legend">
                <span className="dot free"></span>Free
                <span className="dot booked"></span>Booked
                <span className="dot mine"></span>Yours
            </p>
        </section>
    )
}