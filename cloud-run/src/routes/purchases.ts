import { Router, Response } from 'express';
import { authMiddleware, AuthenticatedRequest } from '../middleware/auth';
import {
  createPurchaseRecord,
  findPurchaseByToken,
  findPurchaseByOrderId,
  grantEntitlement,
  revokeEntitlement,
  getEntitlements,
  markPurchaseDelivered,
} from '../services/firestore';
import { validateReceipt } from '../services/iap';

const router = Router();

router.post('/verify', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId, purchaseToken, store, idempotencyKey } = req.body;
    const userId = req.userId!;

    if (!storyId || !purchaseToken || !store) {
      res.status(400).json({ success: false, error: 'Missing required fields: storyId, purchaseToken, store' });
      return;
    }

    if (store !== 'google_play' && store !== 'apple_app_store') {
      res.status(400).json({ success: false, error: 'Invalid store. Use google_play or apple_app_store' });
      return;
    }

    // Idempotency check: has this exact token been used already?
    const existingByToken = await findPurchaseByToken(purchaseToken, store);
    if (existingByToken) {
      if (existingByToken.userId !== userId) {
        console.warn(`[Purchases] Token reuse detected — original user: ${existingByToken.userId}, attempt by: ${userId}`);
        res.status(400).json({ success: false, error: 'Token already consumed' });
        return;
      }
      res.json({ success: true, alreadyOwned: true, entitlementId: existingByToken.storyId });
      return;
    }

    // Validate receipt with the store
    const validation = await validateReceipt(purchaseToken, storyId, store);

    if (!validation.isValid) {
      res.status(400).json({ success: false, error: 'Invalid receipt — store validation failed' });
      return;
    }

    // Check for duplicate order ID
    if (validation.orderId) {
      const existingByOrder = await findPurchaseByOrderId(validation.orderId);
      if (existingByOrder) {
        await grantEntitlement(userId, storyId);
        res.json({ success: true, alreadyOwned: true });
        return;
      }
    }

    // Write purchase record
    const purchaseId = await createPurchaseRecord({
      userId,
      storyId,
      type: 'iap',
      store,
      storeToken: purchaseToken,
      storeOrderId: validation.orderId,
      price: validation.price,
      currency: validation.currency,
      status: 'validated',
      consumed: false,
      expiresAt: null,
      deliveredAt: null,
    });

    // Grant entitlement in transaction
    await grantEntitlement(userId, storyId);

    // Mark as delivered
    await markPurchaseDelivered(purchaseId);

    console.log(`[Purchases] Purchase verified: user=${userId}, story=${storyId}, store=${store}`);
    res.json({ success: true, entitlementId: storyId, storyId });
  } catch (err: any) {
    console.error('[Purchases] Verify error:', err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

router.get('/restore', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const userId = req.userId!;
    const entitlements = await getEntitlements(userId);

    const result = entitlements.map(e => ({
      entitlementId: e.storyId,
      storyId: e.storyId,
      purchasedAt: new Date().toISOString(),
      expiresAt: null,
      store: 'google_play',
    }));

    res.json({ success: true, entitlements: result });
  } catch (err: any) {
    console.error('[Purchases] Restore error:', err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

router.post('/refund', authMiddleware, async (req: AuthenticatedRequest, res: Response): Promise<void> => {
  try {
    const { storyId, purchaseToken } = req.body;
    const userId = req.userId!;

    if (!storyId || !purchaseToken) {
      res.status(400).json({ success: false, error: 'Missing required fields' });
      return;
    }

    await revokeEntitlement(userId, storyId);

    console.log(`[Purchases] Refund processed: user=${userId}, story=${storyId}`);
    res.json({ success: true });
  } catch (err: any) {
    console.error('[Purchases] Refund error:', err);
    res.status(500).json({ success: false, error: err.message || 'Internal error' });
  }
});

export { router as purchasesRouter };
