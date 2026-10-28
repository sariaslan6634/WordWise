"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

type Mode = "login" | "register";

export default function LoginForm({ initialMode, expectedRole = "User", allowRegistration = true }: { initialMode: Mode; expectedRole?: "User" | "Admin"; allowRegistration?: boolean }) {
  const router = useRouter();
  const [mode, setMode] = useState<Mode>(initialMode);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ kind: "error" | "success"; text: string } | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    const form = new FormData(event.currentTarget);
    const payload = Object.fromEntries(form.entries());
    try {
      const response = await fetch(`/api/auth/${mode}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ ...payload, expectedRole }) });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || "İşlem tamamlanamadı.");
      if (mode === "register") {
        setMessage({ kind: "success", text: data.message });
        setMode("login");
      } else {
        setMessage({ kind: "success", text: expectedRole === "Admin" ? "Giriş başarılı. Yönetim paneline yönlendiriliyorsun." : "Giriş başarılı. Öğrenme alanına yönlendiriliyorsun." });
        router.push(expectedRole === "Admin" ? "/admin" : "/learn");
        router.refresh();
      }
    } catch (error) {
      setMessage({ kind: "error", text: error instanceof Error ? error.message : "Bir hata oluştu." });
    } finally {
      setBusy(false);
    }
  }

  return <div className="auth-form-wrap">
    {allowRegistration && <div className="auth-tabs" role="tablist" aria-label="Hesap işlemi"><button type="button" role="tab" aria-selected={mode === "login"} className={mode === "login" ? "active" : ""} onClick={() => { setMode("login"); setMessage(null); }}>Giriş yap</button><button type="button" role="tab" aria-selected={mode === "register"} className={mode === "register" ? "active" : ""} onClick={() => { setMode("register"); setMessage(null); }}>Hesap oluştur</button></div>}
    <form className="auth-form" onSubmit={handleSubmit}>
      {mode === "register" && <label>Adın<input name="name" type="text" placeholder="Ad Soyad" autoComplete="name" required minLength={2}/></label>}
      <label>E-posta adresi<input name="email" type="email" placeholder="ornek@email.com" autoComplete="email" required/></label>
      <label>Şifre<input name="password" type="password" placeholder="En az 6 karakter" autoComplete={mode === "login" ? "current-password" : "new-password"} required minLength={6}/></label>
      {mode === "register" && <label>Şifre tekrar<input name="confirmPassword" type="password" placeholder="Şifreni tekrar gir" autoComplete="new-password" required minLength={6}/></label>}
      {message && <div className={`form-message ${message.kind}`} role="status">{message.text}</div>}
      <button className="button auth-submit" disabled={busy}>{busy ? "Bir saniye…" : mode === "login" ? "Giriş yap" : "Hesabımı oluştur"}<span>↗</span></button>
    </form>
    {mode === "login" && <div className="auth-divider"><span/>ya da<span/></div>}
    {mode === "login" && expectedRole === "User" && <button className="demo-login" type="button" onClick={() => router.push("/admin-login")}>Yönetici girişi <span>↗</span></button>}
  </div>;
}
