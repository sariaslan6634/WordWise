"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import type { RemovedWord } from "@/lib/contracts";
import { timeLeft } from "@/lib/removal-time";

/** Defterden çıkarılan kelimeler: çıkarılma saati, kalan süre ve "Geri al". Süresi dolan kelime tek tek listeden düşer. */
export default function RemovedWords({ items }: { items: RemovedWord[] }) {
  const router = useRouter();
  const [now, setNow] = useState(() => Date.now());
  const [busyId, setBusyId] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 30000);
    return () => clearInterval(timer);
  }, []);

  const visible = items.filter((item) => timeLeft(item.expiresAt, now) !== null);
  if (visible.length === 0) return null;

  async function undo(wordId: string) {
    setBusyId(wordId);
    setError("");
    try {
      const response = await fetch(`/api/backend/api/UserWord/${encodeURIComponent(wordId)}`, { method: "POST" });
      if (!response.ok && response.status !== 409) throw new Error("failed");
      router.refresh();
    } catch {
      setError("Geri alınamadı, tekrar dene.");
    } finally {
      setBusyId("");
    }
  }

  const time = new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
  return <section className="removed-words" aria-label="Silinen kelimeler">
    <div className="removed-head"><h2>Silinenler</h2><p>Çıkardığın kelimeler 24 saat burada bekler; süresi dolan kelime listeden kalkar.</p></div>
    {error && <div className="learn-alert" role="alert">{error}</div>}
    <ul>{visible.map((item) => <li key={item.wordId}>
      <div className="removed-main"><Link href={`/learn/word/${encodeURIComponent(item.text)}`}>{item.text}</Link><span>{item.partOfSpeech || "Kelime"}</span></div>
      <small>Çıkarıldı: {time.format(new Date(item.removedAt))} · Silinmesine <strong>{timeLeft(item.expiresAt, now)}</strong> kaldı</small>
      <button type="button" className="button removed-undo" onClick={() => undo(item.wordId)} disabled={busyId === item.wordId}>↶ Geri al</button>
    </li>)}</ul>
  </section>;
}
