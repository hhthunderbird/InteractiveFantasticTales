import { Storage } from '@google-cloud/storage';

const storage = new Storage();
const bucketName = process.env.STORAGE_BUCKET || 'interactive-tales-2026.appspot.com';
const bucket = storage.bucket(bucketName);

const URL_TTL_MS = 15 * 60 * 1000; // 15 minutes

export async function generateSignedUrl(
  storyId: string,
  assetPath: string,
  expiresMs: number = URL_TTL_MS
): Promise<{ url: string; expiresAt: number }> {
  const file = bucket.file(`stories/${storyId}/${assetPath}`);

  const [exists] = await file.exists();
  if (!exists) {
    throw new Error(`Asset not found: stories/${storyId}/${assetPath}`);
  }

  const now = Date.now();
  const expiresAt = now + expiresMs;

  const [url] = await file.getSignedUrl({
    version: 'v4',
    action: 'read',
    expires: expiresAt,
  });

  return { url, expiresAt };
}

export async function generateSignedUrls(
  storyId: string,
  assetPaths: string[],
  expiresMs: number = URL_TTL_MS
): Promise<Array<{ assetPath: string; signedUrl: string; expiresAt: number }>> {
  const results = await Promise.allSettled(
    assetPaths.map(async (assetPath) => {
      const { url, expiresAt } = await generateSignedUrl(storyId, assetPath, expiresMs);
      return { assetPath, signedUrl: url, expiresAt, success: true };
    })
  );

  return results.map((result, index) => {
    if (result.status === 'fulfilled') {
      return result.value;
    }
    return {
      assetPath: assetPaths[index],
      signedUrl: '',
      expiresAt: 0,
      success: false,
      error: (result.reason as Error).message,
    };
  });
}

export async function generateThumbnailSignedUrl(
  storyId: string,
  size: '128' | '512',
  expiresMs: number = URL_TTL_MS * 60 // thumbnails: 15h TTL
): Promise<{ url: string; expiresAt: number }> {
  return generateSignedUrl(storyId, `thumbnails/cover_${size}.webp`, expiresMs);
}
