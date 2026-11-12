"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import PronunciationButton from "@/components/pronunciation-button";
import SaveWordButton from "@/components/save-word-button";
import { cefrLabel } from "@/lib/cefr";
import type { WordDetail } from "@/lib/contracts";

type State = { status: "loading" } | { status: "found"; word: WordDetail } | { status: "missing" } | { status: "error"; detail: string };

/**
 * Altyazıda tıklanan kelimenin anlamını sağda açılan pencerede gösterir ve kelimeyi bir kez okur.
 * Okuma sırası: kelimenin Amerikan/İngiliz kaydı → tarayıcının İngilizce sesi → ana biçimin kaydı. Kapatınca (ya da kelime değişince) ses durur.
 */
/** `playKey` her tıklamada artar: aynı kelimeye tekrar tıklanınca da kelime yeniden okunur. */
export default function WordLookupPanel({ word, playKey, onClose }: { word: string; playKey: number; onClose: () => void }) {
  const [state, setState] = useState<State>({ status: "loading" });
  const [silent, setSilent] = useState(false); // hiçbir kaynaktan ses bulunamadı
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const closeRef = useRef<HTMLButtonElement>(null);

  const runRef = useRef(0); // her okuma isteğine numara: yeni tıklama eskisini iptal eder
  const baseRef = useRef<string | null>(null); // çekimli kelimenin sözlükteki ana biçimi (challenges → challenge)
  const pendingBaseRef = useRef(0); // ses bulunamadı ve ana biçim bekleniyor: bekleyen okuma numarası

  const stopSpeaking = useCallback(() => {
    runRef.current += 1;
    audioRef.current?.pause();
    audioRef.current = null;
    try { window.speechSynthesis?.cancel(); } catch { /* desteklenmiyor */ }
  }, []);

  /** Kayıtlı ses dosyasını çalar (Amerikan, olmazsa İngiliz). Başlayabildiyse true. */
  const playRecording = useCallback(async (text: string, run: number): Promise<boolean> => {
    for (const accent of ["us", "gb"]) {
      if (run !== runRef.current) return true; // iptal edildi
      const url = `/api/backend/api/WordAudio/${accent}/${encodeURIComponent(text.toLowerCase())}`;
      const ok = await new Promise<boolean>((resolve) => {
        const audio = new Audio(url);
        audioRef.current = audio;
        let settled = false;
        const finish = (value: boolean) => { if (!settled) { settled = true; resolve(value); } };
        audio.addEventListener("playing", () => finish(true), { once: true });
        audio.addEventListener("error", () => finish(false), { once: true });
        audio.play().catch(() => finish(false));
      });
      if (ok) return true;
    }
    return false;
  }, []);

  /** Tarayıcıda İngilizce (tercihen Amerikan) bir ses varsa onunla okur; yoksa false. Türkçe sesle okumaz. */
  const speakWithEnglishVoice = useCallback(async (text: string, run: number): Promise<boolean> => {
    const synth = typeof window !== "undefined" ? window.speechSynthesis : undefined;
    if (!synth) return false;
    let voices = synth.getVoices();
    if (voices.length === 0) {
      voices = await new Promise<SpeechSynthesisVoice[]>((resolve) => {
        const done = () => resolve(synth.getVoices());
        synth.addEventListener("voiceschanged", done, { once: true });
        setTimeout(done, 800);
      });
    }
    const english = voices.filter((voice) => /^en[-_]/i.test(voice.lang));
    const us = english.filter((voice) => /^en[-_]US$/i.test(voice.lang));
    const voice = us.find((item) => /natural|online|google|aria|jenny|samantha/i.test(item.name)) ?? us[0] ?? english[0];
    if (!voice) return false;
    if (run !== runRef.current) return true;
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.voice = voice;
    utterance.lang = voice.lang;
    utterance.rate = 0.9;
    synth.speak(utterance);
    return true;
  }, []);

  /** Okuma sırası: kelimenin kendi kaydı → İngilizce sesli okuma → ana biçimin kaydı (challenges yoksa challenge). */
  const speak = useCallback(async (text: string) => {
    stopSpeaking();
    setSilent(false);
    const run = runRef.current;
    if (await playRecording(text, run)) return;
    if (run !== runRef.current) return;
    if (await speakWithEnglishVoice(text, run)) return;
    if (run !== runRef.current) return;
    const base = baseRef.current;
    if (base) { if (!(await playRecording(base, run)) && run === runRef.current) setSilent(true); }
    else pendingBaseRef.current = run; // anlam gelince ana biçimin kaydı çalınır
  }, [stopSpeaking, playRecording, speakWithEnglishVoice]);

  // Her tıklamada kelimeyi oku (aynı kelimeye tekrar tıklayınca da).
  useEffect(() => {
    void speak(word);
    return stopSpeaking;
  }, [word, playKey, speak, stopSpeaking]);

  // Kelime değişince anlamını getir.
  useEffect(() => {
    const controller = new AbortController();
    setState({ status: "loading" });
    baseRef.current = null;
    pendingBaseRef.current = 0;
    (async () => {
      try {
        const response = await fetch(`/api/backend/api/Words/lookup?text=${encodeURIComponent(word)}`, { signal: controller.signal, cache: "no-store" });
        if (response.status === 404) { setState({ status: "missing" }); return; }
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const body = (await response.json()) as { data?: WordDetail };
        setState(body.data ? { status: "found", word: body.data } : { status: "missing" });
        if (body.data && body.data.text.toLowerCase() === word.toLowerCase() && pendingBaseRef.current !== 0 && pendingBaseRef.current === runRef.current) {
          pendingBaseRef.current = 0;
          setSilent(true); // kelimenin kendisi zaten ana biçim: çalacak başka kayıt yok
        } else if (body.data && body.data.text.toLowerCase() !== word.toLowerCase()) {
          baseRef.current = body.data.text;
          const run = pendingBaseRef.current;
          if (run !== 0 && run === runRef.current) { pendingBaseRef.current = 0; void playRecording(body.data.text, run).then((ok) => { if (!ok && run === runRef.current) setSilent(true); }); }
        }
      } catch (value) {
        if ((value as { name?: string })?.name !== "AbortError") setState({ status: "error", detail: value instanceof Error ? value.message : "" });
      }
    })();
    return () => controller.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [word]);

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
        : <button type="button" className="button word-panel-listen" onClick={() => void speak(word)}>🔊 Tekrar dinle</button>}
      {found && <SaveWordButton key={found.id} wordId={found.id} initialSaved={found.isSaved === true} compact iconOnly/>}
    </div>

    {silent && <p className="word-panel-note">Bu kelime için ses kaydı bulunamadı.</p>}
    {state.status === "loading" && <p className="word-panel-note" role="status">Anlamı getiriliyor…</p>}
    {state.status === "missing" && <p className="word-panel-note">Bu kelime sözlüğümüzde bulunamadı.</p>}
    {state.status === "error" && <p className="word-panel-note" role="alert">Anlam alınamadı{state.detail ? ` (${state.detail})` : ""}. Bağlantını kontrol edip tekrar dene.</p>}

    {found && <div className="word-panel-body">
      <div className="word-panel-meta">
        {found.partOfSpeech && <span>{found.partOfSpeech}</span>}
        {found.cefrLevel && <span>{cefrLabel(found.cefrLevel)}</span>}
        {found.ipa && <span className="word-panel-ipa">{found.ipa}</span>}
      </div>
      <p className="word-panel-def">{found.definition}</p>
      {found.exampleSentences.length > 0 && <div className="word-panel-block"><h3>Örnek</h3>{found.exampleSentences.slice(0, 2).map((sentence) => <blockquote key={sentence}>“{sentence}”</blockquote>)}</div>}
      {found.synonyms.length > 0 && <div className="word-panel-block"><h3>Eş anlamlılar</h3><p>{found.synonyms.slice(0, 6).join(", ")}</p></div>}
      <Link className="word-panel-link" href={`/learn/word/${encodeURIComponent(found.text)}`} target="_blank">Kelime sayfasını aç ↗</Link>
    </div>}
  </aside>;
}
