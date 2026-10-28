import { connection } from "next/server";
import { getAdminWords } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import { ImportWords, ReviewWordVideosButton } from "./word-candidate-tools";

export const instant = false;

export default async function AdminWordCandidatesPage() {
  await connection();
  const { token } = await getSession();
  const pending = await getAdminWords(token, { page: 1, pageSize: 100, isApproved: false });
  return <div className="admin-content">
    <div className="admin-page-heading"><div><div className="eyebrow"><span className="eyebrow-dot"/> İÇERİK ONAYI</div><h1>Kelime adayları <span>✳</span></h1><p>Kelime için video seç, zaman aralığını belirle ve önizlemeden sonra onayla.</p></div><span className={`data-pill ${pending ? "live" : "preview"}`}><i/>{pending ? "Canlı API verisi" : "API bağlantısı yok"}</span></div>
    <section className="panel candidate-import-panel"><div className="panel-heading"><div><span className="panel-kicker">OTOMATİK KELİME BULMA</span><h2>Yeni adaylar bul</h2></div></div><ImportWords/><p className="candidate-import-note">Datamuse sözcükleri, Free Dictionary tanım ve örnekleri, Datamuse ise eş/karşıt anlamları sağlar. Bulunan kelimeler için video adayları da aranır.</p></section>
    {!pending && <div className="preview-banner warning"><span>ⓘ</span><div><b>Aday listesi alınamadı.</b><small>API bağlantısını ve admin oturumunu kontrol et.</small></div></div>}
        <section className="candidate-list"><div className="review-group-heading"><div><span className="panel-kicker">ONAY BEKLEYENLER</span><h2>Kelime adayları</h2></div><span className="queue-count">{pending?.totalCount ?? 0}</span></div>
      {pending?.items.length === 0 && <p className="pending-review-empty">Bekleyen kelime adayı yok. Yukarıdan kelime içe aktarabilirsin.</p>}
      {pending?.items.map((word) => <article className="review-item pending-word-item" key={word.id}><span className="word-initial hue-0">{word.text.slice(0,1).toUpperCase()}</span><div><h3>{word.text}</h3><small>{word.partOfSpeech || "Kelime"} · {word.category || "Genel"}</small></div><p>{word.definition}</p><ReviewWordVideosButton id={word.id} wordText={word.text}/></article>)}
    </section>
  </div>;
}
