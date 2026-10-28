import Link from "next/link";
import { getSavedWords, getUserProfile } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import WordSearch from "./word-search";

const number = new Intl.NumberFormat("tr-TR");

export const instant = false;

export default async function LearnHomePage() {
  const { token, user } = await getSession();
  const [profile, saved] = await Promise.all([getUserProfile(token), getSavedWords(token, 4)]);

  return <>
    <section className="learn-welcome"><div><span className="eyebrow"><span className="eyebrow-dot"/> WORDWISE ÖĞRENME ALANI</span><h1>Hoş geldin, {profile?.name || user?.name || "öğrenci"} <span>✳</span></h1><p>Bugün küçük bir adım daha atalım.</p></div><Link className="button" href="/learn/words">Kelime defterim <span>↗</span></Link></section>
    <WordSearch/>
    {!profile && <div className="learn-alert" role="status">Profil bilgisi API’den alınamadı. API’nin çalıştığını ve oturumunun geçerli olduğunu kontrol et.</div>}
    <section className="learn-stats" aria-label="Öğrenme istatistikleri">
      <article><span>Toplam XP</span><strong>{number.format(profile?.totalXp ?? 0)}</strong><small>öğrenme yolculuğunda</small></article>
      <article><span>Kaydedilen kelime</span><strong>{number.format(profile?.savedWordCount ?? 0)}</strong><small>{profile?.pendingReviewCount ?? 0} tekrar bekliyor</small></article>
      <article><span>Ezberlenen</span><strong>{number.format(profile?.masteredWordCount ?? 0)}</strong><small>kelime</small></article>
      <article><span>Quiz başarısı</span><strong>%{profile?.quizAccuracy ?? 0}</strong><small>{profile?.totalQuizAnswers ?? 0} yanıt</small></article>
    </section>
    <section className="learn-panel">
      <div className="learn-panel-heading"><div><span className="panel-kicker">KELİME DEFTERİM</span><h2>Son kaydettiklerin</h2></div><Link href="/learn/words">Tümünü gör →</Link></div>
      {!saved && <p className="learn-empty">Kaydedilen kelimeler API’den alınamadı.</p>}
      {saved?.items.length === 0 && <p className="learn-empty">Henüz kelime kaydetmedin. Yeni kelimeler keşfetmek için <Link href="/">vitrine göz at.</Link></p>}
      {saved && saved.items.length > 0 && <div className="learn-word-list">{saved.items.map((word) => <article key={word.id}><div><h3>{word.text}</h3><span>{word.cefrLevel || "Seviye belirtilmemiş"} · {word.partOfSpeech || "Kelime"}</span></div><p>{word.definition}</p><span className="learn-level">Seviye {word.knownLevel}/5</span></article>)}</div>}
    </section>
  </>;
}
