using System.Collections.Generic;
using System.IO;
using MatchPack.Data;
using MatchPack.Meta;
using UnityEditor;
using UnityEngine;

namespace MatchPack.EditorTools
{
    /// <summary>
    /// UIIntegrationTool'un ihtiyaç duyduğu asset'leri (AudioLibrary, ShopCatalog, ShopProduct'lar
    /// ve ekran efekti materyali) yoksa üretir, varsa mevcut olanı döner.
    /// </summary>
    public static class UIIntegrationAssets
    {
        public const string ConfigFolder = "Assets/_Project/Data/Config";
        public const string ShopFolder = "Assets/_Project/Data/Shop";
        public const string MaterialFolder = "Assets/_Project/Art/Materials";

        public const string AudioLibraryPath = ConfigFolder + "/AudioLibrary.asset";
        public const string ShopCatalogPath = ConfigFolder + "/ShopCatalog.asset";
        public const string GameConfigPath = ConfigFolder + "/GameConfig.asset";
        public const string FlashMaterialPath = MaterialFolder + "/M_UI_ScreenEdgeFlash.mat";
        public const string FlashShaderName = "MatchPack/UI/ScreenEdgeFlash";

        // Kenar bandının kalınlığı ekran genişliğinin oranı olarak verilir; dikeyde en-boy
        // oranıyla küçültülüp dört kenarda da eşit piksel kalınlığı elde edilir.
        private const float FlashThickness = 0.14f;
        private const float FlashEdgePower = 3f;
        private static readonly Color FlashColor = new Color(0.58f, 0.02f, 0.02f, 1f);

        private struct ProductDefinition
        {
            public string AssetName;
            public string ProductId;
            public int Gold;
            public int Lives;
            public float InfiniteHours;
            public int BoosterEach;
            public string Price;
        }

        // Sahnedeki market kartlarından okunan içerik ve fiyatlar. Gerçek fiyat store'dan gelir;
        // buradaki metin yalnızca store'a bağlanılamadığında gösterilir.
        private static readonly ProductDefinition[] Definitions =
        {
            new ProductDefinition { AssetName = "Product_Coins_1000",   ProductId = "coins_1000",   Gold = 1000,   Price = "99,99 TL" },
            new ProductDefinition { AssetName = "Product_Coins_5000",   ProductId = "coins_5000",   Gold = 5000,   Price = "399,99 TL" },
            new ProductDefinition { AssetName = "Product_Coins_10000",  ProductId = "coins_10000",  Gold = 10000,  Price = "799,99 TL" },
            new ProductDefinition { AssetName = "Product_Coins_25000",  ProductId = "coins_25000",  Gold = 25000,  Price = "1.499,99 TL" },
            new ProductDefinition { AssetName = "Product_Coins_50000",  ProductId = "coins_50000",  Gold = 50000,  Price = "2.999,99 TL" },
            new ProductDefinition { AssetName = "Product_Coins_100000", ProductId = "coins_100000", Gold = 100000, Price = "4.999,99 TL" },
            new ProductDefinition { AssetName = "Product_Box_Starter",  ProductId = "box_starter",  Gold = 0,     InfiniteHours = 1f,  BoosterEach = 1,  Price = "49,99 TL" },
            new ProductDefinition { AssetName = "Product_Box_Beginner", ProductId = "box_beginner", Gold = 1000,  InfiniteHours = 3f,  BoosterEach = 1,  Price = "249,99 TL" },
            new ProductDefinition { AssetName = "Product_Box_Mega",     ProductId = "box_mega",     Gold = 4000,  InfiniteHours = 6f,  BoosterEach = 5,  Price = "499,99 TL" },
            new ProductDefinition { AssetName = "Product_Box_Golden",   ProductId = "box_golden",   Gold = 60000, InfiniteHours = 48f, BoosterEach = 50, Price = "4.999,99 TL" }
        };

