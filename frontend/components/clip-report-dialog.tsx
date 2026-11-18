"use client";

import { useEffect, useRef, useState } from "react";
import type { ClipReportReason } from "@/lib/contracts";

type Envelope<T> = { success?: boolean; message?: string; data?: T };

const reasons: { value: ClipReportReason; label: string }[] = [
  { value: "Quality", label: "Video kalitesi kötü" },
  { value: "CaptionSync", label: "Altyazı videoyla uyuşmuyor" },
  { value: "Inappropriate", label: "Uygunsuz içerik" },
  { value: "Other", label: "Diğer" },
];

async function post(path: string, body: unknown) {
  const response = await fetch(`/api/backend/api/${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const text = await response.text();
  let parsed: Envelope<unknown> | null = null;
  try { parsed = text ? (JSON.parse(text) as Envelope<unknown>) : null; } catch { parsed = null; }
  if (!response.ok || !parsed || parsed.success === false) {
    throw new Error(parsed?.message || `İstek başarısız oldu (HTTP ${response.status}).`);
  }
}

/**
 * "Videoyu bildir" penceresi (Listening ve Writing ortak). "Bana Gösterme" açıkken (varsayılan) bildirilen klip
 * bildirene bir daha gösterilmez; kapalıyken bildirim gider ama klip görünmeye devam eder.
 * Kapanınca onClose(hidden): hidden = klip bildirildi VE gizlenecek.
 */
export default function ClipReportDialog({ clipId, onClose }: { clipId: string; onClose: (hidden: boolean) => void }) {
  const [reason, setReason] = useState<ClipReportReason>("CaptionSync");
  const [note, setNote] = useState("");
  const [hideFromMe, setHideFromMe] = useState(true);
  const [reporting, setReporting] = useState(false);
  const [done, setDone] = useState<null | { hidden: boolean }>(null);
  const [error, setError] = useState("");
  const modalRef = useRef<HTMLDivElement>(null);

  const close = () => { if (!reporting) onClose(done?.hidden === true); };
  const closeRef = useRef(close);
  closeRef.current = close;

  useEffect(() => {
    modalRef.current?.focus();
    const onKey = (event: KeyboardEvent) => { if (event.key === "Escape") closeRef.current(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  async function submit() {
    setReporting(true); setError("");
    try {
      await post(`Clips/${encodeURIComponent(clipId)}/report`, { reason, note: note.trim() || null, hideFromMe });
      setDone({ hidden: hideFromMe });
    } catch (value) {
      setError(value instanceof Error ? value.message : "Bildirim gönderilemedi.");
    } finally { setReporting(false); }
  }

  return <div className="cinema-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) close(); }}>
    <div className="cinema-modal" role="dialog" aria-modal="true" aria-labelledby="report-title" tabIndex={-1} ref={modalRef}>
      {done ? <div className="cinema-success">
        <span className="cinema-success-icon" aria-hidden="true">✓</span>
        <h2 id="report-title">Video bildirildi</h2>
        <p>{done.hidden ? "Teşekkürler, ekibimiz inceleyecek. Bu video artık sana gösterilmeyecek." : "Teşekkürler, ekibimiz inceleyecek. Video görünmeye devam edecek."}</p>
        <button type="button" className="cinema-button primary" onClick={close}>Kapat</button>
      </div> : <form onSubmit={(event) => { event.preventDefault(); void submit(); }} style={{ display: "grid", gap: 16 }}>
        <div><h2 id="report-title">Videoyu bildir</h2><p>Video durduruldu. Bu videoyla ilgili sorun nedir?</p></div>
        <fieldset className="cinema-reasons">
          {reasons.map((item) => <label key={item.value} className="cinema-reason"><input type="radio" name="clip-report-reason" value={item.value} checked={reason === item.value} onChange={() => setReason(item.value)}/>{item.label}</label>)}
        </fieldset>
        <textarea placeholder="İstersen kısaca açıkla (isteğe bağlı)" maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} aria-label="Açıklama (isteğe bağlı)"/>
        <label className="cinema-switch">
          <input type="checkbox" role="switch" checked={hideFromMe} onChange={(event) => setHideFromMe(event.target.checked)}/>
          <span>Bana Gösterme<small>Açıkken bu video bir daha karşına çıkmaz</small></span>
        </label>
        {error && <p className="cinema-error" role="alert">{error}</p>}
        <div className="cinema-modal-actions">
          <button type="button" className="cinema-button" onClick={close} disabled={reporting}>Vazgeç</button>
          <button type="submit" className="cinema-button primary" disabled={reporting}>{reporting ? "Gönderiliyor…" : "Bildir"}</button>
        </div>
      </form>}
    </div>
  </div>;
}
