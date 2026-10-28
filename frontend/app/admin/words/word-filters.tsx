"use client";

import { useRef } from "react";

type Props = { searchText: string; cefrLevel: string; partOfSpeech: string; status: string; totalCount: number };

function submitOnChange(event: React.ChangeEvent<HTMLSelectElement>) {
  event.currentTarget.form?.requestSubmit();
}

export function WordFilters({ searchText, cefrLevel, partOfSpeech, status, totalCount }: Props) {
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  function searchAsYouType(event: React.ChangeEvent<HTMLInputElement>) {
    const form = event.currentTarget.form;
    if (searchTimer.current) clearTimeout(searchTimer.current);
    searchTimer.current = setTimeout(() => form?.requestSubmit(), 350);
  }

  return <form className="word-toolbar" action="/admin/words">
    <label className="word-search"><span>⌕</span><input name="searchText" defaultValue={searchText} onChange={searchAsYouType} placeholder="Kelime veya tanım ara..." aria-label="Kelime ara"/></label>
    <select className="filter-button" name="cefrLevel" defaultValue={cefrLevel} onChange={submitOnChange} aria-label="Seviye filtresi"><option value="">Tüm seviyeler</option>{["A1", "A2", "B1", "B2", "C1", "C2"].map((level) => <option key={level}>{level}</option>)}</select>
    <select className="filter-button" name="partOfSpeech" defaultValue={partOfSpeech} onChange={submitOnChange} aria-label="Kelime türü filtresi"><option value="">Tüm türler</option>{["noun", "verb", "adjective", "adverb", "phrase", "preposition"].map((type) => <option key={type} value={type}>{type}</option>)}</select>
    <select className="filter-button" name="status" defaultValue={status} onChange={submitOnChange} aria-label="Durum filtresi"><option value="all">Tümü</option><option value="active">Aktif</option><option value="passive">Pasif</option><option value="draft">Taslak</option></select>
    <span className="word-result-count"><b>{totalCount}</b> kelime</span>
  </form>;
}
