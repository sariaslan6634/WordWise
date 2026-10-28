import Link from "next/link";
import { getAdminWords } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import { WordFilters } from "./word-filters";

export const instant = false;

type Search = Record<string, string | string[] | undefined>;
export default async function AdminWordsPage({ searchParams }: { searchParams: Promise<Search> }) {
  const params = await searchParams;
  const value = (key: string) => typeof params[key] === "string" ? params[key] as string : "";
  const page = Math.max(1, Number(value("page")) || 1);
  const searchText = value("searchText");
  const cefrLevel = value("cefrLevel");
  const partOfSpeech = value("partOfSpeech");
  const status = value("status") || "all";
  const { token, user } = await getSession();
  const live = user?.role === "Admin" ? await getAdminWords(token, { page, pageSize: 7, searchText, cefrLevel, partOfSpeech, status }) : null;
  const words = live?.items ?? [];
  const linkFor = (target: number) => {
    const query = new URLSearchParams({ page: String(target) });
    if (searchText) query.set("searchText", searchText);
    if (cefrLevel) query.set("cefrLevel", cefrLevel);
    if (partOfSpeech) query.set("partOfSpeech", partOfSpeech);
    if (status) query.set("status", status);
    return `/admin/words?${query}`;
  };
  return <div className="admin-content">
    <div className="admin-page-heading words-heading"><div><div className="eyebrow"><span className="eyebrow-dot"/> İÇERİK YÖNETİMİ</div><h1>Kelime kütüphanesi <span>✳</span></h1><p>Onaylanan kelimeleri ara ve filtrele.</p></div><div className="heading-actions"><span className={`data-pill ${live ? "live" : "preview"}`}><i/>{live ? "Canlı API verisi" : "API bağlantısı yok"}</span></div></div>
    <WordFilters searchText={searchText} cefrLevel={cefrLevel} partOfSpeech={partOfSpeech} status={status} totalCount={live?.totalCount ?? 0}/>
    <div className="word-table panel"><div className="word-table-head"><span>İNGİLİZCE KELİME</span><span>TANIM</span><span>KATEGORİ</span><span>SEVİYE</span><span>DURUM</span><span/></div>{words.map((word, index) => { const label = !word.isApproved ? "Taslak" : word.isPublished ? "Aktif" : "Pasif"; const tone = !word.isApproved ? "draft" : word.isPublished ? "published" : "passive"; return <div className="word-table-row" key={word.id}><div className="word-title"><span className={`word-initial hue-${index % 4}`}>{word.text.slice(0, 1)}</span><span><b>{word.text}</b><small>{word.partOfSpeech || "Kelime"}</small></span></div><p>{word.definition}</p><span className="category-tag">{word.category || "Genel"}</span><span className="cefr-tag">{word.cefrLevel || "—"}</span><span className={`publish-tag ${tone}`}><i/>{label}</span><span/></div>; })}</div>
    {!live && <div className="preview-banner warning words-preview"><span>ⓘ</span><div><b>Kelime listesi API’den alınamadı.</b><small>Gerçek veri için API bağlantısını kontrol et.</small></div><Link href="/admin-login">Giriş ekranı <span>↗</span></Link></div>}
    {live && <div className="table-pagination"><span>Sayfa <b>{live.page}</b> / {live.totalPages || 1}</span><div><Link aria-disabled={!live.hasPreviousPage} className={!live.hasPreviousPage ? "disabled" : ""} href={linkFor(Math.max(1, page - 1))}>← Önceki</Link><Link aria-disabled={!live.hasNextPage} className={!live.hasNextPage ? "disabled" : ""} href={linkFor(page + 1)}>Sonraki →</Link></div></div>}
  </div>;
}
