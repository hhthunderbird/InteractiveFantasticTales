import { useState, useCallback } from 'react';

export function useAuth() {
  const [user] = useState<any>({
    uid: 'shared-author',
    displayName: 'Autor Principal',
    email: 'autor@interactivetales.com',
  });
  const [loading] = useState(false);
  const [error] = useState<string | null>(null);

  const login = useCallback(async () => {}, []);
  const logout = useCallback(async () => {}, []);
  const clearError = useCallback(() => {}, []);

  return { user, loading, error, login, logout, clearError };
}

