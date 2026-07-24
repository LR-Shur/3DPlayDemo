using System.IO;
using System.Linq;
using Train.Gameplay.Player.Animation.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 从 Ellen 原始 FBX 动画生成运行时原地副本。
    /// 生成过程将实际蒙皮根骨骼 Bip001 的水平位置曲线固定到绑定原点，其余身体、尾巴、裙子和武器曲线保持不变。
    /// </summary>
    public static class EllenInPlaceAnimationGenerator
    {
        private const string OutputRoot = "Assets/Arts/Player/Ellen/AnimationGenerated/InPlace";
        private const string SkinRootPath = "Avatar_Female_Size02_Ellen/Bip001";

        /// <summary>
        /// 创建或覆盖一个可写的原地动画副本，并返回供动画目录引用的资源。
        /// </summary>
        /// <param name="sourceClip">需要保留全部姿势曲线的原始 FBX 动画。</param>
        /// <param name="category">用于组织生成目录的动画分类。</param>
        /// <param name="id">用于生成稳定文件名的动画标识。</param>
        /// <returns>已保存到项目中的原地动画副本。</returns>
        public static AnimationClip CreateOrUpdate(
            AnimationClip sourceClip,
            PlayerAnimationCategory category,
            PlayerAnimationId id)
        {
            var folderPath = $"{OutputRoot}/{category}";
            Directory.CreateDirectory(folderPath);
            var assetPath = $"{folderPath}/{id}.anim";
            var generatedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (generatedClip == null)
            {
                generatedClip = Object.Instantiate(sourceClip);
                generatedClip.name = id.ToString();
                generatedClip.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(generatedClip, assetPath);
            }
            else
            {
                EditorUtility.CopySerialized(sourceClip, generatedClip);
                generatedClip.name = id.ToString();
            }

            FreezeHorizontalSkinRootCurves(generatedClip);
            CorrectKnownSkinRootFacing(generatedClip, id);
            EditorUtility.SetDirty(generatedClip);
            return generatedClip;
        }

        /// <summary>
    /// 将实际蒙皮根骨骼的 X/Z 位置曲线改为零常量，消除动画自身向前或侧向滑动。
    /// 不能使用各动画首帧的 X/Z 值，因为不同动画的首帧根位置不同，切换时会在缩放后的骨架上产生明显瞬移。
        /// </summary>
        /// <param name="animationClip">需要处理的可写动画副本。</param>
        private static void FreezeHorizontalSkinRootCurves(AnimationClip animationClip)
        {
            var bindings = AnimationUtility.GetCurveBindings(animationClip)
                .Where(binding => binding.type == typeof(Transform) &&
                                  binding.path == SkinRootPath &&
                                  (binding.propertyName == "m_LocalPosition.x" ||
                                   binding.propertyName == "m_LocalPosition.z"))
                .ToArray();

            foreach (var binding in bindings)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(animationClip, binding);
                if (sourceCurve == null || sourceCurve.length == 0)
                {
                    continue;
                }

                var firstKey = sourceCurve.keys[0];
                var lastKey = sourceCurve.keys[^1];
                var constantCurve = new AnimationCurve(
                    new Keyframe(firstKey.time, 0f),
                    new Keyframe(lastKey.time, 0f))
                {
                    preWrapMode = sourceCurve.preWrapMode,
                    postWrapMode = sourceCurve.postWrapMode,
                };
                AnimationUtility.SetEditorCurve(animationClip, binding, constantCurve);
            }
        }

        /// <summary>
        /// 修正已确认朝向错误的动画资源。
        /// 仅旋转表现骨架的根节点，不旋转 Player 逻辑根物体，因此不会影响输入方向、相机朝向或其他动画。
        /// </summary>
        /// <param name="animationClip">需要修正的可写动画副本。</param>
        /// <param name="id">当前动画的稳定标识。</param>
        private static void CorrectKnownSkinRootFacing(
            AnimationClip animationClip,
            PlayerAnimationId id)
        {
            if (id != PlayerAnimationId.Attack_Normal_03_01)
            {
                return;
            }

            RotateSkinRootAroundYAxis(animationClip, 180f);
        }

        /// <summary>
        /// 将皮肤根骨骼的局部旋转曲线整体绕 Y 轴旋转指定角度。
        /// 使用四元数乘法逐关键帧重建四条曲线，避免直接修改单一分量造成旋转失真。
        /// </summary>
        /// <param name="animationClip">需要修改旋转曲线的可写动画副本。</param>
        /// <param name="angle">绕局部 Y 轴附加的角度。</param>
        private static void RotateSkinRootAroundYAxis(AnimationClip animationClip, float angle)
        {
            var rotationBindings = AnimationUtility.GetCurveBindings(animationClip)
                .Where(binding => binding.type == typeof(Transform) &&
                                  binding.path == SkinRootPath &&
                                  binding.propertyName.StartsWith("m_LocalRotation."))
                .ToDictionary(binding => binding.propertyName);

            if (!rotationBindings.TryGetValue("m_LocalRotation.x", out var xBinding) ||
                !rotationBindings.TryGetValue("m_LocalRotation.y", out var yBinding) ||
                !rotationBindings.TryGetValue("m_LocalRotation.z", out var zBinding) ||
                !rotationBindings.TryGetValue("m_LocalRotation.w", out var wBinding))
            {
                Debug.LogWarning($"动画 {animationClip.name} 缺少 Bip001 旋转曲线，无法修正朝向。");
                return;
            }

            var xCurve = AnimationUtility.GetEditorCurve(animationClip, xBinding);
            var yCurve = AnimationUtility.GetEditorCurve(animationClip, yBinding);
            var zCurve = AnimationUtility.GetEditorCurve(animationClip, zBinding);
            var wCurve = AnimationUtility.GetEditorCurve(animationClip, wBinding);
            if (xCurve == null || yCurve == null || zCurve == null || wCurve == null)
            {
                return;
            }

            var keyTimes = xCurve.keys.Select(key => key.time)
                .Concat(yCurve.keys.Select(key => key.time))
                .Concat(zCurve.keys.Select(key => key.time))
                .Concat(wCurve.keys.Select(key => key.time))
                .Distinct()
                .OrderBy(time => time)
                .ToArray();
            var correction = Quaternion.AngleAxis(angle, Vector3.up);
            var correctedXKeys = new Keyframe[keyTimes.Length];
            var correctedYKeys = new Keyframe[keyTimes.Length];
            var correctedZKeys = new Keyframe[keyTimes.Length];
            var correctedWKeys = new Keyframe[keyTimes.Length];
            var previousRotation = Quaternion.identity;

            for (var index = 0; index < keyTimes.Length; index++)
            {
                var time = keyTimes[index];
                var sourceRotation = new Quaternion(
                    xCurve.Evaluate(time),
                    yCurve.Evaluate(time),
                    zCurve.Evaluate(time),
                    wCurve.Evaluate(time)).normalized;
                var correctedRotation = correction * sourceRotation;
                if (index > 0 && Quaternion.Dot(previousRotation, correctedRotation) < 0f)
                {
                    correctedRotation = new Quaternion(
                        -correctedRotation.x,
                        -correctedRotation.y,
                        -correctedRotation.z,
                        -correctedRotation.w);
                }

                correctedXKeys[index] = new Keyframe(time, correctedRotation.x);
                correctedYKeys[index] = new Keyframe(time, correctedRotation.y);
                correctedZKeys[index] = new Keyframe(time, correctedRotation.z);
                correctedWKeys[index] = new Keyframe(time, correctedRotation.w);
                previousRotation = correctedRotation;
            }

            AnimationUtility.SetEditorCurve(animationClip, xBinding, new AnimationCurve(correctedXKeys));
            AnimationUtility.SetEditorCurve(animationClip, yBinding, new AnimationCurve(correctedYKeys));
            AnimationUtility.SetEditorCurve(animationClip, zBinding, new AnimationCurve(correctedZKeys));
            AnimationUtility.SetEditorCurve(animationClip, wBinding, new AnimationCurve(correctedWKeys));
        }
    }
}
