import Link from "next/link";
import { redirect } from "next/navigation";
import LoginForm from "../login/login-form";
import { getSession } from "@/lib/session";

export const instant = false;

export default async function AdminLoginPage() {
  const { user } = await getSession();
  if (user) redirect(user.role === "Admin" ? "/admin" : "/learn");

  return <main className="auth-page">
    <section className="auth-visual">
      <Link className="brand auth-brand" href="/"><span className="brand-mark">w.</span><span>wordwise</span></Link>
      <div className="auth-visual-copy"><span className="eyebrow"><span className="eyebrow-dot"/> WORDWISE STUDIO</span><h1>İçeriği<br />yönet, öğrenme<br /><em>yolunu aç.</em></h1><p>Platformun kelimelerini ve öğrenme deneyimini yönet.</p></div>
      <div className="auth-quote"><span>“</span><p>Small steps every day add up to big changes.</p><small>WORDWISE STUDIO</small></div>
      <div className="auth-shape shape-a"/><div className="auth-shape shape-b"/>
    </section>
    <section className="auth-panel"><div className="auth-panel-inner">
      <Link className="back-home" href="/">← Vitrine dön</Link>
      <div className="auth-welcome"><div className="eyebrow"><span className="eyebrow-dot"/> YÖNETİCİ HESABI</div><h2>Yönetim alanına giriş.</h2><p>Admin hesabınla devam et.</p></div>
      <LoginForm initialMode="login" expectedRole="Admin" allowRegistration={false} />
      <p className="auth-footnote"><Link href="/login">Üye girişine dön</Link></p>
      <div className="auth-panel-footer"><span>© 2026 WordWise</span><span>WordWise Studio <b>✳</b></span></div>
    </div></section>
  </main>;
}
