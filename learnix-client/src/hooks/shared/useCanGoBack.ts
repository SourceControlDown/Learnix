import { useLocation } from 'react-router-dom';

/**
 * Whether `navigate(-1)` would land on another page of this app rather than leave it.
 *
 * React Router stamps every history entry it creates with an incrementing `idx`, starting
 * at 0 for the entry the app was opened on. So anything above 0 means we pushed at least
 * one navigation ourselves and can safely step back.
 */
export function useCanGoBack(): boolean {
    // Re-evaluated on every navigation — `history.state` is not reactive on its own.
    useLocation();

    const idx = (window.history.state as { idx?: number } | null)?.idx ?? 0;
    return idx > 0;
}
