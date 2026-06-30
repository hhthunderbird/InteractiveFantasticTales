using System;
using InteractiveFantasticTales.Core;
using UnityEngine;

namespace InteractiveFantasticTales.Services
{
    /// <summary>
    /// Download cost management and budget protection.
    /// Implements audit C8 mitigations: CDN awareness, download throttling,
    /// budget alerts, and cost-conscious download strategies.
    /// </summary>
    public static class DownloadThrottleService
    {
        // Budget limits (configurable per environment)
        private static long _dailyEgressBudgetBytes = 100_000_000_000; // 100 GB/day
        private static long _monthlyEgressBudgetBytes = 2_000_000_000_000; // 2 TB/month
        private static long _perUserDailyLimitBytes = 500_000_000; // 500 MB/user/day

        // Tracking
        private static long _todayEgressBytes;
        private static long _monthlyEgressBytes;
        private static string _todayDate;
        private static string _currentMonth;

        // CDN base URL (populated from Remote Config in production)
        public static string CdnBaseUrl { get; set; } = "https://cdn.interactivefantastictales.com";

        /// <summary>
        /// Checks whether a download of the given size is allowed under current budget.
        /// </summary>
        public static bool CanDownload(long sizeBytes, string userId)
        {
            CheckDateRollover();

            // Global daily budget check
            if (_todayEgressBytes + sizeBytes > _dailyEgressBudgetBytes)
            {
                Debug.LogWarning($"[DownloadThrottle] Daily egress budget exceeded! {_todayEgressBytes}/{_dailyEgressBudgetBytes} bytes");
                return false;
            }

            // Global monthly budget check
            if (_monthlyEgressBytes + sizeBytes > _monthlyEgressBudgetBytes)
            {
                Debug.LogWarning($"[DownloadThrottle] Monthly egress budget exceeded! {_monthlyEgressBytes}/{_monthlyEgressBudgetBytes} bytes");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Records a download for budget tracking.
        /// </summary>
        public static void RecordDownload(long sizeBytes)
        {
            CheckDateRollover();
            _todayEgressBytes += sizeBytes;
            _monthlyEgressBytes += sizeBytes;

            // Persist to PlayerPrefs (approximate — server is authoritative)
            SecureStorage.ObfuscatedPrefs.SetString("dl_today", _todayEgressBytes.ToString());
            SecureStorage.ObfuscatedPrefs.SetString("dl_month", _monthlyEgressBytes.ToString());

            // Log for monitoring
            if (_todayEgressBytes > _dailyEgressBudgetBytes * 0.8)
                Debug.LogWarning($"[DownloadThrottle] WARNING: 80%+ of daily egress budget used ({_todayEgressBytes / 1_000_000_000f:F1} GB)");
        }

        /// <summary>
        /// Returns the CDN URL for an asset. Uses CDN in production, direct Cloud Storage in dev.
        /// </summary>
        public static string GetAssetDownloadUrl(string storyId, string version, string assetPath)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return $"https://storage.googleapis.com/ift-stories/{storyId}/v{version}/{assetPath}";
#else
            return $"{CdnBaseUrl}/stories/{storyId}/v{version}/{assetPath}";
#endif
        }

        /// <summary>
        /// Determines download strategy: full vs lazy vs wifi-only.
        /// </summary>
        public enum DownloadStrategy { Full, Lazy, WiFiOnly }

        public static DownloadStrategy GetDownloadStrategy(long totalStorySizeBytes, long freeSpaceBytes)
        {
            // WiFi check
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
            {
                if (totalStorySizeBytes > 50_000_000) // >50 MB on mobile data
                    return DownloadStrategy.WiFiOnly;
            }

            // No internet
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return DownloadStrategy.Lazy; // Only cached assets

            // Space check
            if (freeSpaceBytes < totalStorySizeBytes * 2) // Need 2x for extraction
                return DownloadStrategy.Lazy;

            return DownloadStrategy.Full;
        }

        /// <summary>
        /// Returns a user-friendly warning message based on download size and network.
        /// </summary>
        public static string GetDownloadWarning(long sizeBytes)
        {
            var sizeMb = sizeBytes / 1_000_000f;

            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork
                && sizeBytes > 20_000_000)
                return $"Este download usará ~{sizeMb:F0} MB dos seus dados móveis. Recomendamos Wi-Fi.";

            if (sizeBytes > 200_000_000)
                return $"Download grande (~{sizeMb:F0} MB). Certifique-se de ter espaço e conexão estável.";

            return null; // No warning needed
        }

        /// <summary>
        /// Checks if we should block downloads for cost reasons.
        /// Returns a user-facing message if blocked, null if allowed.
        /// </summary>
        public static string CheckDownloadAllowed(long sizeBytes)
        {
            if (_todayEgressBytes + sizeBytes > _dailyEgressBudgetBytes * 0.95)
                return "Serviço temporariamente sobrecarregado. Tente novamente mais tarde.";

            if (Application.internetReachability == NetworkReachability.NotReachable)
                return "Sem conexão com a internet. Conecte-se para baixar novas histórias.";

            return null; // Allowed
        }

        private static void CheckDateRollover()
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var month = DateTime.UtcNow.ToString("yyyy-MM");

            if (_todayDate != today)
            {
                _todayDate = today;
                _todayEgressBytes = 0;
            }

            if (_currentMonth != month)
            {
                _currentMonth = month;
                _monthlyEgressBytes = 0;
            }
        }

        /// <summary>
        /// Resets all tracking (for testing / GDPR deletion).
        /// </summary>
        public static void ResetTracking()
        {
            _todayEgressBytes = 0;
            _monthlyEgressBytes = 0;
            _todayDate = null;
            _currentMonth = null;
        }
    }
}
