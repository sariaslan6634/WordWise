"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import type { RemovedWord } from "@/lib/contracts";
import { NOTIFY_WITHIN_MS, timeLeft } from "@/lib/removal-time";

const STORAGE_KEY = "wordwise-dismissed-notifications";

function readDismissed(): string[] {
  try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || "[]") as string[]; } catch { return []; }
}
function writeDismissed(ids: string[]) {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(ids.slice(-200))); } catch { /* yoksay */ }
}
const keyOf = (item: RemovedWord) => `${item.wordId}:${item.removedAt}`;

/** Üst çubuktaki bildirim zili: silinmesine 3 saatten az kalan kelimeleri haber verir. */
export default function NotificationBell() {
  const [items, setItems] = useState<RemovedWord[]>([]);
  const [dismissed, setDismissed] = useState<string[]>([]);
  const [now, setNow] = useState(() => Date.now());
  const [open, setOpen] = useState(false);
  const box = useRef<HTMLDivElement>(null);

  const load = useCallback(async () => {
    try {
      const response = await fetch("/api/backend/api/UserWord/removed", { cache: "no-store" });
      if (!response.ok) return;
      const body = (await response.json()) as { data?: RemovedWord[] };
      setItems(body.data ?? []);
    } catch { /* ağ hatası: sonraki turda dene */ }
  }, []);

  useEffect(() => {
    setDismissed(readDismissed());
    load();
    const poll = setInterval(load, 60000);
    const tick = setInterval(() => setNow(Date.now()), 30000);
    const onFocus = () => load();
    window.addEventListener("focus", onFocus);
    return () => { clearInterval(poll); clearInterval(tick); window.removeEventListener("focus", onFocus); };
  }, [load]);

  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => { if (box.current && !box.current.contains(event.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [open]);

  const due = items.filter((item) => {
    const left = new Date(item.expiresAt).getTime() - now;
    return left > 0 && left <= NOTIFY_WITHIN_MS;
  });
  const unread = due.filter((item) => !dismissed.includes(keyOf(item)));

  function markAllRead() {
    const next = [...new Set([...dismissed, ...due.map(keyOf)])];
    setDismissed(next);
    writeDismissed(next);
  }

  return <div className="notify" ref={box}>
    <button type="button" className="notify-button" aria-label={unread.length ? `${unread.length} yeni bildirim` : "Bildirimler"} aria-expanded={open} onClick={() => setOpen((value) => !value)}>
      <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" /><path d="M13.7 21a2 2 0 0 1-3.4 0" /></svg>
      {unread.length > 0 && <span className="notify-badge">{unread.length}</span>}
    </button>
    {open && <div className="notify-panel" role="dialog" aria-label="Bildirimler">
      <div className="notify-head"><strong>Bildirimler</strong>{unread.length > 0 && <button type="button" onClick={markAllRead}>Okundu say</button>}</div>
      {due.length === 0 && <p className="notify-empty">Yeni bildirim yok.</p>}
      <ul>{due.map((item) => <li key={keyOf(item)} className={dismissed.includes(keyOf(item)) ? "is-read" : ""}>
        <Link href="/learn/words" onClick={() => setOpen(false)}><strong>{item.text}</strong> kelimesinin kalıcı silinmesine <b>{timeLeft(item.expiresAt, now)}</b> kaldı. Geri almak için tıkla.</Link>
      </li>)}</ul>
    </div>}
  </div>;
}
