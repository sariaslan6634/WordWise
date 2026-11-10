"use client";

import { useRouter } from "next/navigation";
import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from "react";

type Suggestion = { text: string; partOfSpeech: string | null };

export default function WordSearch() {
  const router = useRouter();
  const [text, setText] = useState("");
  const [items, setItems] = useState<Suggestion[]>([]);
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const requestId = useRef(0);

  // Yazdıkça öneri getir (kısa bir bekleme ile); en son yazılana ait cevap geçerlidir.
  useEffect(() => {
    const prefix = text.trim();
    const id = ++requestId.current;
    if (!prefix) { setItems([]); return; }
    const timer = setTimeout(async () => {
      try {
        const response = await fetch(`/api/backend/api/Words/suggest?prefix=${encodeURIComponent(prefix)}&limit=8`);
        if (!response.ok) return;
        const body = (await response.json()) as { success?: boolean; data?: Suggestion[] };
        if (id !== requestId.current) return;
        setItems(body.success === false ? [] : body.data ?? []);
        setActive(-1);
      } catch { /* öneri alınamazsa arama yine de çalışır */ }
    }, 120);
    return () => clearTimeout(timer);
  }, [text]);

  function go(word: string) {
    const value = word.trim();
    if (!value) return;
    setOpen(false);
    router.push(`/learn/word/${encodeURIComponent(value)}`);
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    go(active >= 0 && items[active] ? items[active].text : text);
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown" && items.length > 0) {
      event.preventDefault();
      setOpen(true);
      setActive((current) => (current + 1) % items.length);
    } else if (event.key === "ArrowUp" && items.length > 0) {
      event.preventDefault();
      setOpen(true);
      setActive((current) => (current <= 0 ? items.length - 1 : current - 1));
    } else if (event.key === "Escape") {
      setOpen(false);
      setActive(-1);
    }
  }

  const showList = open && items.length > 0;

  return <section className="learn-word-search panel">
    <div><span className="panel-kicker">BUGÜN NE ÖĞRENMEK İSTİYORSUN?</span><h2>Bir kelime ara</h2><p>Tanımını, telaffuzunu, cümlelerini ve videolu örneğini keşfet.</p></div>
    <form onSubmit={submit}>
      <label className="sr-only" htmlFor="learn-word-search">İngilizce kelime</label>
      <div className="word-suggest" onBlur={(event) => { if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false); }}>
        <input
          id="learn-word-search"
          value={text}
          onChange={(event) => { setText(event.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onKeyDown={onKeyDown}
          placeholder="Örn. apple"
          autoComplete="off"
          role="combobox"
          aria-expanded={showList}
          aria-controls="learn-word-suggestions"
          aria-autocomplete="list"
          aria-activedescendant={active >= 0 ? `suggestion-${active}` : undefined}
          required
        />
        {showList && <ul id="learn-word-suggestions" role="listbox" className="word-suggest-list">
          {items.map((item, index) => <li key={item.text} id={`suggestion-${index}`} role="option" aria-selected={index === active}>
            <button type="button" tabIndex={-1} className={index === active ? "is-active" : ""} onMouseDown={(event) => event.preventDefault()} onClick={() => go(item.text)}>
              <b>{item.text.slice(0, text.trim().length)}</b>{item.text.slice(text.trim().length)}
            </button>
          </li>)}
        </ul>}
      </div>
      <button className="button" type="submit">Kelimeyi öğren →</button>
    </form>
  </section>;
}
