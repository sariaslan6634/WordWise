"use client";

import { useState } from "react";
import SyncedCaptionPlayer from "@/components/synced-caption-player";
import type { VideoClip } from "@/lib/contracts";

type Envelope<T> = { success?: boolean; message?: string; data?: T };
type Filter = "pending" | "published";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/backend/api/AdminVideo/${path}`, init);
  const body = await response.text();
  let parsed: Envelope<T> | null = null;
  try { parsed = body ? (JSON.parse(body) as Envelope<T>) : null; } catch { parsed = null; }
  if (!response.ok || !parsed || parsed.success === false) {
    throw new Error(parsed?.message || `İstek başarısız oldu (HTTP ${response.status}).`);
  }
  return parsed.data as T;
}

const STEP = 0.5;
const round1 = (value: number) => Math.round(value * 10) / 10;

function formatTime(seconds: number) {
  const whole = Math.floor(seconds);
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, "0")}`;
}

export default function ClipManager({ initialClips }: { initialClips: VideoClip[] }) {
  const [clips, setClips] = useState(initialClips);
  const [url, setUrl] = useState("");
  const [filter, setFilter] = useState<Filter>("pending");
  const [generating, setGenerating] = useState(false);
  const [actingId, setActingId] = useState<string | null>(null);
  const [openId, setOpenId] = useState<string | null>(null);
  const [draft, setDraft] = useState<Record<string, { start: number; end: number }>>({});
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  async function generate(event: React.FormEvent) {
    event.preventDefault();
    if (!url.trim()) { setError("Bir YouTube bağlantısı yapıştır."); return; }
    setGenerating(true); setError(""); setNotice("");
    try {
      const created = await request<VideoClip[]>("clips/from-video", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ url: url.trim() }),
      });
      setClips(await request<VideoClip[]>("clips"));
      const passed = created.filter((clip) => clip.qcStatus === "Passed").length;
      setNotice(created.length === 0
        ? "Bu videoda uygun tek cümlelik klip bulunamadı (ya da hepsi zaten yayında)."
        : `${created.length} klip üretildi, ${passed} tanesi kalite kontrolünden geçti.`);
      setUrl(""); setFilter("pending");
    } catch (value) { setError(value instanceof Error ? value.message : "Klipler üretilemedi."); }
    finally { setGenerating(false); }
  }

  async function toggleBurnedIn(clip: VideoClip, value: boolean) {
    setActingId(clip.id); setError(""); setNotice("");
    try {
      await request<number>(`clips/by-video/${encodeURIComponent(clip.youtubeId)}/burned-in`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ value }),
      });
      setClips((current) => current.map((item) => (item.youtubeId === clip.youtubeId ? { ...item, hasBurnedInCaptions: value } : item)));
    } catch (value) { setError(value instanceof Error ? value.message : "İşlem başarısız oldu."); }
    finally { setActingId(null); }
  }

  async function review(clip: VideoClip, action: "publish" | "reject") {
    setActingId(clip.id); setError(""); setNotice("");
    try {
      const updated = await request<VideoClip>(`clips/${encodeURIComponent(clip.id)}/${action}`, { method: "POST" });
      setClips((current) => action === "reject"
        ? current.filter((item) => item.id !== clip.id)
        : current.map((item) => (item.id === clip.id ? updated : item)));
      if (openId === clip.id && action === "reject") setOpenId(null);
    } catch (value) { setError(value instanceof Error ? value.message : "İşlem başarısız oldu."); }
    finally { setActingId(null); }
  }

  const timingOf = (clip: VideoClip) => draft[clip.id] ?? { start: clip.startSec, end: clip.endSec };
  function nudge(clip: VideoClip, field: "start" | "end", delta: number) {
    const current = timingOf(clip);
    const next = { ...current, [field]: Math.max(0, round1(current[field] + delta)) };
    setDraft((all) => ({ ...all, [clip.id]: next }));
  }
  function setTime(clip: VideoClip, field: "start" | "end", raw: string) {
    const value = Number(raw);
    if (!Number.isFinite(value) || value < 0) return;
    setDraft((all) => ({ ...all, [clip.id]: { ...timingOf(clip), [field]: value } }));
  }
  async function saveTiming(clip: VideoClip) {
    const { start, end } = timingOf(clip);
    setActingId(clip.id); setError(""); setNotice("");
    try {
      const updated = await request<VideoClip>(`clips/${encodeURIComponent(clip.id)}/timing`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ startSec: start, endSec: end }),
      });
      setClips((current) => current.map((item) => (item.id === clip.id ? updated : item)));
      setDraft((all) => { const { [clip.id]: _removed, ...rest } = all; void _removed; return rest; });
      setNotice("Klibin süresi kaydedildi.");
    } catch (value) { setError(value instanceof Error ? value.message : "Süre kaydedilemedi."); }
    finally { setActingId(null); }
  }

  const visible = clips.filter((clip) => (filter === "published" ? clip.isPublished : !clip.isPublished));
  const pendingCount = clips.filter((clip) => !clip.isPublished).length;
  const publishedCount = clips.length - pendingCount;

  return <section className="clip-manager">
    <form className="panel clip-generate" onSubmit={generate}>
      <label htmlFor="clip-url"><span className="panel-kicker">YOUTUBE BAĞLANTISI</span></label>
      <div className="clip-generate-row">
        <input id="clip-url" type="text" inputMode="url" placeholder="https://www.youtube.com/watch?v=..." value={url} onChange={(event) => setUrl(event.target.value)} disabled={generating}/>
        <button type="submit" className="clip-button primary" disabled={generating}>{generating ? "Klipler üretiliyor…" : "Klipleri üret"}</button>
      </div>
      <small>Ajan videonun altyazısını okur, farklı yerlerden 5-6 tek cümle seçer ve kalite kontrolünden geçirir. Birkaç saniye sürebilir.</small>
      {error && <p className="preview-error" role="alert">{error}</p>}
      {notice && <p className="clip-notice" role="status">{notice}</p>}
    </form>

    <div className="clip-tabs" role="tablist" aria-label="Klip durumu">
      <button type="button" role="tab" aria-selected={filter === "pending"} className={filter === "pending" ? "active" : ""} onClick={() => setFilter("pending")}>Onay bekleyen <b>{pendingCount}</b></button>
      <button type="button" role="tab" aria-selected={filter === "published"} className={filter === "published" ? "active" : ""} onClick={() => setFilter("published")}>Yayında <b>{publishedCount}</b></button>
    </div>

    {visible.length === 0 && <p className="pending-review-empty">{filter === "pending" ? "Onay bekleyen klip yok. Yukarıdan bir YouTube bağlantısı ekleyerek başla." : "Henüz yayınlanmış klip yok."}</p>}

    <div className="clip-list">{visible.map((clip) => {
      const passed = clip.qcStatus === "Passed";
      const open = openId === clip.id;
      return <article className={`clip-card ${passed ? "passed" : "failed"}`} key={clip.id}>
        <div className="clip-card-head">
          <span className={`clip-badge ${passed ? "pass" : "fail"}`}>{passed ? "Uygun" : "Uygun değil"}</span>
          {clip.isPublished && <span className="clip-badge live">Yayında</span>}
          <small>{clip.sourceTitle || clip.youtubeId} · {formatTime(clip.startSec)}–{formatTime(clip.endSec)}{clip.isAutoCaption ? " · otomatik altyazı" : ""}</small>
        </div>
        <p className="clip-text">“{clip.text}”</p>
        {clip.qcReasons.length > 0 && <ul className="clip-reasons">{clip.qcReasons.map((reason) => <li key={reason}>{reason}</li>)}</ul>}
        {clip.qcWarnings.length > 0 && <ul className="clip-warnings">{clip.qcWarnings.map((warning) => <li key={warning}>{warning}</li>)}</ul>}
        <label className="clip-burned"><input type="checkbox" checked={clip.hasBurnedInCaptions} disabled={actingId === clip.id} onChange={(event) => toggleBurnedIn(clip, event.target.checked)}/> Videoda yazılı altyazı var <small>(öğrenciye bizim altyazımız gösterilmez; bu videonun tüm klipleri için geçerli)</small></label>
        {!clip.isPublished && (() => {
          const timing = timingOf(clip);
          const changed = timing.start !== clip.startSec || timing.end !== clip.endSec;
          return <div className="clip-timing" role="group" aria-label="Klip süresini ayarla">
            {(["start", "end"] as const).map((field) => <div className="clip-timing-field" key={field}>
              <span>{field === "start" ? "Başlangıç" : "Bitiş"} (sn)</span>
              <button type="button" className="clip-button" onClick={() => nudge(clip, field, -STEP)} aria-label={`${field === "start" ? "Başlangıcı" : "Bitişi"} ${STEP} sn geri al`}>−{STEP}</button>
              <input type="number" min={0} step={0.1} value={timing[field]} onChange={(event) => setTime(clip, field, event.target.value)} aria-label={field === "start" ? "Başlangıç saniyesi" : "Bitiş saniyesi"}/>
              <button type="button" className="clip-button" onClick={() => nudge(clip, field, STEP)} aria-label={`${field === "start" ? "Başlangıcı" : "Bitişi"} ${STEP} sn ilerlet`}>+{STEP}</button>
            </div>)}
            <div className="clip-timing-actions">
              <button type="button" className="clip-button primary" disabled={!changed || actingId === clip.id || timing.end <= timing.start} onClick={() => saveTiming(clip)}>Süreyi kaydet</button>
              {changed && <button type="button" className="clip-button" onClick={() => setDraft((all) => { const { [clip.id]: _removed, ...rest } = all; void _removed; return rest; })}>Geri al</button>}
              {changed && <small>Önizleme yeni süreyle oynar; kaydetmeden yayınlanmaz.</small>}
            </div>
          </div>;
        })()}
        <div className="clip-actions">
          <button type="button" className="clip-button" onClick={() => setOpenId(open ? null : clip.id)}>{open ? "Önizlemeyi kapat" : "Önizle"}</button>
          {!clip.isPublished && <button type="button" className="clip-button primary" disabled={!passed || actingId === clip.id} title={passed ? undefined : "Kalite kontrolünden geçmeyen klip yayınlanamaz"} onClick={() => review(clip, "publish")}>Yayınla</button>}
          <button type="button" className="clip-button danger" disabled={actingId === clip.id} onClick={() => review(clip, "reject")}>{clip.isPublished ? "Yayından kaldır" : "Reddet"}</button>
        </div>
        {open && <div className="clip-preview"><SyncedCaptionPlayer videoId={clip.youtubeId} startSec={timingOf(clip).start} endSec={timingOf(clip).end} text={clip.text} words={clip.words} showCaption={!clip.hasBurnedInCaptions}/></div>}
      </article>;
    })}</div>
  </section>;
}
