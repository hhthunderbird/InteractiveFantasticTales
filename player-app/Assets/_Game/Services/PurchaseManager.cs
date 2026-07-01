using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using InteractiveFantasticTales.Core;

namespace InteractiveFantasticTales.Services
{
    public class PurchaseManager : MonoBehaviour, IDetailedStoreListener
    {
        public static PurchaseManager Instance;

        private IStoreController _storeController;
        private IExtensionProvider _extensions;

        public event Action<string, bool> OnPurchaseComplete;
        public event Action<string, string> OnPurchaseFailureOccurred;
        public event Action<bool> OnStoreInitialized;
        public event Action<List<Product>> OnProductsUpdated;

        public readonly Dictionary<string, string> ProductIds = new()
        {
            { "forest-of-doom", "com.ift.story.forest_of_doom" },
            { "mountain-of-fire", "com.ift.story.mountain_of_fire" },
            { "vampire-crypts", "com.ift.story.vampire_crypts" },
            { "city-of-thieves", "com.ift.story.city_of_thieves" },
            { "premium_monthly", "com.ift.premium.monthly" },
            { "premium_yearly", "com.ift.premium.yearly" },
        };

        public readonly Dictionary<string, string> DefaultPrices = new()
        {
            { "forest-of-doom", "R$ 4,99" },
            { "mountain-of-fire", "R$ 9,99" },
            { "vampire-crypts", "R$ 9,99" },
            { "city-of-thieves", "R$ 14,99" },
            { "premium_monthly", "R$ 14,99/mes" },
            { "premium_yearly", "R$ 99,99/ano" },
        };

        private bool _isInitialized;
        private string _pendingPurchaseStoryId;

        public bool IsInitialized => _isInitialized;
        public IStoreController StoreController => _storeController;
        public IExtensionProvider Extensions => _extensions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            if (_isInitialized) { Debug.Log("[PurchaseManager] Already initialized, skipping."); return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            InitializeMock();
#else
            InitializeReal();
#endif
        }

        private void InitializeMock()
        {
            try
            {
                Debug.Log("[PurchaseManager] Mock mode — IAP initialization simulated");
                _isInitialized = true;
                Debug.Log($"[PurchaseManager] Mock initialized with {DefaultPrices.Count} products");
                OnStoreInitialized?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] Mock initialization failed: {e.Message}");
                _isInitialized = false;
                OnStoreInitialized?.Invoke(false);
            }
        }

        private void InitializeReal()
        {
            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (var kvp in ProductIds)
                {
                    var type = kvp.Key.Contains("premium") ? ProductType.Subscription : ProductType.NonConsumable;
                    builder.AddProduct(kvp.Value, type);
                }
                UnityPurchasing.Initialize(this, builder);
                Debug.Log($"[PurchaseManager] IAP initialization started with {ProductIds.Count} products");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] IAP initialization threw: {e.Message}");
                _isInitialized = false;
                OnStoreInitialized?.Invoke(false);
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            try
            {
                _storeController = controller;
                _extensions = extensions;
                var products = controller.products?.all;
                if (products != null)
                {
                    OnProductsUpdated?.Invoke(new List<Product>(products));
                    Debug.Log($"[PurchaseManager] Store initialized with {products.Length} products");
                    foreach (var product in products)
                        Debug.Log($"[PurchaseManager]   {product.definition.id} = {product.metadata.localizedPriceString}");
                }
                _isInitialized = true;
                OnStoreInitialized?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] OnInitialized error: {e.Message}");
                _isInitialized = false;
                OnStoreInitialized?.Invoke(false);
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[PurchaseManager] IAP initialization failed: {error} — {message}");
            _isInitialized = false;
            OnStoreInitialized?.Invoke(false);
            OnPurchaseFailureOccurred?.Invoke("system", $"Store initialization failed: {message}");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, error.ToString());
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            try
            {
                var product = args.purchasedProduct;
                if (product == null)
                {
                    Debug.LogError("[PurchaseManager] ProcessPurchase called with null product");
                    OnPurchaseFailureOccurred?.Invoke(_pendingPurchaseStoryId ?? "unknown", "Null product in purchase callback");
                    return PurchaseProcessingResult.Complete;
                }

                Debug.Log($"[PurchaseManager] Purchase completed: {product.definition.id}");

                var storyId = ResolveStoryIdFromProductId(product.definition.id);
                if (string.IsNullOrEmpty(storyId))
                {
                    Debug.LogWarning($"[PurchaseManager] Unknown product ID: {product.definition.id}");
                    OnPurchaseFailureOccurred?.Invoke(_pendingPurchaseStoryId ?? "unknown", $"Unknown product: {product.definition.id}");
                    return PurchaseProcessingResult.Complete;
                }

                var receipt = product.receipt ?? "";
                var storeName = GetStoreName();
                CacheReceipt(storyId, receipt);
                StartCoroutine(ValidateAndDeliverCoroutine(storyId, receipt, storeName));
                return PurchaseProcessingResult.Pending;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] ProcessPurchase exception: {e.Message}");
                OnPurchaseFailureOccurred?.Invoke(_pendingPurchaseStoryId ?? "unknown", $"Processing error: {e.Message}");
                return PurchaseProcessingResult.Complete;
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            try
            {
                var storyId = _pendingPurchaseStoryId ?? "unknown";
                var reason = failureDescription?.message ?? failureDescription?.reason.ToString() ?? "Unknown failure";
                Debug.LogError($"[PurchaseManager] Purchase failed for {product?.definition?.id ?? "??"}: {reason}");
                OnPurchaseFailureOccurred?.Invoke(storyId, GetUserFriendlyError(reason));
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] OnPurchaseFailed exception: {e.Message}");
                OnPurchaseFailureOccurred?.Invoke(_pendingPurchaseStoryId ?? "unknown", "Purchase failed — unknown error");
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            var storyId = _pendingPurchaseStoryId ?? "unknown";
            Debug.LogError($"[PurchaseManager] Purchase failed (legacy): {product?.definition?.id ?? "??"} — {reason}");
            OnPurchaseFailureOccurred?.Invoke(storyId, reason.ToString());
        }

        public void BuyStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId))
            {
                Debug.LogError("[PurchaseManager] BuyStory called with null/empty storyId");
                OnPurchaseFailureOccurred?.Invoke("unknown", "No story ID provided");
                return;
            }
            _pendingPurchaseStoryId = storyId;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SimulateMockPurchase(storyId);
