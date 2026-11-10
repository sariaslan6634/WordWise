"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import SaveWordButton from "@/components/save-word-button";
import RemovedWords from "./removed-words";
import type { RemovedWord, SavedWord } from "@/lib/contracts";

export default function SavedWordsView({ words, totalCount, removed }: { words: SavedWord[]; totalCount: number; removed: RemovedWord[] }) {
  const [pos, setPos] = useState("");

  // Filtre seçenekleri kullanıcının kendi kelimelerindeki türlerden çıkar (noun, verb...).
  const partsOfSpeech = useMemo(
    () => [...new Set(words.map((word) => word.partOfSpeech).filter((value): value is string => Boolean(value)))].sort(),
    [words],
  );
  const visible = pos ? words.filter((word) => word.partOfSpeech === pos) : words;

  return <>
    <div className="learn-panel-heading">
      <div><span className="panel-kicker">KİŞİSEL KÜTÜPHANE</span><h1>Kelime defterim</h1><p>Kaydettiğin kelimeler ve tekrar durumları.</p></div>
      <div className="learn-heading-side"><span className="learn-count">{pos ? `${visible.length} / ` : ""}{totalCount} kelime</span><RemovedWords items={removed}/></div>
    </div>

    {words.length > 0 && <div className="learn-words-toolbar">
      <label className="learn-filter">
        <span>Tür</span>
        <select value={pos} onChange={(event) => setPos(event.target.value)} aria-label="Kelime türüne göre filtrele">
          <option value="">Tümü</option>
          {partsOfSpeech.map((value) => <option key={value} value={value}>{value}</option>)}
        </select>
      </label>
    </div>}

    {words.length === 0 && <p className="learn-empty">Henüz kaydedilmiş kelime yok. Bir kelime sayfasında “Kaydet” butonuna bas.</p>}
    {words.length > 0 && visible.length === 0 && <p className="learn-empty">Bu türde kayıtlı kelimen yok.</p>}
    {visible.length > 0 && <div className="learn-word-list has-actions">{visible.map((word) => <article key={word.id}>
      <div><h2><Link href={`/learn/word/${encodeURIComponent(word.text)}`}>{word.text}</Link></h2><span>{word.cefrLevel || "Seviye belirtilmemiş"} · {word.partOfSpeech || "Kelime"}</span></div>
      <p>{word.definition}</p>
      <div className="learn-word-meta"><span>Öğrenme seviyesi {word.knownLevel}/5</span><span>{word.reviewCount} tekrar</span></div>
      <div className="learn-word-actions"><SaveWordButton wordId={word.wordId} initialSaved compact/></div>
      {word.exampleSentence && <blockquote className="learn-example">“{word.exampleSentence}”</blockquote>}
      {word.personalNote && <small className="learn-note">Not: {word.personalNote}</small>}
    </article>)}</div>}
  </>;
}
