import { Router, Response } from 'express';
import { authMiddleware, AuthenticatedRequest } from '../middleware/auth';
import { hasEntitlement, isStoryFree } from '../services/firestore';
import { generateSignedUrl, generateSignedUrls, generateThumbnailSignedUrl } from '../services/storage';

const router = Router();

router.post('/:storyId/asset-url', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId } = req.params;
    const { assetPath } = req.body;
    const userId = req.userId!;

    if (!assetPath) {
      res.status(400).json({ success: false, error: 'Missing assetPath' });
      return;
    }

    const free = await isStoryFree(storyId);
    if (!free) {
      const hasAccess = await hasEntitlement(userId, storyId);
      if (!hasAccess) {
        res.status(403).json({ success: false, error: 'Not entitled to this story' });
        return;
      }
    }

    const { url, expiresAt } = await generateSignedUrl(storyId, assetPath);

    res.json({
      success: true,
      signedUrl: url,
      assetPath,
      expiresAt,
    });
  } catch (err: any) {
    console.error(`[Stories] Asset URL error (${req.params.storyId}):`, err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

router.post('/:storyId/asset-urls/batch', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId } = req.params;
    const { paths } = req.body;
    const userId = req.userId!;

    if (!paths || !Array.isArray(paths) || paths.length === 0) {
      res.status(400).json({ success: false, error: 'Missing or empty paths array' });
      return;
    }

    if (paths.length > 50) {
      res.status(400).json({ success: false, error: 'Maximum 50 assets per batch request' });
      return;
    }

    const free = await isStoryFree(storyId);
    if (!free) {
      const hasAccess = await hasEntitlement(userId, storyId);
      if (!hasAccess) {
        res.status(403).json({ success: false, error: 'Not entitled to this story' });
        return;
      }
    }

    const urls = await generateSignedUrls(storyId, paths);

    res.json({ success: true, urls });
  } catch (err: any) {
    console.error(`[Stories] Batch URL error (${req.params.storyId}):`, err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

router.get('/:storyId/entitlement', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId } = req.params;
    const userId = req.userId!;

    const free = await isStoryFree(storyId);
    if (free) {
      res.json({ success: true, hasEntitlement: true });
      return;
    }

    const entitled = await hasEntitlement(userId, storyId);
    res.json({ success: true, hasEntitlement: entitled });
  } catch (err: any) {
    console.error(`[Stories] Entitlement check error (${req.params.storyId}):`, err);
    res.status(500).json({ success: false, hasEntitlement: false, error: err.message || 'Internal error' });
  }
});

router.get('/:storyId/thumbnail/:size', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId, size } = req.params;

    if (size !== '128' && size !== '512') {
      res.status(400).json({ success: false, error: 'Size must be 128 or 512' });
      return;
    }

    const { url, expiresAt } = await generateThumbnailSignedUrl(storyId, size as '128' | '512');

    res.json({ success: true, signedUrl: url, expiresAt });
  } catch (err: any) {
    console.error(`[Stories] Thumbnail error (${req.params.storyId}):`, err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

export { router as storiesRouter };
