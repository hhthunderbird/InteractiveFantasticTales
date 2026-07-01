import express from 'express';
import cors from 'cors';
import * as admin from 'firebase-admin';
import { authMiddleware } from './middleware/auth';
import { purchasesRouter } from './routes/purchases';
import { storiesRouter } from './routes/stories';
import { ratingsRouter } from './routes/ratings';

admin.initializeApp();
console.log('[API] Firebase Admin initialized');

const app = express();
const PORT = parseInt(process.env.PORT || '8080', 10);

app.use(cors());
app.use(express.json({ limit: '1mb' }));

app.get('/health', (_req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

app.use('/api/purchases', purchasesRouter);
app.use('/api/stories', storiesRouter);
app.use('/api/ratings', ratingsRouter);

app.use((_req, res) => {
  res.status(404).json({ error: 'Not found' });
});

app.use((err: Error, _req: express.Request, res: express.Response, _next: express.NextFunction) => {
  console.error('[API] Unhandled error:', err);
  res.status(500).json({ error: 'Internal server error' });
});

app.listen(PORT, () => {
  console.log(`[API] IFT Cloud Run listening on port ${PORT}`);
});
