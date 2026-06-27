import { initializeApp, type FirebaseApp } from 'firebase/app';
import { getFirestore, connectFirestoreEmulator, type Firestore } from 'firebase/firestore';
import { getAuth, connectAuthEmulator, GoogleAuthProvider, type Auth } from 'firebase/auth';
import { getStorage, connectStorageEmulator, type FirebaseStorage } from 'firebase/storage';

let app: FirebaseApp | null = null;
let _db: Firestore | null = null;
let _auth: Auth | null = null;
let _storage: FirebaseStorage | null = null;

export const googleProvider = new GoogleAuthProvider();

function getApp(): FirebaseApp {
  if (!app) {
    const apiKey = import.meta.env.VITE_FIREBASE_API_KEY;
    const projectId = import.meta.env.VITE_FIREBASE_PROJECT_ID;

    if (!apiKey || !projectId) {
      throw new Error(
        'Firebase config missing. Set VITE_FIREBASE_API_KEY and VITE_FIREBASE_PROJECT_ID in .env',
      );
    }

    app = initializeApp({
      apiKey,
      authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
      projectId,
      storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
      messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
      appId: import.meta.env.VITE_FIREBASE_APP_ID,
    });

    if (import.meta.env.DEV && import.meta.env.VITE_USE_FIREBASE_EMULATOR === 'true') {
      try {
        connectFirestoreEmulator(getDb(), 'localhost', 8080);
        connectAuthEmulator(getAuthInstance(), 'http://localhost:9099');
        connectStorageEmulator(getStorageInstance(), 'localhost', 9199);
      } catch {
        console.warn('Firebase emulator connection failed. Using production.');
      }
    }
  }
  return app;
}

export function getDb(): Firestore {
  if (!_db) _db = getFirestore(getApp());
  return _db;
}

export function getAuthInstance(): Auth {
  if (!_auth) _auth = getAuth(getApp());
  return _auth;
}

export function getStorageInstance(): FirebaseStorage {
  if (!_storage) _storage = getStorage(getApp());
  return _storage;
}
