import * as admin from 'firebase-admin';
import { FieldValue, Firestore } from 'firebase-admin/firestore';

let _db: Firestore | null = null;

function db(): Firestore {
  if (!_db) {
    _db = admin.firestore();
  }
  return _db;
}

export interface PurchaseRecord {
  userId: string;
  storyId: string;
  type: string;
  store: string;
  storeToken: string;
  storeOrderId: string;
  price: number;
  currency: string;
  status: 'validated' | 'delivered' | 'refunded' | 'needs_investigation';
  consumed: boolean;
  purchasedAt: admin.firestore.Timestamp;
  expiresAt: admin.firestore.Timestamp | null;
  deliveredAt: admin.firestore.Timestamp | null;
}

export async function createPurchaseRecord(record: Omit<PurchaseRecord, 'purchasedAt'>): Promise<string> {
  const ref = db().collection('purchases').doc();
  await ref.set({
    ...record,
    purchasedAt: FieldValue.serverTimestamp(),
  });
  return ref.id;
}

export async function findPurchaseByToken(storeToken: string, store: string): Promise<PurchaseRecord | null> {
  const snapshot = await db().collection('purchases')
    .where('storeToken', '==', storeToken)
    .where('store', '==', store)
    .limit(1)
    .get();

  if (snapshot.empty) return null;
  return snapshot.docs[0].data() as PurchaseRecord;
}

export async function findPurchaseByOrderId(storeOrderId: string): Promise<PurchaseRecord | null> {
  const snapshot = await db().collection('purchases')
    .where('storeOrderId', '==', storeOrderId)
    .limit(1)
    .get();

  if (snapshot.empty) return null;
  return snapshot.docs[0].data() as PurchaseRecord;
}

export async function grantEntitlement(userId: string, storyId: string): Promise<void> {
  const userRef = db().collection('users').doc(userId);
  const progressRef = userRef.collection('storyProgress').doc(storyId);

  await db().runTransaction(async (transaction) => {
    transaction.update(userRef, {
      ownedStoryIds: FieldValue.arrayUnion(storyId),
    });

    transaction.set(progressRef, {
      owned: true,
      purchasedAt: FieldValue.serverTimestamp(),
      purchaseType: 'iap',
    }, { merge: true });
  });
}

export async function revokeEntitlement(userId: string, storyId: string): Promise<void> {
  const userRef = db().collection('users').doc(userId);
  const progressRef = userRef.collection('storyProgress').doc(storyId);

  await db().runTransaction(async (transaction) => {
    transaction.update(userRef, {
      ownedStoryIds: FieldValue.arrayRemove(storyId),
    });

    transaction.update(progressRef, {
      owned: false,
    });
  });
}

export async function getEntitlements(userId: string): Promise<Array<{ storyId: string; owned: boolean }>> {
  const snapshot = await db().collection('users').doc(userId)
    .collection('storyProgress')
    .where('owned', '==', true)
    .get();

  return snapshot.docs.map((doc) => ({
    storyId: doc.id,
    owned: true,
  }));
}

export async function hasEntitlement(userId: string, storyId: string): Promise<boolean> {
  const doc = await db().collection('users').doc(userId)
    .collection('storyProgress').doc(storyId).get();

  if (!doc.exists) return false;
  const data = doc.data();
  return data?.owned === true;
}

export async function isStoryFree(storyId: string): Promise<boolean> {
  const doc = await db().collection('stories').doc(storyId).get();
  if (!doc.exists) return false;
  const data = doc.data();
  const pricing = data?.pricing as { type?: string } | undefined;
  return pricing?.type === 'free';
}

export async function markPurchaseDelivered(purchaseId: string): Promise<void> {
  await db().collection('purchases').doc(purchaseId).update({
    status: 'delivered',
    consumed: true,
    deliveredAt: FieldValue.serverTimestamp(),
  });
}
