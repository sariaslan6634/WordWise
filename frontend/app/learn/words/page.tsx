import { getSavedWords } from "@/lib/server-api";
import { getSession } from "@/lib/session";

export const instant = false;

export default async function SavedWordsPage() {
  const { token } = await getSession();
  const page = await getSavedWords(token, 50);

  return <section className="learn-panel learn-words-page">
    <div className="learn-panel-heading"><div><span className="panel-kicker">KİŞİSEL KÜTÜPHANE</span><h1>Kelime defterim</h1><p>Kaydettiğin kelimeler ve tekrar durumları.</p></div><span className="learn-count">{page?.totalCount ?? 0} kelime</span></div>
    {!page && <div className="learn-alert" role="status">Kelime defteri API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div>}
    {page?.items.length === 0 && <p className="learn-empty">Henüz kaydedilmiş kelime yok.</p>}
    {page && page.items.length > 0 && <div className="learn-word-list">{page.items.map((word) => <article key={word.id}><div><h2>{word.text}</h2><span>{word.cefrLevel || "Seviye belirtilmemiş"} · {word.partOfSpeech || "Kelime"}</span></div><p>{word.definition}</p><div className="learn-word-meta"><span>Öğrenme seviyesi {word.knownLevel}/5</span><span>{word.reviewCount} tekrar</span></div>{word.personalNote && <small className="learn-note">Not: {word.personalNote}</small>}</article>)}</div>}
  </section>;
}