#else
            PerformRealPurchase(storyId);
#endif
        }

        public string GetLocalizedPrice(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return "R$ --";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return DefaultPrices.GetValueOrDefault(storyId, "R$ --");
#else
            try
            {
                var productId = ProductIds.GetValueOrDefault(storyId, "");
                if (string.IsNullOrEmpty(productId)) return DefaultPrices.GetValueOrDefault(storyId, "R$ --");
                if (_storeController == null) return DefaultPrices.GetValueOrDefault(storyId, "R$ --");
                var product = _storeController.products?.WithID(productId);
                if (product != null && product.metadata != null && !string.IsNullOrEmpty(product.metadata.localizedPriceString))
                    return product.metadata.localizedPriceString;
                return DefaultPrices.GetValueOrDefault(storyId, "R$ --");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchaseManager] GetLocalizedPrice error: {e.Message}");
                return DefaultPrices.GetValueOrDefault(storyId, "R$ --");
            }
#endif
        }

        public void RestorePurchases()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[PurchaseManager] Mock restore purchases");
#else
            PerformRealRestore();
#endif
        }

        public bool IsProductOwned(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var mockOwnedKey = $"purchase_mock_owned_{storyId}";
            return SecureStorage.ObfuscatedPrefs.GetInt(mockOwnedKey, 0) == 1;
#else
            try
            {
                if (!string.IsNullOrEmpty(LoadCachedReceipt(storyId))) return true;
                if (_storeController == null) return false;
                var productId = ProductIds.GetValueOrDefault(storyId, "");
                if (string.IsNullOrEmpty(productId)) return false;
                var product = _storeController.products?.WithID(productId);
                return product != null && product.hasReceipt;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchaseManager] IsProductOwned error: {e.Message}");
                return false;
            }
