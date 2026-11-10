/** "5 saat 12 dk" biçiminde kalan süre; süre dolduysa null. */
export function timeLeft(expiresAt: string, now: number): string | null {
  const ms = new Date(expiresAt).getTime() - now;
  if (!Number.isFinite(ms) || ms <= 0) return null;
  const minutes = Math.max(1, Math.ceil(ms / 60000));
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m} dk`;
  return m === 0 ? `${h} saat` : `${h} saat ${m} dk`;
}

export const NOTIFY_WITHIN_MS = 3 * 60 * 60 * 1000;
