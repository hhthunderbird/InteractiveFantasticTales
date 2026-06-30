using System;
using UnityEngine;

namespace InteractiveFantasticTales.Core
{
    /// <summary>
    /// Age gate and parental control system.
    /// Shows on first launch only. Stores birth year securely.
    /// Determines access level: Child (U13), Teen (13-17), Adult (18+).
    /// </summary>
    public static class AgeGate
    {
        private const string PrefKeyBirthYear = "agegate_birth_year";
        private const string PrefKeyAgeGateDone = "agegate_completed";
        private const string PrefKeyPurchasePin = "agegate_purchase_pin";

        public enum AccessLevel { Unknown, Child, Teen, Adult }

        public static AccessLevel CurrentLevel { get; private set; } = AccessLevel.Unknown;
        public static int BirthYear { get; private set; }
        public static int CurrentAge => DateTime.Now.Year - BirthYear;
        public static bool HasCompletedAgeGate => SecureStorage.ObfuscatedPrefs.GetInt(PrefKeyAgeGateDone, 0) == 1;

        /// <summary>
        /// Call once on app startup. Returns true if age gate needs to be shown.
        /// </summary>
        public static bool NeedsAgeGate()
        {
            if (HasCompletedAgeGate)
            {
                BirthYear = SecureStorage.ObfuscatedPrefs.GetInt(PrefKeyBirthYear, 0);
                if (BirthYear > 1900 && BirthYear <= DateTime.Now.Year)
                {
                    CurrentLevel = CalculateLevel(BirthYear);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Sets the user's birth year and persists it. Call from age gate UI.
        /// Returns the calculated access level.
        /// </summary>
        public static AccessLevel SetBirthYear(int year)
        {
            BirthYear = Mathf.Clamp(year, 1900, DateTime.Now.Year);
            CurrentLevel = CalculateLevel(BirthYear);

            SecureStorage.ObfuscatedPrefs.SetInt(PrefKeyBirthYear, BirthYear);
            SecureStorage.ObfuscatedPrefs.SetInt(PrefKeyAgeGateDone, 1);
            SecureStorage.ObfuscatedPrefs.Save();

            Debug.Log($"[AgeGate] Birth year set to {BirthYear}, access level: {CurrentLevel}");
            return CurrentLevel;
        }

        /// <summary>
        /// Removes age gate data (for GDPR data deletion).
        /// </summary>
        public static void ClearAgeGateData()
        {
            CurrentLevel = AccessLevel.Unknown;
            BirthYear = 0;
            SecureStorage.ObfuscatedPrefs.DeleteKey(PrefKeyBirthYear);
            SecureStorage.ObfuscatedPrefs.DeleteKey(PrefKeyAgeGateDone);
            SecureStorage.ObfuscatedPrefs.DeleteKey(PrefKeyPurchasePin);
            SecureStorage.ObfuscatedPrefs.Save();
        }

        public static bool IsChild => CurrentLevel == AccessLevel.Child;
        public static bool IsTeen => CurrentLevel == AccessLevel.Teen;
        public static bool IsAdult => CurrentLevel == AccessLevel.Adult;

        /// <summary>
        /// Analytics enabled? Blocked for children (COPPA); opt-in for teens.
        /// </summary>
        public static bool AnalyticsAllowed => CurrentLevel >= AccessLevel.Teen;

        /// <summary>
        /// Cloud saves allowed? Blocked for children under COPPA.
        /// </summary>
        public static bool CloudSaveAllowed => CurrentLevel >= AccessLevel.Teen;

        /// <summary>
        /// IAP allowed without parental PIN? Teens need PIN; Children blocked entirely.
        /// </summary>
        public static bool PurchaseAllowed => CurrentLevel >= AccessLevel.Teen;

        /// <summary>
        /// Does the current session require a parental PIN for purchases?
        /// </summary>
        public static bool RequiresPurchasePin => CurrentLevel == AccessLevel.Teen;

        // --- Purchase PIN ---
        public static bool HasPurchasePin => SecureStorage.ObfuscatedPrefs.GetInt(PrefKeyPurchasePin, 0) == 1;

        public static void SetParentalPin(string pin)
        {
            if (pin.Length != 4 || !int.TryParse(pin, out _)) return;
            var hash = ComputeSimpleHash(pin);
            SecureStorage.ObfuscatedPrefs.SetString(PrefKeyPurchasePin, hash);
            SecureStorage.ObfuscatedPrefs.SetInt(PrefKeyPurchasePin, 1);
            SecureStorage.ObfuscatedPrefs.Save();
        }

        public static bool ValidatePin(string pin)
        {
            if (!HasPurchasePin) return true; // No PIN set, allow
            var storedHash = SecureStorage.ObfuscatedPrefs.GetString(PrefKeyPurchasePin, "");
            var inputHash = ComputeSimpleHash(pin);
            return storedHash == inputHash;
        }

        private static string ComputeSimpleHash(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input + "IFT_ParentalPin_2026");
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private static AccessLevel CalculateLevel(int birthYear)
        {
            var age = DateTime.Now.Year - birthYear;
            if (age < 13) return AccessLevel.Child;
            if (age < 18) return AccessLevel.Teen;
            return AccessLevel.Adult;
        }
    }
}
