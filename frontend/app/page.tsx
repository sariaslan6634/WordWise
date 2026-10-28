import Link from "next/link";

const steps = [
  { n: "01", title: "Bir kelime, bir an", body: "Gerçek cümlelerde ve kısa video kesitlerinde kelimeleri bağlamıyla yakala." },
  { n: "02", title: "Kendi ritminde tekrar", body: "Kaydettiğin kelimeler doğru zamanda yeniden karşına çıksın." },
  { n: "03", title: "Öğrendiklerin biriksin", body: "Mini quiz'lerle ilerlemeni gör, her gün biraz daha ileri git." },
];

export default function Home() {
  return (
    <main className="landing">
      <header className="site-header wrap">
        <Link className="brand" href="/" aria-label="WordWise ana sayfa"><span className="brand-mark">w.</span><span>wordwise</span></Link>
        <nav className="top-nav" aria-label="Ana menü"><a href="#yontem">Nasıl çalışır?</a><a href="#kesfet">Keşfet</a></nav>
        <div className="header-actions"><Link className="quiet-link" href="/login">Giriş yap</Link><Link className="button button-small" href="/login?mode=register">Ücretsiz başla <span>↗</span></Link></div>
      </header>

      <section className="hero wrap">
        <div className="hero-copy">
          <div className="eyebrow"><span className="eyebrow-dot" /> DİL ÖĞRENMEK DAHA DOĞAL OLABİLİR</div>
          <h1>Kelimeler,<br />yerine <em>otursun.</em></h1>
          <p className="hero-lede">Ezber listelerini bırak. İngilizceyi gerçek hayattan kısa videolar, akıllı tekrarlar ve küçük başarılarla öğren.</p>
          <div className="hero-actions"><Link className="button" href="/login?mode=register">İlk kelimene başla <span>↗</span></Link><a className="watch-link" href="#yontem"><span className="play">▶</span> Nasıl çalıştığını gör</a></div>
          <div className="social-proof"><div className="avatar-stack"><span>AY</span><span>MK</span><span>ED</span><span>+</span></div><div><strong>Her gün küçük bir adım.</strong><small>Kelime dağarcığın zamanla büyür.</small></div></div>
        </div>

        <div className="hero-art" id="kesfet" aria-label="WordWise kelime kartı önizlemesi">
          <div className="art-orbit orbit-one" /><div className="art-orbit orbit-two" />
          <div className="float-note note-left"><span className="note-icon note-purple">✳</span><span><strong>Bağlamla öğren</strong><small>Gerçek hayattan örnekler</small></span></div>
          <article className="word-card">
            <div className="word-card-top"><span className="word-label">BUGÜNÜN KELİMESİ</span><span className="level-pill">B2 · İLERİ</span></div>
            <div className="word-main"><h2>serendipity</h2><button className="sound-button" aria-label="Telaffuzu dinle">⌁</button></div>
            <div className="phonetic">/ˌser.ənˈdɪp.ə.ti/ <span>isim</span></div>
            <p className="definition">Aramadan, beklenmedik bir anda güzel veya faydalı bir şey bulma.</p>
            <div className="video-preview"><div className="video-image"><div className="sunset"/><div className="video-play">▶</div><span className="video-time">00:18</span></div><div className="video-caption"><span className="caption-dot"/> “It was pure <b>serendipity</b> that we met.”</div><div className="video-source">THE GOOD PLACE <span>·</span> SEASON 2</div></div>
            <div className="word-card-bottom"><span>Bir sonraki tekrarın <b>yarın</b></span><button aria-label="Kelimeyi kaydet">♡</button></div>
          </article>
          <div className="float-note note-right"><span className="streak-icon">✦</span><span><strong>7 gün üst üste</strong><small>İstikrarını koru!</small></span><span className="streak-arrow">↗</span></div>
          <span className="decor-star star-a">✳</span><span className="decor-star star-b">✦</span>
        </div>
      </section>

      <section className="trust-strip wrap"><span>İNGİLİZCE ÖĞRENMEK ARTIK DAHA</span><b>anlamlı</b><i>·</i><b>akılda kalıcı</b><i>·</i><b>sana ait</b><span className="trust-spark">✳</span></section>

      <section className="method-section" id="yontem"><div className="wrap"><div className="section-heading"><div><div className="eyebrow"><span className="eyebrow-dot"/> WORDWISE YÖNTEMİ</div><h2>Öğrenme, hayatın<br />içinde <em>güzelleşir.</em></h2></div><p>Bir kelimeyi hatırlamak için onu bir yerde duymak, bir cümlede görmek ve kendi zamanında tekrar etmek yeter.</p></div><div className="steps-grid">{steps.map((step) => <article className="step-card" key={step.n}><span className="step-number">{step.n}</span><span className="step-squiggle">{step.n === "01" ? "↗" : step.n === "02" ? "↻" : "✳"}</span><h3>{step.title}</h3><p>{step.body}</p></article>)}</div></div></section>

      <section className="closing-cta wrap"><div><span className="eyebrow"><span className="eyebrow-dot"/> BUGÜN BAŞLA</span><h2>Bir kelimeyle<br /><em>başlayalım mı?</em></h2></div><Link className="button button-light" href="/login?mode=register">WordWise&apos;ı keşfet <span>↗</span></Link><span className="cta-flower">✳</span></section>

      <footer className="site-footer wrap"><Link className="brand" href="/"><span className="brand-mark">w.</span><span>wordwise</span></Link><span>Her gün biraz daha. © 2026 WordWise</span><Link href="/login">Hesabına giriş yap ↗</Link></footer>
    </main>
  );
}
