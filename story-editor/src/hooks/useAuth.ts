import { useState, useEffect, useCallback } from 'react';
import type { User } from 'firebase/auth';
import { isLocalMode, LOCAL_USER_ID } from '../lib/local-mode';

const fakeUser: User = {
  uid: LOCAL_USER_ID,
  email: 'dev@localhost',
  displayName: 'Desenvolvedor Local',
  emailVerified: true,
  isAnonymous: false,
  phoneNumber: null,
  photoURL: null,
  providerId: 'local',
  tenantId: null,
  providerData: [],
  metadata: { creationTime: '', lastSignInTime: '' },
  refreshToken: '',
  getIdToken: async () => '',
  getIdTokenResult: async () => ({ token: '', claims: {}, authTime: '', issuedAtTime: '', expirationTime: '', signInProvider: null, signInSecondFactor: null }),
  toJSON: () => ({}),
  delete: async () => {},
  reload: async () => {},
} as unknown as User;

export function useAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isLocalMode()) {
      setUser(fakeUser);
      setLoading(false);
      return;
    }

    let cancelled = false;
    const initFirebaseAuth = async () => {
      try {
      const { getAuthInstance } = await import('../lib/firebase');
      const { onAuthStateChanged } = await import('firebase/auth');
        const auth = getAuthInstance();
        return onAuthStateChanged(auth, (u) => {
          if (!cancelled) { setUser(u); setLoading(false); }
        });
      } catch {
        if (!cancelled) { setUser(null); setLoading(false); setError('Firebase não configurado. Use VITE_LOCAL_MODE=true.'); }
      }
    };
    const promise = initFirebaseAuth();
    return () => { cancelled = true; promise.then((unsub) => unsub?.()); };
  }, []);

  const login = useCallback(async () => {
    if (isLocalMode()) { setUser(fakeUser); return; }
    setError(null);
    try {
      const { getAuthInstance, googleProvider } = await import('../lib/firebase');
      const { signInWithPopup } = await import('firebase/auth');
      await signInWithPopup(getAuthInstance(), googleProvider);
    } catch (e: any) {
      setError(e.code === 'auth/popup-closed-by-user' ? 'Login cancelado.' : `Erro: ${e.message}`);
    }
  }, []);

  const logout = useCallback(async () => {
    if (isLocalMode()) { setUser(null); return; }
    try {
      const { getAuthInstance } = await import('../lib/firebase');
      const { signOut } = await import('firebase/auth');
      await signOut(getAuthInstance());
    } catch (e: any) { setError(`Erro: ${e.message}`); }
  }, []);

  const clearError = useCallback(() => setError(null), []);

  return { user, loading, error, login, logout, clearError };
}
