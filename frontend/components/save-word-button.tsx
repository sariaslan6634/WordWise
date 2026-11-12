"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

/** Instagram "kaydet" benzeri yer imi butonu: kelimeyi kelime defterine ekler / çıkarır. */
export default function SaveWordButton({ wordId, initialSaved, compact = false, iconOnly = false }: { wordId: string; initialSaved: boolean; compact?: boolean; iconOnly?: boolean }) {
  const router = useRouter();
  const [saved, setSaved] = useState(initialSaved);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function toggle() {
    if (busy) return;
    const next = !saved;
    setBusy(true);
    setError("");
    setSaved(next); // anında tepki ver; hata olursa geri al
    try {
      const response = await fetch(`/api/backend/api/UserWord/${encodeURIComponent(wordId)}`, { method: next ? "POST" : "DELETE" });
      // 409: zaten kayıtlı, 404 (silerken): zaten kayıtlı değil. İkisi de istenen duruma denk gelir.
      if (!response.ok && !(next && response.status === 409) && !(!next && response.status === 404)) throw new Error("failed");
      router.refresh();
    } catch {
      setSaved(!next);
      setError("İşlem yapılamadı, tekrar dene.");
    } finally {
      setBusy(false);
    }
  }

  const label = saved ? "Kelime defterinden çıkar" : "Kelime defterine kaydet";
  return (
    <div className={`save-word${compact ? " is-compact" : ""}${iconOnly ? " is-icon" : ""}`}>
      <button type="button" className={`save-word-button${saved ? " is-saved" : ""}`} onClick={toggle} disabled={busy} aria-pressed={saved} aria-label={label} title={label}>
        <svg viewBox="0 0 24 24" width="22" height="22" fill={saved ? "currentColor" : "none"} stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <path d="M6 3h12a1 1 0 0 1 1 1v17l-7-4.5L5 21V4a1 1 0 0 1 1-1z" />
        </svg>
        {!iconOnly && <span>{saved ? "Kaydedildi" : "Kaydet"}</span>}
      </button>
      {error && <small role="alert">{error}</small>}
    </div>
  );
}
