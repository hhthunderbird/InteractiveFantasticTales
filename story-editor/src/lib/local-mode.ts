export const isLocalMode = (): boolean =>
  import.meta.env.VITE_LOCAL_MODE === 'true' || import.meta.env.DEV;

export const LOCAL_USER_ID = 'local-dev-user';
