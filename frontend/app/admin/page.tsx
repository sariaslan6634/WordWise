import Link from "next/link";
import { connection } from "next/server";
import type { DashboardStats } from "@/lib/contracts";
import { getAdminPendingContent, getAdminStats } from "@/lib/server-api";
import { getSession } from "@/lib/session";

export const instant = false;

const emptyStats: DashboardStats = {
  totalWords: 0, totalVideos: 0, totalQuizQuestions: 0, totalUsers: 0,
  newUsersToday: 0, activeUsersLast7Days: 0, activeUsersLast30Days: 0,
  quizAnswersLast7Days: 0, wordsAddedLast24Hours: 0, videosAddedLast24Hours: 0,
  sessionsOpenedLast24Hours: 0, usersRegisteredLast24Hours: 0, pendingWordCount: 0,
  dailyUserActivity: [], pendingVideoCandidates: 0, todayCorrectAnswers: 0,
  todayWrongAnswers: 0, todayTotalAnswers: 0, totalXpEarned: 0,
  topSearchedWords: [], hardestQuestions: [],
};

const number = new Intl.NumberFormat("tr-TR");

function getChartDays(stats: DashboardStats) {
  const activityByDate = new Map(stats.dailyUserActivity.map((item) => [item.date.slice(0, 10), item.activeUsers]));
  const today = new Date();
  today.setUTCHours(0, 0, 0, 0);
  return Array.from({ length: 30 }, (_, index) => {
    const date = new Date(today);
    date.setUTCDate(today.getUTCDate() - 29 + index);
    const key = date.toISOString().slice(0, 10);
    return { key, date, users: activityByDate.get(key) ?? 0 };
  });
}