        public static AudioLibrary EnsureAudioLibrary()
        {
            AudioLibrary existing = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath);
            if (existing != null) { return existing; }

            EnsureFolder(ConfigFolder);
            AudioLibrary created = ScriptableObject.CreateInstance<AudioLibrary>();
            AssetDatabase.CreateAsset(created, AudioLibraryPath);
            UIIntegrationUtility.Log($"{AudioLibraryPath} oluşturuldu (klipler boş, elle doldurulacak).");
            UIIntegrationUtility.Problem("AudioLibrary içindeki ses klipleri boş; ses dosyaları gelince Inspector'dan bağla.");
            return created;
        }

        public static GameConfig LoadGameConfig()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            if (config == null) { UIIntegrationUtility.Problem($"{GameConfigPath} bulunamadı."); }

            return config;
        }

        public static ShopCatalog EnsureShopCatalog()
        {
            EnsureFolder(ConfigFolder);
            EnsureFolder(ShopFolder);

            List<ShopProduct> products = new List<ShopProduct>();
            for (int i = 0; i < Definitions.Length; i++)
            {
                products.Add(EnsureProduct(Definitions[i]));
            }

            ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ShopCatalog>();
                AssetDatabase.CreateAsset(catalog, ShopCatalogPath);
                UIIntegrationUtility.Log($"{ShopCatalogPath} oluşturuldu.");
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty array = serialized.FindProperty("_products");
            array.arraySize = products.Count;
            for (int i = 0; i < products.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = products[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        public static Material EnsureFlashMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);

            if (material == null)
            {
                Shader shader = Shader.Find(FlashShaderName);
                if (shader == null)
                {
                    UIIntegrationUtility.Problem(
                        $"{FlashShaderName} shader'ı bulunamadı. UI_ScreenEdgeFlash.shader derlenmemiş olabilir.");
                    return null;
                }

                EnsureFolder(MaterialFolder);
                material = new Material(shader) { name = "M_UI_ScreenEdgeFlash" };
                AssetDatabase.CreateAsset(material, FlashMaterialPath);
                UIIntegrationUtility.Log($"{FlashMaterialPath} oluşturuldu.");
            }

            // Değerler her çalıştırmada yeniden yazılır; eski bir materyal kalmışsa ayarı güncellenir.
            material.SetColor("_FlashColor", FlashColor);
            material.SetFloat("_Thickness", FlashThickness);
            material.SetFloat("_EdgePower", FlashEdgePower);
            material.SetFloat("_Intensity", 0f);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static ShopProduct EnsureProduct(ProductDefinition definition)
        {
            string path = $"{ShopFolder}/{definition.AssetName}.asset";
            ShopProduct product = AssetDatabase.LoadAssetAtPath<ShopProduct>(path);

            if (product == null)
            {
                product = ScriptableObject.CreateInstance<ShopProduct>();
                AssetDatabase.CreateAsset(product, path);
                UIIntegrationUtility.Log($"{path} oluşturuldu.");
            }

            SerializedObject serialized = new SerializedObject(product);
            serialized.FindProperty("_productId").stringValue = definition.ProductId;
            serialized.FindProperty("_productType").enumValueIndex = (int)ShopProductType.Consumable;
            serialized.FindProperty("_fallbackPriceText").stringValue = definition.Price;
            serialized.FindProperty("_goldReward").intValue = definition.Gold;
            serialized.FindProperty("_livesReward").intValue = definition.Lives;
            serialized.FindProperty("_infiniteLivesHours").floatValue = definition.InfiniteHours;

            SerializedProperty boosters = serialized.FindProperty("_boosterRewards");
            boosters.arraySize = definition.BoosterEach > 0 ? 4 : 0;
            for (int i = 0; i < boosters.arraySize; i++)
            {
                boosters.GetArrayElementAtIndex(i).intValue = definition.BoosterEach;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(product);
            return product;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) { return; }

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
