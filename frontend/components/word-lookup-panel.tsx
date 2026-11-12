"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import PronunciationButton from "@/components/pronunciation-button";
import SaveWordButton from "@/components/save-word-button";
import type { WordDetail } from "@/lib/contracts";

type State = { status: "loading" } | { status: "found"; word: WordDetail } | { status: "missing" } | { status: "error" };

/**
 * Altyazıda tıklanan kelimenin anlamını sağda açılan pencerede gösterir ve kelimeyi bir kez okur.
 * Okuma sırası: Amerikan kaydı → İngiliz kaydı → tarayıcının sesli okuması. Kapatınca (ya da kelime değişince) ses durur.
 */
export default function WordLookupPanel({ word, onClose }: { word: string; onClose: () => void }) {
  const [state, setState] = useState<State>({ status: "loading" });
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const closeRef = useRef<HTMLButtonElement>(null);

  const stopSpeaking = useCallback(() => {
    audioRef.current?.pause();
    audioRef.current = null;
    try { window.speechSynthesis?.cancel(); } catch { /* desteklenmiyor */ }
  }, []);

  const speak = useCallback((text: string) => {
    stopSpeaking();
    const fallback = () => {
      try {
        if (!window.speechSynthesis) return;
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = "en-US";
        utterance.rate = 0.9;
        window.speechSynthesis.speak(utterance);
      } catch { /* sesli okuma yok */ }
    };
    const tryAccent = (accents: string[]) => {
      const [accent, ...rest] = accents;
      if (!accent) { fallback(); return; }
      const audio = new Audio(`/api/backend/api/WordAudio/${accent}/${encodeURIComponent(text.toLowerCase())}`);
      audioRef.current = audio;
      const next = () => { if (audioRef.current === audio) tryAccent(rest); };
      audio.addEventListener("error", next, { once: true });
      audio.play().catch(next);
    };
    tryAccent(["us", "gb"]);
  }, [stopSpeaking]);

  // Kelime değişince: bir kez oku ve anlamını getir.
  useEffect(() => {
    const controller = new AbortController();
    setState({ status: "loading" });
    speak(word);
    (async () => {
      try {
        const response = await fetch(`/api/backend/api/Words/lookup?text=${encodeURIComponent(word)}`, { signal: controller.signal, cache: "no-store" });
        if (response.status === 404) { setState({ status: "missing" }); return; }
        if (!response.ok) throw new Error("failed");
        const body = (await response.json()) as { data?: WordDetail };
        setState(body.data ? { status: "found", word: body.data } : { status: "missing" });
      } catch (value) {
        if ((value as { name?: string })?.name !== "AbortError") setState({ status: "error" });
      }
    })();
    return () => { controller.abort(); stopSpeaking(); };
  }, [word, speak, stopSpeaking]);

  useEffect(() => {
    closeRef.current?.focus();
    const onKey = (event: KeyboardEvent) => { if (event.key === "Escape") onClose(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  const found = state.status === "found" ? state.word : null;
  const baseForm = found && found.text.toLowerCase() !== word.toLowerCase() ? found.text : null;

  return <aside className="word-panel" role="dialog" aria-label={`${word} kelimesinin anlamı`}>
    <div className="word-panel-top">
      <span className="panel-kicker">KELİME</span>
      <button type="button" ref={closeRef} className="word-panel-close" onClick={onClose} aria-label="Kapat ve videoya devam et" title="Kapat ve videoya devam et">✕</button>
    </div>

    <h2 className="word-panel-title">{word}</h2>
    {baseForm && <p className="word-panel-base">↳ ana biçim: <strong>{baseForm}</strong></p>}

    <div className="word-panel-actions">
      {found && found.audioAccents && found.audioAccents.length > 0 && !baseForm
        ? <PronunciationButton word={found.text} accents={found.audioAccents}/>
        : <button type="button" className="button word-panel-listen" onClick={() => speak(word)}>🔊 Tekrar dinle</button>}
      {found && <SaveWordButton wordId={found.id} initialSaved={found.isSaved === true} compact/>}
    </div>

    {state.status === "loading" && <p className="word-panel-note" role="status">Anlamı getiriliyor…</p>}
    {state.status === "missing" && <p className="word-panel-note">Bu kelime sözlüğümüzde bulunamadı.</p>}
    {state.status === "error" && <p className="word-panel-note" role="alert">Anlam alınamadı. Bağlantını kontrol edip tekrar dene.</p>}

    {found && <div className="word-panel-body">
      <div className="word-panel-meta">
        {found.partOfSpeech && <span>{found.partOfSpeech}</span>}
        {found.cefrLevel && <span>{found.cefrLevel}</span>}
        {found.ipa && <span className="word-panel-ipa">{found.ipa}</span>}
      </div>
      <p className="word-panel-def">{found.definition}</p>
      {found.exampleSentences.length > 0 && <div className="word-panel-block"><h3>Örnek</h3>{found.exampleSentences.slice(0, 2).map((sentence) => <blockquote key={sentence}>“{sentence}”</blockquote>)}</div>}
      {found.synonyms.length > 0 && <div className="word-panel-block"><h3>Eş anlamlılar</h3><p>{found.synonyms.slice(0, 6).join(", ")}</p></div>}
      <Link className="word-panel-link" href={`/learn/word/${encodeURIComponent(found.text)}`} target="_blank">Kelime sayfasını aç ↗</Link>
    </div>}
    <p className="word-panel-hint">Kapatınca video kaldığı yerden devam eder.</p>
  </aside>;
}
