using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Tsumiki.Runtime
{
    public sealed class AppPurchaseManager : MonoBehaviour, IDetailedStoreListener
    {
        public const string IntermediateProductId = "com.minoruhayashi.tsumikinanko.unlock.intermediate";
        public const string AdvancedProductId = "com.minoruhayashi.tsumikinanko.unlock.advanced";
        public const string AllLevelsProductId = "com.minoruhayashi.tsumikinanko.unlock.all";

        private const string IntermediateKey = "iapIntermediateUnlocked";
        private const string AdvancedKey = "iapAdvancedUnlocked";
        private static AppPurchaseManager instance;
        private IStoreController storeController;
        private IExtensionProvider extensionProvider;

        public static AppPurchaseManager Instance
        {
            get
            {
                if (instance) return instance;
                instance = FindAnyObjectByType<AppPurchaseManager>();
                if (!instance) instance = new GameObject("App Store purchases").AddComponent<AppPurchaseManager>();
                return instance;
            }
        }

        public static bool IntermediateUnlocked => PlayerPrefs.GetInt(IntermediateKey, 0) == 1;
        public static bool AdvancedUnlocked => PlayerPrefs.GetInt(AdvancedKey, 0) == 1;
        public bool Ready => storeController != null;
        public string Status { get; private set; } = "ストアに せつぞくしています…";

        private void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePurchasing();
        }

        private void InitializePurchasing()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            builder.AddProduct(IntermediateProductId, ProductType.NonConsumable);
            builder.AddProduct(AdvancedProductId, ProductType.NonConsumable);
            builder.AddProduct(AllLevelsProductId, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public string Price(string productId, string fallback)
        {
            var product = storeController?.products.WithID(productId);
            return product != null && product.availableToPurchase ? product.metadata.localizedPriceString : fallback;
        }

        public void Buy(string productId)
        {
            if (!Ready) { Status = "ストアに せつぞくできません。あとで おためしください。"; return; }
            var product = storeController.products.WithID(productId);
            if (product == null || !product.availableToPurchase) { Status = "この しょうひんは いま こうにゅうできません。"; return; }
            Status = "こうにゅうを かくにんしています…";
            storeController.InitiatePurchase(product);
        }

        public void RestorePurchases()
        {
            if (!Ready || extensionProvider == null) { Status = "ストアに せつぞくできません。"; return; }
#if UNITY_IOS || UNITY_STANDALONE_OSX
            Status = "こうにゅうを ふくげんしています…";
            extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions((success, error) =>
                Status = success ? "こうにゅうの ふくげんを かくにんしました。" : "ふくげんできませんでした。" + (string.IsNullOrEmpty(error) ? "" : $" ({error})"));
#else
            Status = "こうにゅうの ふくげんは iPhone・iPadで おこなえます。";
#endif
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            extensionProvider = extensions;
            Status = "こうにゅうする ないようを えらんでください。";
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Status = "ストアに せつぞくできません。つうしんを かくにんしてください。";
            Debug.LogWarning($"IAP initialization failed: {error} {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var id = args.purchasedProduct.definition.id;
            if (id == IntermediateProductId || id == AllLevelsProductId) PlayerPrefs.SetInt(IntermediateKey, 1);
            if (id == AdvancedProductId || id == AllLevelsProductId) PlayerPrefs.SetInt(AdvancedKey, 1);
            PlayerPrefs.Save();
            Status = "こうにゅうが かんりょうしました。ありがとうございます。";
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Status = reason == PurchaseFailureReason.UserCancelled ? "こうにゅうを キャンセルしました。" : "こうにゅうできませんでした。";
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure)
        {
            OnPurchaseFailed(product, failure.reason);
            Debug.LogWarning($"Purchase failed: {product.definition.id} {failure.reason} {failure.message}");
        }
    }
}