export default async function AdminDashboardPage() {
  await connection();
  const { token, user } = await getSession();
  const [apiStats, pendingContent] = await Promise.all([getAdminStats(token), getAdminPendingContent(token)]);
  const stats = apiStats ?? emptyStats;
  const chartDays = getChartDays(stats);
  const maxUsers = Math.max(...chartDays.map((day) => day.users), 1);
  const axisLabels = [maxUsers, maxUsers * 2 / 3, maxUsers / 3, 0].map((value) => Number(value.toFixed(1)));
  const points = chartDays.map((day, index) => ({
    x: 18 + (index * 624) / 29,
    y: 185 - (day.users / maxUsers) * 150,
  }));
  const linePath = points.map((point, index) => `${index === 0 ? "M" : "L"}${point.x.toFixed(1)},${point.y.toFixed(1)}`).join(" ");
  const areaPath = `${linePath} L642,190 L18,190 Z`;
  const chartLabels = [0, 7, 14, 21, 29].map((index) => new Intl.DateTimeFormat("tr-TR", { day: "numeric", month: "short", timeZone: "UTC" }).format(chartDays[index].date));

  return <div className="admin-content">
    <div className="admin-page-heading">
      <div><div className="eyebrow"><span className="eyebrow-dot"/> WORDWISE STUDIO</div><h1>Günaydın, {user?.name?.split(" ")[0] ?? "Admin"} <span>✳</span></h1><p>Platformundaki gelişme yolculuğuna göz at.</p></div>
      <div className="heading-actions"><span className={`data-pill ${apiStats ? "live" : "preview"}`}><i/>{apiStats ? "Canlı API verisi" : "API bağlantısı yok"}</span><span className="date-range-label">◷ &nbsp;Son 30 gün</span></div>
    </div>
    {!apiStats && <div className="preview-banner warning"><span>ⓘ</span><div><b>Dashboard verileri alınamadı.</b><small>API bağlantısını kontrol et. Ekrandaki değerler sıfırdır; örnek veri değildir.</small></div></div>}

    <section className="stat-grid" aria-label="Platform istatistikleri">
      <article className="stat-card"><div className="stat-card-top"><span>Toplam kelime</span><i className="stat-icon green">Aa</i></div><strong>{number.format(stats.totalWords)}</strong><small>Tüm kelime kütüphanesi</small></article>
      <article className="stat-card"><div className="stat-card-top"><span>Toplam kullanıcı</span><i className="stat-icon lavender">♧</i></div><strong>{number.format(stats.totalUsers)}</strong><small>Tüm zamanlar</small></article>
      <article className="stat-card"><div className="stat-card-top"><span>Aktif kullanıcı</span><i className="stat-icon blue">↗</i></div><strong>{number.format(stats.activeUsersLast30Days)}</strong><small>Son 30 günde giriş yapan</small></article>
      <article className="stat-card"><div className="stat-card-top"><span>Toplam video</span><i className="stat-icon peach">▷</i></div><strong>{number.format(stats.totalVideos)}</strong><small>Video kütüphanesi</small></article>
    </section>

    <section className="dashboard-grid">
      <article className="panel activity-panel"><div className="panel-heading"><div><span className="panel-kicker">KULLANICI AKTİVİTESİ</span><h2>Günlük giriş yapan üyeler</h2></div><span className="panel-period">Son 30 gün</span></div>
        <div className="chart-summary"><strong>{number.format(stats.activeUsersLast30Days)}</strong><span>30 günde giriş yapan farklı üye</span></div>
        <div className="activity-chart" role="img" aria-label="Son 30 günde günlük giriş yapan farklı üye sayısı grafiği">
          <div className="activity-chart-y">{axisLabels.map((label, index) => <span key={index}>{label}</span>)}</div>
          <div className="activity-chart-grid"><i/><i/><i/><i/></div>
          <svg viewBox="0 0 660 200" preserveAspectRatio="none" aria-hidden="true"><defs><linearGradient id="usersFill" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stopColor="#bade72" stopOpacity=".38"/><stop offset="100%" stopColor="#bade72" stopOpacity="0"/></linearGradient></defs><path d={areaPath} fill="url(#usersFill)"/><path d={linePath} fill="none" stroke="#839d53" strokeWidth="3" vectorEffect="non-scaling-stroke" strokeLinecap="round" strokeLinejoin="round"/></svg>
          <div className="activity-chart-labels">{chartLabels.map((label, index) => <span key={`${index}-${label}`}>{label}</span>)}</div>
        </div>
        <div className="activity-legend"><i/> Günlük giriş yapan farklı üyeler</div>
      </article>

      <article className="panel admin-summary-panel"><div className="panel-heading"><div><span className="panel-kicker">SON 24 SAAT</span><h2>Yeni hareketler</h2></div><span className="panel-icon">✳</span></div>
        <div className="recent-summary">
          <article><span>Eklenen kelime</span><strong>{number.format(stats.wordsAddedLast24Hours)}</strong></article>
          <article><span>Eklenen video</span><strong>{number.format(stats.videosAddedLast24Hours)}</strong></article>
          <article><span>Açılan oturum</span><strong>{number.format(stats.sessionsOpenedLast24Hours)}</strong></article>
          <article><span>Kayıt olan kullanıcı</span><strong>{number.format(stats.usersRegisteredLast24Hours)}</strong></article>
        </div>
      </article>
    </section>

    <section className="dashboard-grid lower-grid">
      <article className="panel popular-panel"><div className="panel-heading"><div><span className="panel-kicker">ÜYE KELİME DEFTERLERİ</span><h2>En çok kaydedilen kelimeler</h2></div><Link className="panel-link" href="/admin/words">Tüm kelimeler <span>→</span></Link></div>
        {stats.topSearchedWords.length === 0 ? <p className="pending-review-empty">Henüz kaydetme verisi yok.</p> : <div className="popular-list">{stats.topSearchedWords.slice(0, 5).map((word, index) => <div className="popular-row" key={word.text}><span className="popular-rank">{String(index + 1).padStart(2, "0")}</span><span className={`word-initial hue-${index % 4}`}>{word.text.slice(0, 1)}</span><div className="popular-word"><b>{word.text}</b><small>Kaydedilme sayısı</small></div><div className="popular-progress"><i style={{ width: `${Math.round((word.saveCount / Math.max(...stats.topSearchedWords.map((item) => item.saveCount), 1)) * 100)}%` }}/></div><span className="popular-count">{number.format(word.saveCount)}</span></div>)}</div>}
      </article>
      <article className="panel queue-panel"><div className="panel-heading"><div><span className="panel-kicker">YÖNETİCİ ONAYI</span><h2>Onay bekleyen içerikler</h2></div><span className="queue-count">{(pendingContent?.words.length ?? stats.pendingWordCount) + (pendingContent?.videoCandidates.length ?? stats.pendingVideoCandidates)}</span></div>
        <div className="pending-review-grid"><div><b>{pendingContent?.words.length ?? stats.pendingWordCount}</b><span>kelime</span></div><div><b>{pendingContent?.videoCandidates.length ?? stats.pendingVideoCandidates}</b><span>video adayı</span></div></div>
        {!pendingContent && <p className="pending-review-empty">Onay kuyruğu API’den alınamadı.</p>}
        {pendingContent && pendingContent.words.length + pendingContent.videoCandidates.length === 0 && <p className="pending-review-empty">Bekleyen içerik yok.</p>}
        <Link className="queue-action" href="/admin/videos">Onay kuyruğunu aç <span>↗</span></Link>
      </article>
    </section>
  </div>;
}