#endif
        }

        public List<Product> GetProducts()
        {
            if (_storeController?.products?.all == null) return null;
            return new List<Product>(_storeController.products.all);
        }

        public string ResolveStoryIdFromProductId(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            foreach (var kvp in ProductIds)
                if (kvp.Value == productId) return kvp.Key;
            return null;
        }

        public void ConfirmPendingPurchase(Product product)
        {
            if (_storeController == null || product == null) return;
            try
            {
                _storeController.ConfirmPendingPurchase(product);
                Debug.Log($"[PurchaseManager] Confirmed pending purchase: {product.definition.id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] ConfirmPendingPurchase error: {e.Message}");
            }
        }

        private void SimulateMockPurchase(string storyId)
        {
            try
            {
                Debug.Log($"[PurchaseManager] Mock purchase: {storyId}");
                var mockOwnedKey = $"purchase_mock_owned_{storyId}";
                SecureStorage.ObfuscatedPrefs.SetInt(mockOwnedKey, 1);
                SecureStorage.ObfuscatedPrefs.Save();
                CacheReceipt(storyId, $"mock_receipt_{storyId}_{Guid.NewGuid():N}");
                OnPurchaseComplete?.Invoke(storyId, true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] Mock purchase error: {e.Message}");
                OnPurchaseFailureOccurred?.Invoke(storyId, $"Mock purchase error: {e.Message}");
            }
        }

        private void PerformRealPurchase(string storyId)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[PurchaseManager] IAP not initialized. Purchase aborted.");
                OnPurchaseFailureOccurred?.Invoke(storyId, "Store not initialized");
                return;
            }
            var productId = ProductIds.GetValueOrDefault(storyId, "");
            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogError($"[PurchaseManager] No product ID mapped for story: {storyId}");
                OnPurchaseFailureOccurred?.Invoke(storyId, $"Unknown product: {storyId}");
                return;
            }
            try
            {
                Debug.Log($"[PurchaseManager] Initiating purchase: {productId}");
                _storeController.InitiatePurchase(productId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] InitiatePurchase exception: {e.Message}");
                OnPurchaseFailureOccurred?.Invoke(storyId, $"Purchase initiation failed: {e.Message}");
            }
        }

        private void PerformRealRestore()
        {
            try
            {
                var appleExt = _extensions?.GetExtension<IAppleExtensions>();
                if (appleExt != null)
                {
                    Debug.Log("[PurchaseManager] Restoring Apple transactions...");
                    appleExt.RestoreTransactions((success, error) =>
                    {
                        if (success) Debug.Log("[PurchaseManager] Apple restore completed successfully");
                        else
                        {
                            Debug.LogError($"[PurchaseManager] Apple restore failed: {error}");
                            OnPurchaseFailureOccurred?.Invoke("system", $"Restore failed: {error}");
                        }
                    });
                }
                else
                {
                    Debug.Log("[PurchaseManager] Not an Apple platform — Google Play restores automatically on launch");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PurchaseManager] RestorePurchases exception: {e.Message}");
                OnPurchaseFailureOccurred?.Invoke("system", $"Restore error: {e.Message}");
            }
        }

        private System.Collections.IEnumerator ValidateAndDeliverCoroutine(string storyId, string receipt, string storeName)
        {
            var authService = CloudRunAuthService.Instance;
            var purchaseService = new PurchaseService(authService);
            purchaseService.Initialize();

            PurchaseVerifyResponse response = null;
            var callbackCalled = false;
            string errorMessage = null;

            yield return purchaseService.VerifyPurchase(storyId, receipt, storeName, resp =>
            {
                callbackCalled = true;
                response = resp;
                errorMessage = resp?.error;
            });

            if (callbackCalled)
            {
                if (response != null && response.success)
                {
                    Debug.Log($"[PurchaseManager] Receipt validated for {storyId}");
                    var productId = ProductIds.GetValueOrDefault(storyId, "");
                    if (!string.IsNullOrEmpty(productId) && _storeController != null)
                    {
                        var product = _storeController.products?.WithID(productId);
                        if (product != null) ConfirmPendingPurchase(product);
                    }
                    OnPurchaseComplete?.Invoke(storyId, true);
                }
                else
                {
                    Debug.LogError($"[PurchaseManager] Receipt validation failed for {storyId}: {errorMessage ?? "Unknown"}");
                    OnPurchaseFailureOccurred?.Invoke(storyId, GetUserFriendlyError(errorMessage ?? "Validation failed"));
                }
            }
            else
            {
                Debug.LogError($"[PurchaseManager] Receipt validation timed out for {storyId}");
                OnPurchaseFailureOccurred?.Invoke(storyId, GetUserFriendlyError("Network timeout during validation"));
            }
            yield return null;
        }

        private void CacheReceipt(string storyId, string receipt)
        {
            try
            {
                if (string.IsNullOrEmpty(storyId) || string.IsNullOrEmpty(receipt)) return;
                var path = System.IO.Path.Combine(Application.persistentDataPath, $"purchase_receipt_{storyId}.enc");
                SecureStorage.SaveToFile(path, receipt);
                Debug.Log($"[PurchaseManager] Receipt cached for {storyId}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchaseManager] CacheReceipt error: {e.Message}");
            }
        }

        private string LoadCachedReceipt(string storyId)
        {
            try
            {
                var path = System.IO.Path.Combine(Application.persistentDataPath, $"purchase_receipt_{storyId}.enc");
                return SecureStorage.LoadFromFile(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PurchaseManager] LoadCachedReceipt error: {e.Message}");
                return null;
            }
        }

        private string GetStoreName()
        {
#if UNITY_EDITOR
            return "mock_store";
#elif UNITY_IOS
            return "apple_app_store";
#elif UNITY_ANDROID
            return "google_play";
#else
            return "unknown";
#endif
        }

        private string GetUserFriendlyError(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "Erro desconhecido na compra";
            return reason switch
            {
                "DuplicateTransaction" => "Compra ja foi processada.",
                "ProductUnavailable" => "Produto indisponivel no momento.",
                "PurchaseUnavailable" => "Compras nao estao disponiveis.",
                "SignatureInvalid" => "Falha na validacao da compra.",
                "UserCancelled" => "Compra cancelada pelo usuario.",
                "PaymentDeclined" => "Pagamento recusado.",
                "ExistingPurchasePending" => "Ha uma compra pendente.",
                "NotAllowed" => "Compra nao autorizada.",
                _ => $"Falha na compra: {reason}"
            };
        }
    }
}
