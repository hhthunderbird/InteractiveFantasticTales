using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace InteractiveFantasticTales.Core
{
    /// <summary>
    /// Anti-tampering storage layer. Encrypts data with AES-256 + HMAC integrity check.
    /// Device-derived key prevents save file sharing between devices.
    /// Corrupted/tampered data is detected and discarded.
    /// </summary>
    public static class SecureStorage
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int SaltSize = 32;
        private const int HmacSize = 32;
        private const int Iterations = 10000;

        private static readonly byte[] AppSalt = Encoding.UTF8.GetBytes("IFT_SecureSalt_v1_2026");

        private static byte[] _cachedKey;
        private static string _cachedDeviceId;

        /// <summary>
        /// Returns a device-derived AES key. Stable across app launches but unique per device.
        /// </summary>
        private static byte[] GetDeviceKey()
        {
            var deviceId = SystemInfo.deviceUniqueIdentifier;
            if (_cachedKey != null && _cachedDeviceId == deviceId)
                return _cachedKey;

            using (var derive = new Rfc2898DeriveBytes(deviceId, AppSalt, Iterations, HashAlgorithmName.SHA256))
            {
                _cachedKey = derive.GetBytes(KeySize / 8);
                _cachedDeviceId = deviceId;
                return _cachedKey;
            }
        }

        /// <summary>
        /// Encrypts plain text and returns base64-encoded ciphertext with HMAC appended.
        /// Format: [16 bytes IV][ciphertext][32 bytes HMAC] → Base64
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var key = GetDeviceKey();

            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(key, iv))
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    var encrypted = ms.ToArray();

                    // Append HMAC-SHA256 of the encrypted data (including IV)
                    using (var hmac = new HMACSHA256(key))
                    {
                        var hash = hmac.ComputeHash(encrypted);
                        var combined = new byte[encrypted.Length + hash.Length];
                        Buffer.BlockCopy(encrypted, 0, combined, 0, encrypted.Length);
                        Buffer.BlockCopy(hash, 0, combined, encrypted.Length, hash.Length);
                        return Convert.ToBase64String(combined);
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts a base64-encoded ciphertext. Returns null if integrity check fails (tampered).
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";

            try
            {
                var combined = Convert.FromBase64String(cipherText);
                if (combined.Length < 16 + HmacSize) return null;

                var key = GetDeviceKey();

                // Verify HMAC
                var encryptedLength = combined.Length - HmacSize;
                var encrypted = new byte[encryptedLength];
                var storedHmac = new byte[HmacSize];
                Buffer.BlockCopy(combined, 0, encrypted, 0, encryptedLength);
                Buffer.BlockCopy(combined, encryptedLength, storedHmac, 0, HmacSize);

                using (var hmac = new HMACSHA256(key))
                {
                    var computedHmac = hmac.ComputeHash(encrypted);
                    if (!ConstantTimeEquals(computedHmac, storedHmac))
                    {
                        Debug.LogWarning("[SecureStorage] Integrity check FAILED — data tampered or corrupted");
                        return null;
                    }
                }

                // Decrypt
                var iv = new byte[16];
                Buffer.BlockCopy(encrypted, 0, iv, 0, 16);
                var cipherBytes = new byte[encryptedLength - 16];
                Buffer.BlockCopy(encrypted, 16, cipherBytes, 0, cipherBytes.Length);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(key, iv))
                    using (var ms = new MemoryStream(cipherBytes))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SecureStorage] Decryption failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves encrypted data to a file. Returns true on success.
        /// </summary>
        public static bool SaveToFile(string path, string data)
        {
            try
            {
                var encrypted = Encrypt(data);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, encrypted);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureStorage] Save failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads and decrypts data from a file. Returns null if file missing or integrity fails.
        /// </summary>
        public static string LoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var encrypted = File.ReadAllText(path);
                return Decrypt(encrypted);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SecureStorage] Load failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes the file at path. Returns true if deleted or already absent.
        /// </summary>
        public static bool DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Obfuscated PlayerPrefs wrapper. Keys are hashed; values are optionally encrypted.
        /// Protects against casual tampering via file explorer.
        /// </summary>
        public static class ObfuscatedPrefs
        {
            private static string HashKey(string key)
            {
                using (var sha = SHA256.Create())
                {
                    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key + "_ift_obfuscate"));
                    return Convert.ToBase64String(hash).Substring(0, 16);
                }
            }

            public static void SetInt(string key, int value)
            {
                var obfuscated = value ^ 0x5A5A5A5A;
                PlayerPrefs.SetInt(HashKey(key), obfuscated);
            }

            public static int GetInt(string key, int defaultValue = 0)
            {
                var obfuscated = PlayerPrefs.GetInt(HashKey(key), defaultValue ^ 0x5A5A5A5A);
                return obfuscated ^ 0x5A5A5A5A;
            }

            public static void SetFloat(string key, float value)
            {
                var bytes = BitConverter.GetBytes(value);
                for (int i = 0; i < bytes.Length; i++) bytes[i] ^= 0x5A;
                PlayerPrefs.SetString(HashKey(key), Convert.ToBase64String(bytes));
            }

            public static float GetFloat(string key, float defaultValue = 0f)
            {
                var stored = PlayerPrefs.GetString(HashKey(key), "");
                if (string.IsNullOrEmpty(stored)) return defaultValue;
                try
                {
                    var bytes = Convert.FromBase64String(stored);
                    for (int i = 0; i < bytes.Length; i++) bytes[i] ^= 0x5A;
                    return BitConverter.ToSingle(bytes, 0);
                }
                catch { return defaultValue; }
            }

            public static void SetString(string key, string value)
            {
                PlayerPrefs.SetString(HashKey(key), Encrypt(value));
            }

            public static string GetString(string key, string defaultValue = "")
            {
                var encrypted = PlayerPrefs.GetString(HashKey(key), "");
                if (string.IsNullOrEmpty(encrypted)) return defaultValue;
                var decrypted = Decrypt(encrypted);
                return decrypted ?? defaultValue;
            }

            public static void DeleteKey(string key)
            {
                PlayerPrefs.DeleteKey(HashKey(key));
            }

            public static void Save() => PlayerPrefs.Save();
        }

        /// <summary>
        /// Constant-time byte array comparison to prevent timing attacks on HMAC verification.
        /// </summary>
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
