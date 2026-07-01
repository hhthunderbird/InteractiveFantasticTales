import { Request, Response, NextFunction } from 'express';
import * as admin from 'firebase-admin';

export interface AuthenticatedRequest extends Request {
  userId?: string;
}

export async function authMiddleware(
  req: AuthenticatedRequest,
  res: Response,
  next: NextFunction
): Promise<void> {
  const authHeader = req.headers.authorization;
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    res.status(401).json({ error: 'Missing or invalid Authorization header' });
    return;
  }

  const idToken = authHeader.split('Bearer ')[1];
  if (!idToken) {
    res.status(401).json({ error: 'Empty token' });
    return;
  }

  try {
    const decoded = await admin.auth().verifyIdToken(idToken);
    req.userId = decoded.uid;
    next();
  } catch (err: any) {
    console.warn('[Auth] Token verification failed:', err.message);
    res.status(401).json({ error: 'Invalid or expired token' });
  }
}

export function optionalAuth(
  req: AuthenticatedRequest,
  _res: Response,
  next: NextFunction
): void {
  const authHeader = req.headers.authorization;
  if (authHeader && authHeader.startsWith('Bearer ')) {
    const idToken = authHeader.split('Bearer ')[1];
    admin.auth().verifyIdToken(idToken)
      .then(decoded => { req.userId = decoded.uid; })
      .catch(() => { /* ignore — proceed unauthenticated */ })
      .finally(() => next());
  } else {
    next();
  }
}
