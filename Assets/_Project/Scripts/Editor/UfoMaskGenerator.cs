using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MatchPack.EditorTools
{
    /// <summary>
    /// UFO görselinin beyaz arka planını ayıran siluet maskesini üretir. Kubbedeki beyaz parlamalar
    /// arka planla aynı renkte olduğu için renk anahtarı yetmez; maske görseldeki en büyük koyu
    /// bölgeden, içindeki boşluklar doldurularak çıkarılır.
    /// </summary>
    public static class UfoMaskGenerator
    {
        // Üç kanalın en küçüğü bunun altındaysa piksel UFO'ya aittir (0.9 * 255).
        private const byte BackgroundThreshold = 229;
        private const int ClosingIterations = 2;

        // Sigma 0.55 gauss çekirdeği; kenarı yumuşatıp JPEG tırtığını gizler.
        private static readonly float[] BlurKernel = { 0.1385f, 0.723f, 0.1385f };

        /// <summary>Kaynak görselden maskeyi üretip PNG olarak yazar ve import eder.</summary>
        public static bool Generate(string sourcePath, string maskPath)
        {
            var source = new Texture2D(2, 2);

            try
            {
                if (!File.Exists(sourcePath) || !source.LoadImage(File.ReadAllBytes(sourcePath)))
                {
                    Debug.LogError($"UfoMaskGenerator: could not read {sourcePath}.");
                    return false;
                }

                int width = source.width;
                int height = source.height;

                bool[] mask = FindLargestRegion(source.GetPixels32(), width, height);
                FillHoles(mask, width, height);

                for (int i = 0; i < ClosingIterations; i++) { mask = Morph(mask, width, height, true); }
                for (int i = 0; i < ClosingIterations; i++) { mask = Morph(mask, width, height, false); }

                WritePng(Blur(mask, width, height), width, height, maskPath);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }

            AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        private static bool[] FindLargestRegion(Color32[] pixels, int width, int height)
        {
            var labels = new int[pixels.Length];
            var queue = new Queue<int>();
            int bestLabel = 0;
            int bestSize = 0;
            int label = 0;

            for (int start = 0; start < pixels.Length; start++)
            {
                if (labels[start] != 0 || !IsForeground(pixels[start])) { continue; }

                label++;
                int size = 0;
                labels[start] = label;
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    int index = queue.Dequeue();
                    size++;
                    VisitNeighbours(index, width, height, neighbour =>
                    {
                        if (labels[neighbour] != 0 || !IsForeground(pixels[neighbour])) { return; }

                        labels[neighbour] = label;
                        queue.Enqueue(neighbour);
                    });
                }

                if (size > bestSize)
                {
                    bestSize = size;
                    bestLabel = label;
                }
            }

            var mask = new bool[pixels.Length];
            for (int i = 0; i < mask.Length; i++) { mask[i] = labels[i] == bestLabel && bestLabel != 0; }

            return mask;
        }

        // Kenardan erişilemeyen her boş piksel silüetin içindedir (kubbe parlaması gibi) ve doldurulur.
        private static void FillHoles(bool[] mask, int width, int height)
        {
            var isOutside = new bool[mask.Length];
            var queue = new Queue<int>();

            for (int i = 0; i < mask.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                bool isBorder = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                if (!isBorder || mask[i]) { continue; }

                isOutside[i] = true;
                queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                VisitNeighbours(queue.Dequeue(), width, height, neighbour =>
                {
                    if (isOutside[neighbour] || mask[neighbour]) { return; }

                    isOutside[neighbour] = true;
                    queue.Enqueue(neighbour);
                });
            }

            for (int i = 0; i < mask.Length; i++) { mask[i] = !isOutside[i]; }
        }

        // Artı şeklinde komşulukla genişletme (dilate) veya daraltma (erode).
        private static bool[] Morph(bool[] mask, int width, int height, bool isDilate)
        {
            var result = new bool[mask.Length];

            for (int i = 0; i < mask.Length; i++)
            {
                bool value = mask[i];
                VisitNeighbours(i, width, height, neighbour =>
                {
                    value = isDilate ? value || mask[neighbour] : value && mask[neighbour];
                });
                result[i] = value;
            }

            return result;
        }

        private static float[] Blur(bool[] mask, int width, int height)
        {
            var horizontal = new float[mask.Length];
            var result = new float[mask.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    horizontal[y * width + x] = SampleRow(mask, width, x, y);
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int k = -1; k <= 1; k++)
                    {
                        sum += horizontal[Mathf.Clamp(y + k, 0, height - 1) * width + x] * BlurKernel[k + 1];
                    }

                    result[y * width + x] = sum;
                }
            }

            return result;
        }

        private static float SampleRow(bool[] mask, int width, int x, int y)
        {
            float sum = 0f;
            for (int k = -1; k <= 1; k++)
            {
                sum += (mask[y * width + Mathf.Clamp(x + k, 0, width - 1)] ? 1f : 0f) * BlurKernel[k + 1];
            }

            return sum;
        }

        private static void WritePng(float[] alpha, int width, int height, string path)
        {
            var pixels = new Color32[alpha.Length];
            for (int i = 0; i < alpha.Length; i++)
            {
                byte value = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha[i]) * 255f);
                pixels[i] = new Color32(value, value, value, 255);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            try
            {
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static bool IsForeground(Color32 pixel)
        {
            return Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b)) < BackgroundThreshold;
        }

        private static void VisitNeighbours(int index, int width, int height, System.Action<int> visit)
        {
            int x = index % width;
            int y = index / width;

            if (x > 0) { visit(index - 1); }
            if (x < width - 1) { visit(index + 1); }
            if (y > 0) { visit(index - width); }
            if (y < height - 1) { visit(index + width); }
        }
    }
}
