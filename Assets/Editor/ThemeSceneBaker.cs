using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.EditorTools
{
    /// <summary>把两张主题关卡的地标直接烘进场景文件，运行时不再创建布景。</summary>
    public static class ThemeSceneBaker
    {
        private const string MaterialFolder = "Assets/Arts/ThirdParty/Environment/SceneMaterials/";

        [MenuItem("Tools/Train/Level Design/Bake New Theme Scenes")]
        public static void Bake()
        {
            BakeInternal();
        }

        private static void BakeInternal()
        {
            var current = SceneManager.GetActiveScene();
            BakeScene("Assets/Scenes/Playable/Level_OrbitalCargo.unity", false);
            BakeScene("Assets/Scenes/Playable/Level_CryoGarden.unity", true);
            if (current.IsValid() && !string.IsNullOrEmpty(current.path))
            {
                EditorSceneManager.OpenScene(current.path, OpenSceneMode.Single);
            }
        }

        private static void BakeScene(string path, bool cryo)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var oldDressing = GameObject.Find("StageDressing");
            if (oldDressing != null) Object.DestroyImmediate(oldDressing);
            var old = GameObject.Find("THEME_BAKED_SCENE");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("THEME_BAKED_SCENE");
            if (cryo) BakeCryo(root.transform); else BakeCargo(root.transform);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BakeCargo(Transform root)
        {
            var metal = SceneMaterial("CargoMetal", new Color(.08f, .13f, .18f), .7f, .25f);
            var accent = SceneMaterial("CargoAccent", new Color(.04f, .42f, .55f), .55f, .2f);
            Box(root, "CargoDeck", new Vector3(-47f, -.35f, -42f), new Vector3(34f, .35f, 38f), metal);
            for (var i = 0; i < 5; i++) Box(root, "CargoContainer_" + i, new Vector3(-59f + i * 6f, 1.2f, -56f), new Vector3(5f, 2.4f, 3.2f), accent);
            Model(root, "FactoryCrane", "Assets/Resources/ThemeModels/crane.fbx", new Vector3(-47f, 0f, -45f), Vector3.one * 2.4f, metal);
            Model(root, "FactoryConveyor", "Assets/Resources/ThemeModels/conveyor-long.fbx", new Vector3(-47f, .05f, -50f), new Vector3(2f, 1f, 8f), metal);
            Model(root, "FactoryGate", "Assets/Resources/ThemeModels/door-wide-closed.fbx", new Vector3(-47f, 0f, -61f), Vector3.one * 3f, accent);
            for (var i = 0; i < 3; i++) Box(root, "HangarTower_" + i, new Vector3(-59f + i * 12f, 5f, -27f), new Vector3(4f, 10f, 3f), metal);
        }

        private static void BakeCryo(Transform root)
        {
            var ice = SceneMaterial("CryoIce", new Color(.32f, .68f, .82f), .2f, .5f);
            var crystal = SceneMaterial("CryoCrystal", new Color(.55f, .18f, .95f), .1f, .6f);
            Box(root, "FrozenPlateau", new Vector3(-47f, -.4f, -44f), new Vector3(38f, .3f, 42f), ice);
            Model(root, "NatureCliff", "Assets/Resources/ThemeModels/cliff_large_stone.dae", new Vector3(-63f, 0f, -54f), Vector3.one * 3.2f, ice);
            Model(root, "NatureRock", "Assets/Resources/ThemeModels/rock_largeA.dae", new Vector3(-31f, 0f, -56f), Vector3.one * 2.2f, crystal);
            Model(root, "NaturePine", "Assets/Resources/ThemeModels/tree_pineTallA.dae", new Vector3(-62f, 0f, -29f), Vector3.one * 2.8f, ice);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 2f / 8f;
                Model(root, "CrystalBloom_" + i, "Assets/Resources/ThemeModels/rock_largeA.dae", new Vector3(-47f + Mathf.Cos(angle) * 14f, 1.3f, -44f + Mathf.Sin(angle) * 14f), Vector3.one * .6f, crystal);
            }
        }

        private static void Model(Transform root, string name, string path, Vector3 position, Vector3 scale, Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, root);
            instance.name = name; instance.transform.position = position; instance.transform.localScale = scale;
            ApplyMaterial(instance, material);
        }

        private static void Box(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(root); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                for (var i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots.Length == 0 ? new[] { material } : slots;
            }
        }

        private static Material SceneMaterial(string name, Color color, float metallic, float smoothness)
        {
            EnsureFolder(MaterialFolder);
            var path = MaterialFolder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.TrimEnd('/').Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
