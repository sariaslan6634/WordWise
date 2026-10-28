import { NextResponse } from "next/server";
import { apiBaseUrl } from "@/lib/server-api";

export async function POST(request: Request) {
  const body = await request.json().catch(() => null);
  if (!body || typeof body.email !== "string" || typeof body.name !== "string" || typeof body.password !== "string" || typeof body.confirmPassword !== "string") {
    return NextResponse.json({ message: "Tüm alanları doldur." }, { status: 400 });
  }
  try {
    const response = await fetch(`${apiBaseUrl}/api/Auths/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      cache: "no-store",
    });
    const result = await response.json().catch(() => null);
    if (!response.ok || !result?.success) {
      const detail = Array.isArray(result?.errors) ? result.errors.join(" ") : "";
      return NextResponse.json({ message: [result?.message, detail].filter(Boolean).join(" ") || "Kayıt oluşturulamadı." }, { status: response.status || 502 });
    }
    return NextResponse.json({ message: "Hesabın oluşturuldu. Giriş yapmadan önce e-posta adresini onayla." }, { status: 201 });
  } catch {
    return NextResponse.json({ message: "WordWise API'sine ulaşılamadı. API'nin çalıştığını kontrol et." }, { status: 502 });
  }
}
