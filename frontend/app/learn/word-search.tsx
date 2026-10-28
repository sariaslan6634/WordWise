"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

export default function WordSearch() {
  const router = useRouter();
  const [text, setText] = useState("");

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const word = text.trim();
    if (word) router.push(`/learn/word/${encodeURIComponent(word)}`);
  }

  return <section className="learn-word-search panel">
    <div><span className="panel-kicker">BUGÜN NE ÖĞRENMEK İSTİYORSUN?</span><h2>Bir kelime ara</h2><p>Tanımını, telaffuzunu, cümlelerini ve videolu örneğini keşfet.</p></div>
    <form onSubmit={submit}><label className="sr-only" htmlFor="learn-word-search">İngilizce kelime</label><input id="learn-word-search" value={text} onChange={(event) => setText(event.target.value)} placeholder="Örn. apple" required/><button className="button" type="submit">Kelimeyi öğren →</button></form>
  </section>;
}
