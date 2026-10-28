"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import type { VideoCandidateReview } from "@/lib/contracts";
import YouTubeSegmentPlayer from "./youtube-segment-player";

type ApiEnvelope<T> = { success?: boolean; message?: string; data?: T };

async function readResponse<T>(response: Response) {
  const body = await response.text();
  if (!body) throw new Error(`Sunucu boş yanıt döndürdü (HTTP ${response.status}).`);
  try { return JSON.parse(body) as ApiEnvelope<T>; }
  catch { throw new Error(`Sunucu yanıtı okunamadı (HTTP ${response.status}).`); }
}

export default function VideoCandidatePreview({ review }: { review: VideoCandidateReview }) {
  const router = useRouter();
  const [startValue, setStartValue] = useState("0");
  const [endValue, setEndValue] = useState("15");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const startSec = Number.parseInt(startValue, 10);
  const endSec = Number.parseInt(endValue, 10);
  const validRange = Number.isInteger(startSec) && Number.isInteger(endSec) && startSec >= 0 && endSec > startSec;
  const previewStart = Number.isInteger(startSec) && startSec >= 0 ? startSec : 0;
  const previewEnd = Number.isInteger(endSec) && endSec > previewStart ? endSec : previewStart + 1;
  const embedUrl = `https://www.youtube.com/embed/${encodeURIComponent(review.candidate.youtubeId)}?start=${previewStart}&end=${previewEnd}&playsinline=1&rel=0`;

  async function approve() {
    if (!validRange) { setError("Bitiş saniyesi başlangıç saniyesinden büyük olmalı."); return; }
    setBusy(true); setError("");
    try {
      const response = await fetch(`/api/backend/api/AdminVideo/candidates/${encodeURIComponent(review.candidate.id)}/approve`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ startSec, endSec, transcript: null }),
      });
      const result = await readResponse<string>(response);
      if (!response.ok || result.success === false) throw new Error(result.message || `Video onaylanamadı (HTTP ${response.status}).`);
      router.push(`/admin/words?status=active&searchText=${encodeURIComponent(review.word.text)}`);
    } catch (value) { setError(value instanceof Error ? value.message : "Onaylama işlemi başarısız oldu."); }
    finally { setBusy(false); }
  }

  const word = review.word;
  const candidate = review.candidate;
  return <div className="admin-content video-approval-content">
    <div className="admin-page-heading"><div><div className="eyebrow"><span className="eyebrow-dot"/> YAYIN ÖNİZLEMESİ</div><h1>{word.text} <span>✳</span></h1><p>Videoda kelimenin geçtiği aralığı belirle, son görünümü kontrol et ve onayla.</p></div></div>
    <section className="video-approval-grid">
      <article className="panel clip-editor">
        <div className="panel-heading"><div><span className="panel-kicker">YOUTUBE VİDEOSU</span><h2>{candidate.title}</h2><p>{candidate.channelTitle || "YouTube"}</p></div></div>
        <YouTubeSegmentPlayer videoId={candidate.youtubeId} startSec={previewStart} endSec={previewEnd} onUseStart={(seconds) => setStartValue(String(seconds))} onUseEnd={(seconds) => setEndValue(String(seconds))}/>
        <a className="external-video-link" href={`https://www.youtube.com/watch?v=${encodeURIComponent(candidate.youtubeId)}`} target="_blank" rel="noreferrer">Videonun tamamını YouTube&apos;da aç ↗</a>
        <div className="clip-range-fields"><label>Başlangıç saniyesi<input type="number" min="0" step="1" value={startValue} onChange={(event) => setStartValue(event.target.value)}/></label><label>Bitiş saniyesi<input type="number" min="1" step="1" value={endValue} onChange={(event) => setEndValue(event.target.value)}/></label></div>
        <p className="clip-range-note">Önizleme {previewStart}. saniyeden başlayıp {previewEnd}. saniyede durur. Bu aralık veritabanında saniye olarak saklanır.</p>
        {!validRange && <p className="preview-error" role="alert">Bitiş saniyesi başlangıçtan büyük olmalı.</p>}
      </article>

      <article className="panel learning-preview-card">
        <div className="panel-heading"><div><span className="panel-kicker">ÖĞRENCİNİN GÖRECEĞİ</span><h2>Kelime kartı önizlemesi</h2></div><span className="data-pill preview">Taslak</span></div>
        <div className="learning-word-heading"><div><h3>{word.text}</h3><span>{word.partOfSpeech || "Kelime"}{word.cefrLevel ? ` · ${word.cefrLevel}` : ""}</span></div><div className="learning-pronunciation">{word.ipa || "Telaffuz bilgisi yok"}<small>Yazılış: {word.text}</small></div></div>
        <p className="learning-definition">{word.definition}</p>
        <div className="clip-card-player"><iframe key={`card-${candidate.youtubeId}-${previewStart}-${previewEnd}`} src={embedUrl} title={`${word.text} kelime videosu`} allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen/></div>
        <p className="learning-clip-caption">{candidate.title} · {previewStart}s–{previewEnd}s</p>
        <section className="learning-preview-section"><h4>Örnek cümleler</h4>{word.exampleSentences.length ? <ul>{word.exampleSentences.slice(0, 4).map((sentence, index) => <li key={`${sentence}-${index}`}>{sentence}</li>)}</ul> : <p>Henüz örnek cümle eklenmemiş.</p>}</section>
        <div className="learning-related-grid"><section className="learning-preview-section"><h4>Benzer kelimeler</h4><p>{word.synonyms.length ? word.synonyms.join(", ") : "Benzer kelime bulunmuyor."}</p></section><section className="learning-preview-section"><h4>Zıt kelimeler</h4><p>{word.antonyms.length ? word.antonyms.join(", ") : "Zıt kelime bulunmuyor."}</p></section></div>
      </article>
    </section>
    <div className="video-approval-footer"><a href={`/admin/videos?wordId=${encodeURIComponent(candidate.wordId)}&word=${encodeURIComponent(word.text)}`}>← Aday videolara dön</a><div>{error && <p className="preview-error" role="alert">{error}</p>}<button className="candidate-approve" disabled={busy || !validRange} onClick={approve}>{busy ? "Onaylanıyor…" : "Bu videoyu ve kelimeyi onayla →"}</button></div></div>
  </div>;
}
