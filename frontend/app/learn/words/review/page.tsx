import Link from "next/link";
import { getReviewWords } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import ReviewSession from "./review-session";

export const instant = false;

export default async function ReviewPage({ searchParams }: { searchParams: Promise<{ pos?: string; all?: string }> }) {
  const [{ token }, { pos, all }] = await Promise.all([getSession(), searchParams]);
  const includeNotDue = all === "1";
  const words = await getReviewWords(token, { includeNotDue, partOfSpeech: pos || undefined });

  if (!words) return <section className="learn-panel"><div className="learn-alert" role="status">Tekrar listesi API’den alınamadı. Oturumunu ve API bağlantısını kontrol et.</div><Link className="button" href="/learn/words">← Kelime defterim</Link></section>;

  if (words.length === 0) {
    const query = new URLSearchParams({ all: "1" });
    if (pos) query.set("pos", pos);
    return <section className="learn-panel review-empty">
      <span className="panel-kicker">TEKRAR</span>
      <h1>{includeNotDue ? "Tekrar edilecek kelime yok" : "Şimdilik tekrar edilecek kelime yok 🎉"}</h1>
      <p>{includeNotDue ? "Bu seçimde kayıtlı kelimen yok." : "Vakti gelen kelime yok. İstersen kayıtlı kelimelerini yine de baştan tekrar edebilirsin."}</p>
      <div className="review-actions">
        <Link className="button" href="/learn/words">← Kelime defterim</Link>
        {!includeNotDue && <Link className="button" href={`/learn/words/review?${query}`}>Yine de tekrar et</Link>}
      </div>
    </section>;
  }

  return <ReviewSession words={words}/>;
}
