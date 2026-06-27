import { ref, uploadBytesResumable, getDownloadURL } from 'firebase/storage';
import { getStorageInstance } from '../lib/firebase';

export async function uploadAsset(
  storyId: string,
  file: File,
  onProgress?: (pct: number) => void,
): Promise<{ path: string; url: string }> {
  const storage = getStorageInstance();
  const filePath = `stories/${storyId}/assets/${file.name}`;
  const storageRef = ref(storage, filePath);
  const task = uploadBytesResumable(storageRef, file);

  return new Promise((resolve, reject) => {
    task.on(
      'state_changed',
      (snap) => {
        const pct = (snap.bytesTransferred / snap.totalBytes) * 100;
        onProgress?.(pct);
      },
      reject,
      async () => {
        try {
          const url = await getDownloadURL(task.snapshot.ref);
          resolve({ path: filePath, url });
        } catch (e) {
          reject(e);
        }
      },
    );
  });
}
