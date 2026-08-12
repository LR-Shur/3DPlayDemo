#if UNITY_EDITOR
using System.Linq;
using Train.Infrastructure.Assets;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace Train.EditorTools.Architecture
{
    [InitializeOnLoad]
    internal static class YooAssetProjectConfigurator
    {
        private const string PackageName = "DefaultPackage";
        private const string SettingsFolder = "Assets/Resources/Settings";
        private const string SettingsPath =
            SettingsFolder + "/YooAssetRuntimeSettings.asset";

        private static readonly string[] CollectPaths =
        {
            "Assets/Prefabs",
            "Assets/Data",
            "Assets/Scenes/Playable",
            "Assets/UI",
            "Assets/Arts/Characters"
        };

        static YooAssetProjectConfigurator()
        {
            EditorApplication.delayCall += EnsureConfigurationIfMissing;
        }

        [MenuItem("Tools/Train/Architecture/Configure YooAsset")]
        private static void RebuildConfiguration()
        {
            Configure(forceRebuild: true);
        }

        private static void EnsureConfigurationIfMissing()
        {
            Configure(forceRebuild: false);
        }

        private static void Configure(bool forceRebuild)
        {
            EnsureRuntimeSettings();

            var desiredPaths = CollectPaths
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();
            var package = BundleCollectorSettingData.Setting.Packages
                .FirstOrDefault(candidate => candidate.PackageName == PackageName);

            if (IsCurrent(package, desiredPaths))
            {
                return;
            }

            if (package != null && !forceRebuild)
            {
                Debug.LogWarning(
                    $"YooAsset package '{PackageName}' differs from the recommended " +
                    "project collectors. Existing configuration was preserved. Use " +
                    "Tools/Train/Architecture/Configure YooAsset to rebuild it explicitly.");
                return;
            }

            if (package != null)
            {
                BundleCollectorSettingData.RemovePackage(package);
            }

            package = BundleCollectorSettingData.CreatePackage(PackageName);
            package.EnableAddressable = false;
            package.AutoCollectShaders = true;
            package.IgnoreRuleName = nameof(NormalIgnoreRule);

            foreach (var collectPath in desiredPaths)
            {
                var groupName = collectPath
                    .Replace("Assets/", string.Empty)
                    .Replace('/', '_');
                var group = BundleCollectorSettingData.CreateGroup(
                    package,
                    groupName);
                var collector = new BundleCollector
                {
                    CollectPath = collectPath,
                    CollectorGUID = AssetDatabase.AssetPathToGUID(collectPath),
                    CollectorType = ECollectorType.MainAssetCollector,
                    AddressRuleName = nameof(AddressByFolderAndFileName),
                    PackRuleName = nameof(PackDirectory),
                    FilterRuleName = nameof(CollectAll)
                };
                BundleCollectorSettingData.CreateCollector(group, collector);
            }

            BundleCollectorSettingData.FixFile();
            BundleCollectorSettingData.SaveFile();
            Debug.Log(
                $"Configured YooAsset package '{PackageName}' with " +
                $"{desiredPaths.Length} collector groups.");
        }

        private static bool IsCurrent(
            BundleCollectorPackage package,
            string[] desiredPaths)
        {
            if (package == null ||
                package.EnableAddressable ||
                package.Groups.Count != desiredPaths.Length)
            {
                return false;
            }

            var currentPaths = package.Groups
                .SelectMany(group => group.Collectors)
                .Select(collector => collector.CollectPath)
                .OrderBy(path => path)
                .ToArray();
            var addressRulesAreCurrent = package.Groups
                .SelectMany(group => group.Collectors)
                .All(collector => collector.AddressRuleName ==
                    nameof(AddressByFolderAndFileName));
            return addressRulesAreCurrent && currentPaths.SequenceEqual(
                desiredPaths.OrderBy(path => path));
        }

        private static void EnsureRuntimeSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<YooAssetRuntimeSettings>(
                    SettingsPath) != null)
            {
                return;
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder(SettingsFolder);
            var settings = ScriptableObject.CreateInstance<YooAssetRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
