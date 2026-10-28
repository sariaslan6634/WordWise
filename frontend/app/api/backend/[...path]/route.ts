import { NextResponse } from "next/server";
import { apiBaseUrl } from "@/lib/server-api";
import { sessionCookieName } from "@/lib/session";

type Context = { params: Promise<{ path: string[] }> };

async function forward(request: Request, context: Context) {
  const token = request.headers.get("cookie")?.split(";").map((entry) => entry.trim()).find((entry) => entry.startsWith(`${sessionCookieName}=`))?.slice(sessionCookieName.length + 1);
  if (!token) return NextResponse.json({ message: "Oturum açman gerekiyor." }, { status: 401 });

  const { path } = await context.params;
  if (path[0] !== "api" || path.some((part) => part === "." || part === "..")) {
    return NextResponse.json({ message: "Geçersiz API yolu." }, { status: 400 });
  }
  const incoming = new URL(request.url);
  const target = `${apiBaseUrl}/${path.map(encodeURIComponent).join("/")}${incoming.search}`;
  try {
    const upstream = await fetch(target, {
      method: request.method,
      headers: {
        ...(request.headers.get("content-type") ? { "Content-Type": request.headers.get("content-type")! } : {}),
        Authorization: `Bearer ${decodeURIComponent(token)}`,
      },
      ...(request.method === "GET" || request.method === "HEAD" ? {} : { body: await request.arrayBuffer() }),
      cache: "no-store",
      redirect: "manual",
    });
    const data = await upstream.arrayBuffer();
    return new NextResponse(data, { status: upstream.status, headers: { "Content-Type": upstream.headers.get("content-type") ?? "application/json" } });
  } catch {
    return NextResponse.json({ message: "WordWise API'sine ulaşılamadı." }, { status: 502 });
  }
}

export const GET = forward;
export const POST = forward;
export const PUT = forward;
export const PATCH = forward;
export const DELETE = forward;
