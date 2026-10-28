import { NextResponse } from "next/server";
import { apiBaseUrl, readAuthResponse } from "@/lib/server-api";
import { sessionCookieName } from "@/lib/session";

export async function POST(request: Request) {
  const body = await request.json().catch(() => null);
  if (!body || typeof body.email !== "string" || typeof body.password !== "string") {
    return NextResponse.json({ message: "E-posta ve şifre gerekli." }, { status: 400 });
  }
  const expectedRole = body.expectedRole === "Admin" ? "Admin" : "User";

  try {
    const apiResponse = await fetch(`${apiBaseUrl}/api/Auths/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email: body.email, password: body.password }),
      cache: "no-store",
    });
    const raw = await apiResponse.json().catch(() => null);
    const auth = readAuthResponse(raw);
    if (!apiResponse.ok || !auth) {
      const message = raw && typeof raw.message === "string" ? raw.message : "E-posta veya şifre hatalı.";
      return NextResponse.json({ message }, { status: apiResponse.status || 502 });
    }
    if (auth.role !== expectedRole) {
      return NextResponse.json({ message: expectedRole === "Admin" ? "Bu hesap yönetici yetkisine sahip değil." : "Yönetici hesabıyla kullanıcı giriş ekranından oturum açılamaz." }, { status: 403 });
    }

    const response = NextResponse.json({ user: { name: auth.name, email: auth.email, role: auth.role } });
    response.cookies.set(sessionCookieName, auth.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      expires: new Date(auth.expiresAt),
    });
    return response;
  } catch {
    return NextResponse.json({ message: "WordWise API'sine ulaşılamadı. API'nin çalıştığını kontrol et." }, { status: 502 });
  }
}
