import Link from "next/link";
import { connection } from "next/server";
import { getAdminVideoCandidateReview } from "@/lib/server-api";
import { getSession } from "@/lib/session";
import VideoCandidatePreview from "./video-candidate-preview";

export const instant = false;
type SearchParams = Promise<{ candidateId?: string }>;
const candidateIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export default async function VideoCandidatePreviewPage({ searchParams }: { searchParams: SearchParams }) {
  await connection();
  const [{ token }, params] = await Promise.all([getSession(), searchParams]);
  const review = params.candidateId && candidateIdPattern.test(params.candidateId)
    ? await getAdminVideoCandidateReview(params.candidateId, token)
    : null;

  if (!review) return <div className="admin-content"><div className="preview-banner warning"><span>ⓘ</span><div><b>Video adayı bulunamadı.</b><small>Aday artık beklemede olmayabilir veya API yanıt vermiyor.</small></div><Link href="/admin/videos">Video adaylarına dön →</Link></div></div>;
  return <VideoCandidatePreview review={review}/>;
}
