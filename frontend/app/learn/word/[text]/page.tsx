import Link from "next/link";
import { connection } from "next/server";
import { getWordByText } from "@/lib/server-api";
import { getSession } from "@/lib/session";

export const instant = false;

export default async function LearnWordPage({ params }: { params: Promise<{ text: string }> }) {
  await connection();
  const [{ token }, { text }] = await Promise.all([getSession(), params]);
  const word = await getWordByText(text, token);

  if (!word) return <section className="learn-panel word-not-found"><span className="panel-kicker">KELİME KÜTÜPHANESİ</span><h1>“{text}” henüz bulunamadı</h1><p>Kelime henüz onaylanmamış olabilir. Başka bir kelime arayabilir veya sonra tekrar deneyebilirsin.</p><Link className="button" href="/learn">Kelime ara</Link></section>;

  return <article className="learn-panel word-learning-page">
    <Link className="word-back-link" href="/learn">← Öğrenme alanına dön</Link>
    <header className="word-learning-heading"><div><span className="panel-kicker">{word.category || "KELİME KÜTÜPHANESİ"}</span><h1>{word.text}</h1><div className="word-learning-meta"><span>{word.ipa || "Telaffuz bilgisi eklenmemiş"}</span><span>{word.partOfSpeech || "Kelime"}</span>{word.cefrLevel && <span>{word.cefrLevel}</span>}</div></div><span className="word-learning-spelling">Yazılış <b>{word.text}</b></span></header>
    <section className="word-learning-definition"><h2>Anlamı</h2><p>{word.definition}</p></section>
    {word.videos.length > 0 && <section className="word-learning-videos"><div className="word-learning-section-heading"><div><span className="panel-kicker">BAĞLAM İÇİNDE DİNLE</span><h2>{word.text} ile video örneği</h2></div></div><div className="word-learning-video-grid">{word.videos.map((video) => {
      const query = new URLSearchParams({ controls: "1", end: String(video.endSec), playsinline: "1", rel: "0" });
      if (video.startSec > 0) query.set("start", String(video.startSec));
      return <article className="word-learning-video" key={video.id}><div className="word-learning-player"><iframe src={`https://www.youtube-nocookie.com/embed/${encodeURIComponent(video.youtubeId)}?${query.toString()}`} title={`${word.text}: ${video.title}`} allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerPolicy="strict-origin-when-cross-origin" allowFullScreen/></div><h3>{video.title}</h3><p>{video.channelTitle || "YouTube"} · {video.startSec}s–{video.endSec}s</p></article>;
    })}</div></section>}
    <div className="word-learning-columns"><section className="word-learning-definition"><h2>Örnek cümleler</h2>{word.exampleSentences.length > 0 ? <ul>{word.exampleSentences.map((sentence, index) => <li key={`${sentence}-${index}`}>{sentence}</li>)}</ul> : <p>Henüz örnek cümle yok.</p>}</section><section className="word-learning-definition"><h2>Benzer kelimeler</h2><p>{word.synonyms.length > 0 ? word.synonyms.join(", ") : "Benzer kelime bulunmuyor."}</p></section><section className="word-learning-definition"><h2>Zıt kelimeler</h2><p>{word.antonyms.length > 0 ? word.antonyms.join(", ") : "Zıt kelime bulunmuyor."}</p></section></div>
  </article>;
}
