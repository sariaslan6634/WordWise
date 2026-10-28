import Link from "next/link";
import LoginForm from "./login-form";
import { getSession } from "@/lib/session";
import { redirect } from "next/navigation";

export const instant = false;

export default async function LoginPage({ searchParams }: { searchParams: Promise<{ mode?: string }> }) {
  const { mode } = await searchParams;
  const { user } = await getSession();
  if (user) redirect(user.role === "Admin" ? "/admin" : "/learn");
  return (
    <main className="auth-page">
      <section className="auth-visual">
        <Link className="brand auth-brand" href="/"><span className="brand-mark">w.</span><span>wordwise</span></Link>
        <div className="auth-visual-copy"><span className="eyebrow"><span className="eyebrow-dot"/> HER KELİMENİN BİR HİKÂYESİ VAR</span><h1>Bir kelime<br />daha. Bir adım<br /><em>daha ileri.</em></h1><p>Gerçek hayatın içinden öğren. Öğrendiklerin seninle kalsın.</p></div>
        <div className="auth-quote"><span>“</span><p>Small steps every day add up to big changes.</p><small>WORDWISE NOTU · 01</small></div>
        <div className="auth-shape shape-a"/><div className="auth-shape shape-b"/>
      </section>
      <section className="auth-panel"><div className="auth-panel-inner"><Link className="back-home" href="/">← Ana sayfaya dön</Link><div className="auth-welcome"><div className="eyebrow"><span className="eyebrow-dot"/> WORDWISE&apos;A HOŞ GELDİN</div><h2>{mode === "register" ? "Yeni bir başlangıç." : "Kaldığın yerden."}</h2><p>{mode === "register" ? "Bugün ilk kelimeni birlikte öğrenelim." : "Biraz pratik, güzel bir ilerleme."}</p></div><LoginForm initialMode={mode === "register" ? "register" : "login"}/><p className="auth-footnote">Devam ederek <a href="#terms">Kullanım Koşulları</a> ve <a href="#privacy">Gizlilik Politikası</a>&apos;nı kabul etmiş olursun.</p><div className="auth-panel-footer"><span>© 2026 WordWise</span><span>Her gün biraz daha. <b>✳</b></span></div></div></section>
    </main>
  );
}
