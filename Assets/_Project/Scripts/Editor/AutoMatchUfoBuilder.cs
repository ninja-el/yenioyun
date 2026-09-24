using MatchPack.Data;
using MatchPack.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MatchPack.EditorTools
{
    /// <summary>
    /// Auto-Match UFO prefab'ını, materyallerini ve ışın mesh'ini üretir ve Booster_AutoMatch
    /// asset'ine bağlar. Tekrar çalıştırılırsa var olan asset'lerin üzerine yazar; GUID'ler korunur.
    /// </summary>
    public static class AutoMatchUfoBuilder
    {
        private const string BaseColorPath = "Assets/_Project/Art/Textures/UFO/T_Ufo_BaseColor.jpeg";
        private const string MaskPath = "Assets/_Project/Art/Textures/UFO/T_Ufo_Mask.png";
        private const string BillboardShaderPath = "Assets/_Project/Art/VFX/UfoBillboard.shader";
        private const string BeamShaderPath = "Assets/_Project/Art/VFX/UfoBeam.shader";
        private const string BillboardMaterialPath = "Assets/_Project/Art/Materials/M_Ufo_Billboard.mat";
        private const string BeamMaterialPath = "Assets/_Project/Art/Materials/M_Ufo_Beam.mat";
        private const string BeamMeshPath = "Assets/_Project/Art/Models/SM_UfoBeam.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/Gameplay/AutoMatchUfo.prefab";
        private const string BoosterDataPath = "Assets/_Project/Data/Boosters/Booster_AutoMatch.asset";

        // Kaynak görselde UFO'nun kapladığı dikdörtgen (piksel). Görselin geri kalanı boş arka plandır.
        private static readonly Vector2 SourceSize = new Vector2(1206f, 1503f);
        private static readonly Rect SourceCrop = Rect.MinMaxRect(110f, 390f, 1095f, 1090f);

        private const float BodyWidth = 2.8f;
        private const int BeamSegments = 32;
        private const float BeamTopRadius = 0.15f;
        private const int RingCount = 5;
        private const int RingPointCount = 48;
        private const float RingWidth = 0.022f;
        private static readonly Color BeamColor = new Color(0.08f, 0.75f, 1f, 0.6f);

        [MenuItem("MatchPack/Build Auto-Match UFO")]
        public static void Build()
        {
            Shader billboardShader = AssetDatabase.LoadAssetAtPath<Shader>(BillboardShaderPath);
            Shader beamShader = AssetDatabase.LoadAssetAtPath<Shader>(BeamShaderPath);
            if (billboardShader == null || beamShader == null)
            {
                Debug.LogError($"AutoMatchUfoBuilder: shader missing at {BillboardShaderPath} or {BeamShaderPath}.");
                return;
            }

            if (!UfoMaskGenerator.Generate(BaseColorPath, MaskPath)) { return; }
            if (!ConfigureTexture(BaseColorPath, true) || !ConfigureTexture(MaskPath, false)) { return; }

            Material billboardMaterial = CreateBillboardMaterial(billboardShader);
            Material beamMaterial = LoadOrCreateMaterial(BeamMaterialPath, beamShader);
            beamMaterial.SetColor("_Color", BeamColor);
            EditorUtility.SetDirty(beamMaterial);

            GameObject prefab = BuildPrefab(billboardMaterial, beamMaterial, LoadOrCreateBeamMesh());
            AssignToBoosterData(prefab);

            AssetDatabase.SaveAssets();
            Debug.Log($"AutoMatchUfoBuilder: built {PrefabPath} and bound it to {BoosterDataPath}.");
        }

        private static bool ConfigureTexture(string path, bool isColor)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                Debug.LogError($"AutoMatchUfoBuilder: texture missing at {path}.");
                return false;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = isColor;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return true;
        }

        private static Material CreateBillboardMaterial(Shader shader)
        {
            Material material = LoadOrCreateMaterial(BillboardMaterialPath, shader);
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
            material.SetTexture("_MaskTex", AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath));
            material.mainTextureScale = new Vector2(SourceCrop.width / SourceSize.x, SourceCrop.height / SourceSize.y);
            material.mainTextureOffset = new Vector2(SourceCrop.xMin / SourceSize.x, 1f - SourceCrop.yMax / SourceSize.y);
            material.SetFloat("_Collected", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Kesik koni: taban yarıçapı 1, tepe yarıçapı BeamTopRadius, yükseklik 1. Tepe parlak, taban soluk.
        private static Mesh LoadOrCreateBeamMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BeamMeshPath);
            bool isNew = mesh == null;
            if (isNew) { mesh = new Mesh { name = "SM_UfoBeam" }; }

            var vertices = new Vector3[(BeamSegments + 1) * 2];
            var colors = new Color[vertices.Length];
            var triangles = new int[BeamSegments * 6];

            for (int i = 0; i <= BeamSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / BeamSegments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * BeamTopRadius + Vector3.up;
                vertices[i * 2 + 1] = direction;
                colors[i * 2] = new Color(1f, 1f, 1f, 0.4f);
                colors[i * 2 + 1] = new Color(1f, 1f, 1f, 0.04f);

                if (i == BeamSegments) { continue; }

                int triangle = i * 6;
                int vertex = i * 2;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex + 2;
                triangles[triangle + 4] = vertex + 1;
                triangles[triangle + 5] = vertex + 3;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            if (isNew) { AssetDatabase.CreateAsset(mesh, BeamMeshPath); }
            else { EditorUtility.SetDirty(mesh); }

            return mesh;
        }

        private static GameObject BuildPrefab(Material billboardMaterial, Material beamMaterial, Mesh beamMesh)
        {
            var root = new GameObject("AutoMatchUfo");

            try
            {
                AutoMatchUfo ufo = root.AddComponent<AutoMatchUfo>();

                // Kameraya dönüş ve ölçek "Body"ye, görselin en/boy oranı alttaki quad'a yazılır.
                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);

                float spriteHeight = BodyWidth * SourceCrop.height / SourceCrop.width;
                MeshRenderer sprite = CreateMeshChild(body.transform, "Sprite", Resources.GetBuiltinResource<Mesh>("Quad.fbx"), billboardMaterial);
                sprite.transform.localScale = new Vector3(BodyWidth, spriteHeight, 1f);

                MeshRenderer beam = CreateMeshChild(root.transform, "Beam", beamMesh, beamMaterial);

                var rings = new LineRenderer[RingCount];
                for (int i = 0; i < RingCount; i++)
                {
                    rings[i] = CreateRing(root.transform, beamMaterial, i);
                }

                var serialized = new SerializedObject(ufo);
                serialized.FindProperty("_body").objectReferenceValue = body.transform;
                serialized.FindProperty("_bodyRenderer").objectReferenceValue = sprite;
                serialized.FindProperty("_beam").objectReferenceValue = beam.transform;

                SerializedProperty ringsProperty = serialized.FindProperty("_rings");
                ringsProperty.arraySize = RingCount;
                for (int i = 0; i < RingCount; i++)
                {
                    ringsProperty.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static MeshRenderer CreateMeshChild(Transform parent, string name, Mesh mesh, Material material)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);

            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return meshRenderer;
        }

        private static LineRenderer CreateRing(Transform parent, Material material, int index)
        {
            var ring = new GameObject($"Ring_{index}");
            ring.transform.SetParent(parent, false);

            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = RingPointCount;
            line.widthMultiplier = RingWidth;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static void AssignToBoosterData(GameObject prefab)
        {
            BoosterData data = AssetDatabase.LoadAssetAtPath<BoosterData>(BoosterDataPath);
            if (data == null)
            {
                Debug.LogError($"AutoMatchUfoBuilder: {BoosterDataPath} not found; assign the UFO prefab by hand.");
                return;
            }

            var serialized = new SerializedObject(data);
            serialized.FindProperty("_ufoPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
