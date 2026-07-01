import { Router, Response } from 'express';
import * as admin from 'firebase-admin';
import { FieldValue } from 'firebase-admin/firestore';
import { authMiddleware, AuthenticatedRequest } from '../middleware/auth';

const router = Router();

function getDb() {
  return admin.firestore();
}

router.post('/', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId, score, review } = req.body;
    const userId = req.userId!;

    if (!storyId || score == null) {
      res.status(400).json({ success: false, error: 'storyId and score are required' });
      return;
    }

    if (typeof score !== 'number' || score < 1 || score > 5 || !Number.isInteger(score)) {
      res.status(400).json({ success: false, error: 'score must be an integer between 1 and 5' });
      return;
    }

    if (review != null) {
      if (typeof review !== 'string') {
        res.status(400).json({ success: false, error: 'review must be a string' });
        return;
      }
      if (review.length > 500) {
        res.status(400).json({ success: false, error: 'review must be at most 500 characters' });
        return;
      }
    }

    const db = getDb();
    const storyRef = db.collection('stories').doc(storyId);
    const storyDoc = await storyRef.get();
    if (!storyDoc.exists) {
      res.status(404).json({ success: false, error: 'Story not found' });
      return;
    }

    const storyData = storyDoc.data()!;
    if (!storyData.metadata?.isPublished) {
      res.status(403).json({ success: false, error: 'Cannot rate unpublished stories' });
      return;
    }

    const ratingRef = storyRef.collection('ratings').doc(userId);
    await ratingRef.set({
      userId,
      score,
      review: review || '',
      createdAt: FieldValue.serverTimestamp(),
    });

    const allRatings = await storyRef.collection('ratings').get();
    let totalScore = 0;
    let count = 0;

    allRatings.forEach(doc => {
      const data = doc.data();
      if (data?.score != null) {
        totalScore += data.score;
        count++;
      }
    });

    const averageRating = count > 0
      ? Math.round((totalScore / count) * 10) / 10
      : 0;

    await storyRef.update({
      'stats.averageRating': averageRating,
      'stats.ratingCount': count,
    });

    console.log(`[Ratings] ${userId} rated ${storyId}: ${score}★ (avg now ${averageRating}, ${count} ratings)`);

    res.json({
      success: true,
      rating: { storyId, userId, score, review },
      stats: { averageRating, ratingCount: count },
    });
  } catch (err: any) {
    console.error('[Ratings] Error:', err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

router.get('/:storyId/mine', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId } = req.params;
    const userId = req.userId!;

    const db = getDb();
    const ratingDoc = await db
      .collection('stories').doc(storyId)
      .collection('ratings').doc(userId)
      .get();

    if (!ratingDoc.exists) {
      res.json({ success: true, hasRated: false, rating: null });
      return;
    }

    const data = ratingDoc.data()!;
    res.json({
      success: true,
      hasRated: true,
      rating: {
        score: data.score,
        review: data.review || '',
        createdAt: data.createdAt?.toDate?.()?.toISOString() || null,
      },
    });
  } catch (err: any) {
    console.error('[Ratings] My rating error:', err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

export { router as ratingsRouter };
