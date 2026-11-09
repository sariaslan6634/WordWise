"use client";

import Link from "next/link";
import { useRef, useState } from "react";
import PronunciationButton from "@/components/pronunciation-button";
import type { ReviewWord } from "@/lib/contracts";

/** Kayıtlı kelimeleri kart kart tekrar ettirir: kelimeyi gör, anlamını çevir, "Bildim / Bilemedim" de. */
export default function ReviewSession({ words }: { words: ReviewWord[] }) {
  const [queue, setQueue] = useState(words);
  const [revealed, setRevealed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [known, setKnown] = useState(0);
  const [missed, setMissed] = useState(0);
  const retried = useRef(new Set<string>()); // bilemeyen kelime oturumda bir kez daha sorulur
  const [extra, setExtra] = useState(0); // tekrar sorulan kelime sayısı (ilerleme çubuğu için)

  const total = words.length + extra;
  const answered = known + missed;
  const card = queue[0];

  async function answer(isKnown: boolean) {
    if (!card || busy) return;
    setBusy(true);
    setError("");
    try {
      const response = await fetch(`/api/backend/api/UserWord/${encodeURIComponent(card.wordId)}/review`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ known: isKnown }),
      });
      if (!response.ok) throw new Error("failed");
      if (isKnown) setKnown((value) => value + 1); else setMissed((value) => value + 1);
      setQueue((current) => {
        const [first, ...rest] = current;
        if (!isKnown && !retried.current.has(first.wordId)) {
          retried.current.add(first.wordId);
          setExtra((value) => value + 1);
          return [...rest, first];
        }
        return rest;
      });
      setRevealed(false);
    } catch {
      setError("Cevap kaydedilemedi, tekrar dene.");
    } finally {
      setBusy(false);
    }
  }

  if (!card) {
    return <section className="learn-panel review-empty">
      <span className="panel-kicker">TEKRAR TAMAM</span>
      <h1>Harika, bu seri bitti 🎉</h1>
      <p>{known} kelimeyi bildin{missed > 0 ? `, ${missed} kez takıldın` : ""}. Bildiklerin daha seyrek, bilemediklerin daha sık karşına çıkacak.</p>
      <div className="review-actions"><Link className="button" href="/learn/words">← Kelime defterim</Link></div>
    </section>;
  }

  return <section className="learn-panel review-session">
    <div className="review-top">
      <Link className="word-back-link" href="/learn/words">← Kelime defterim</Link>
      <span className="learn-count">{Math.min(answered + 1, total)} / {total}</span>
    </div>
    <div className="review-progress" aria-hidden="true"><i style={{ width: `${Math.round((answered / total) * 100)}%` }}/></div>

    <article className="review-card" key={card.wordId + answered}>
      <span className="panel-kicker">{card.partOfSpeech || "KELİME"}</span>
      <h1>{card.text}</h1>
      {card.ipa && <p className="review-ipa">{card.ipa}</p>}
      <PronunciationButton word={card.text} accents={card.audioAccents}/>
      {revealed ? <div className="review-back">
        <p className="review-definition">{card.definition}</p>
        {card.exampleSentences[0] && <blockquote className="learn-example">“{card.exampleSentences[0]}”</blockquote>}
      </div> : <button type="button" className="button review-reveal" onClick={() => setRevealed(true)}>Anlamını göster</button>}
    </article>

    {revealed && <div className="review-actions">
      <button type="button" className="button review-miss" disabled={busy} onClick={() => void answer(false)}>✕ Bilemedim</button>
      <button type="button" className="button review-known" disabled={busy} onClick={() => void answer(true)}>✓ Biliyordum</button>
    </div>}
    {error && <p className="cinema-error" role="alert">{error}</p>}
  </section>;
}
