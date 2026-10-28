import "server-only";

import { cookies } from "next/headers";

export const sessionCookieName = "wordwise_session";

export interface SessionUser {
  name: string;
  email: string;
  role: string;
}

export async function getSession() {
  const token = (await cookies()).get(sessionCookieName)?.value;
  if (!token) return { token: undefined, user: undefined };

  try {
    const payload = token.split(".")[1];
    const claims = JSON.parse(Buffer.from(payload, "base64url").toString("utf8")) as Record<string, string>;
    const role = claims.role ?? claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? "";
    return {
      token,
      user: {
        name: claims.name ?? claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ?? "WordWise üyesi",
        email: claims.email ?? claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] ?? "",
        role,
      } satisfies SessionUser,
    };
  } catch {
    return { token: undefined, user: undefined };
  }
}
