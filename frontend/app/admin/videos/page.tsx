import Link from "next/link";
import { connection } from "next/server";
import { getAdminPendingContent, getAdminVideoCandidatesByWordId } from "@/lib/server-api";
import { getSession } from "@/lib/session";

type SearchParams = Promise<{ wordId?: string; word?: string }>;
const wordIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export const instant = false;
const date = new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" });

export default async function AdminVideosPage({ searchParams }: { searchParams: SearchParams }) {
  await connection();
  const [{ token }, params] = await Promise.all([getSession(), searchParams]);
  const wordId = params.wordId && wordIdPattern.test(params.wordId) ? params.wordId : undefined;
  const [pending, wordCandidates] = await Promise.all([
    wordId ? Promise.resolve(null) : getAdminPendingContent(token),
    wordId ? getAdminVideoCandidatesByWordId(wordId, token) : Promise.resolve(null),
  ]);
  const candidates = (wordId ? wordCandidates : pending?.videoCandidates)
    ?.filter((candidate) => !candidate.isApproved && !candidate.isRejected);
  const wordText = params.word?.trim() || wordCandidates?.[0]?.wordText;
  const dataLoaded = wordId ? wordCandidates !== null : pending !== null;

  return <div className="admin-content">
    <div className="admin-page-heading"><div><div className="eyebrow"><span className="eyebrow-dot"/> İÇERİK ONAYI</div><h1>{wordId && wordText ? `${wordText} video adayları` : "Video adayları"} <span>✳</span></h1><p>{wordId ? `${wordText || "Bu kelime"} için YouTube'dan bulunan adaylar.` : "Yayın öncesinde incelenmesi gereken videolar."}</p></div><div className="heading-actions"><span className={`data-pill ${dataLoaded ? "live" : "preview"}`}><i/>{dataLoaded ? "Canlı API verisi" : "API bağlantısı yok"}</span>{wordId && <Link className="view-site" href="/admin/videos">Tüm adaylar ↗</Link>}</div></div>
    {!dataLoaded && <div className="preview-banner warning"><span>ⓘ</span><div><b>Video adayları alınamadı.</b><small>API bağlantısını ve admin oturumunu kontrol et.</small></div></div>}
    <section className="video-review-groups"><article className="review-group"><div className="review-group-heading"><div><span className="panel-kicker">VİDEO ADAYLARI</span><h2>{wordId && wordText ? `${wordText} için onay bekleyenler` : "Onay bekleyen videolar"}</h2></div><span className="queue-count">{candidates?.length ?? 0}</span></div>
      {dataLoaded && candidates?.length === 0 && <p className="pending-review-empty">{wordId ? `${wordText || "Bu kelime"} için bekleyen YouTube videosu bulunamadı. YouTube Data API v3 anahtarını ve kota durumunu kontrol et.` : "Onay bekleyen video adayı yok."}</p>}
      <div className="review-items">{candidates?.map((video) => <article className="review-item" key={video.id}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img className="review-thumb" src={video.thumbnailUrl || `https://img.youtube.com/vi/${video.youtubeId}/hqdefault.jpg`} alt=""/><div><h3>{video.wordText}</h3><small>{video.channelTitle || "YouTube"} · {date.format(new Date(video.fetchedAt))}</small></div><p>{video.title}{video.description ? ` — ${video.description}` : ""}</p><Link href={`/admin/videos/preview?candidateId=${encodeURIComponent(video.id)}`}>Önizle ve zaman seç ↗</Link></article>)}</div>
    </article></section>
  </div>;
}
