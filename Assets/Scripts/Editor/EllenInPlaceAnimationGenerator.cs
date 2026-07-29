using System.IO;
using System.Linq;
using Train.Gameplay.Player.Animation.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 从 Ellen 原始 FBX 动画生成运行时原地副本。
    /// 生成过程将实际蒙皮根骨骼 Bip001 的水平位置固定到绑定原点。
    /// 普通移动动画保留完整根旋转；TurnBack 仅把水平转向提取为数据曲线。
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
        /// <param name="loop">该动画是否需要作为循环动画播放。</param>
        /// <param name="authoredTurnCurve">TurnBack 返回提取的水平转向曲线，其余动画返回空。</param>
        /// <returns>已保存到项目中的原地动画副本。</returns>
        public static AnimationClip CreateOrUpdate(
            AnimationClip sourceClip,
            PlayerAnimationCategory category,
            PlayerAnimationId id,
            bool loop,
            out AnimationCurve authoredTurnCurve)
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
            authoredTurnCurve = id == PlayerAnimationId.TurnBack
                ? RemoveHorizontalSkinRootRotation(generatedClip)
                : null;
            CorrectKnownSkinRootFacing(generatedClip, id);
            ConfigureLooping(generatedClip, loop);
            EditorUtility.SetDirty(generatedClip);
            return generatedClip;
        }

        /// <summary>
        /// 从 TurnBack 的 Bip001 四元数曲线中提取水平转向，并从骨架根姿势中移除对应的 Y 轴 Twist。
        /// Bip001 的俯仰、侧倾和所有子骨骼动画仍完整保留。
        /// </summary>
        /// <param name="animationClip">需要处理的 TurnBack 运行时动画副本。</param>
        /// <returns>横轴为动画归一化进度、纵轴为累计转向比例的单调曲线。</returns>
        private static AnimationCurve RemoveHorizontalSkinRootRotation(
            AnimationClip animationClip)
        {
            var rotationBindings = AnimationUtility.GetCurveBindings(animationClip)
                .Where(binding => binding.type == typeof(Transform) &&
                                  binding.path == SkinRootPath &&
                                  binding.propertyName.StartsWith(
                                      "m_LocalRotation.",
                                      System.StringComparison.Ordinal))
                .ToDictionary(binding => binding.propertyName);
            if (!rotationBindings.TryGetValue(
                    "m_LocalRotation.x",
                    out var xBinding) ||
                !rotationBindings.TryGetValue(
                    "m_LocalRotation.y",
                    out var yBinding) ||
                !rotationBindings.TryGetValue(
                    "m_LocalRotation.z",
                    out var zBinding) ||
                !rotationBindings.TryGetValue(
                    "m_LocalRotation.w",
                    out var wBinding))
            {
                Debug.LogError(
                    $"动画 {animationClip.name} 缺少 Bip001 四元数曲线，无法移除水平根转向。",
                    animationClip);
                return CreateFallbackTurnCurve();
            }

            var xCurve = AnimationUtility.GetEditorCurve(animationClip, xBinding);
            var yCurve = AnimationUtility.GetEditorCurve(animationClip, yBinding);
            var zCurve = AnimationUtility.GetEditorCurve(animationClip, zBinding);
            var wCurve = AnimationUtility.GetEditorCurve(animationClip, wBinding);
            var keyTimes = xCurve.keys.Select(key => key.time)
                .Concat(yCurve.keys.Select(key => key.time))
                .Concat(zCurve.keys.Select(key => key.time))
                .Concat(wCurve.keys.Select(key => key.time))
                .Distinct()
                .OrderBy(time => time)
                .ToArray();
            if (keyTimes.Length < 2)
            {
                Debug.LogError(
                    $"动画 {animationClip.name} 的 Bip001 四元数关键帧不足，无法移除水平根转向。",
                    animationClip);
                return CreateFallbackTurnCurve();
            }

            var firstRotation = EvaluateRotation(
                xCurve,
                yCurve,
                zCurve,
                wCurve,
                keyTimes[0]);
            var inverseFirstRotation = Quaternion.Inverse(firstRotation);
            var cleanedXKeys = new Keyframe[keyTimes.Length];
            var cleanedYKeys = new Keyframe[keyTimes.Length];
            var cleanedZKeys = new Keyframe[keyTimes.Length];
            var cleanedWKeys = new Keyframe[keyTimes.Length];
            var turnKeys = new Keyframe[keyTimes.Length];
            var previousRawYaw = 0f;
            var unwrappedYaw = 0f;
            var previousProgress = 0f;
            var turnDirectionSign = 0f;
            var previousCleanedRotation = firstRotation;
            var clipDuration = Mathf.Max(0.0001f, keyTimes[^1]);

            for (var index = 0; index < keyTimes.Length; index++)
            {
                var time = keyTimes[index];
                var sourceRotation = EvaluateRotation(
                    xCurve,
                    yCurve,
                    zCurve,
                    wCurve,
                    time);
                var relativeRotation =
                    sourceRotation * inverseFirstRotation;
                var rawYaw = CalculateHorizontalTwist(relativeRotation);
                if (index > 0)
                {
                    unwrappedYaw += Mathf.DeltaAngle(
                        previousRawYaw,
                        rawYaw);
                }

                previousRawYaw = rawYaw;
                var horizontalRotation = Quaternion.AngleAxis(
                    unwrappedYaw,
                    Vector3.up);
                var cleanedRotation =
                    relativeRotation *
                    Quaternion.Inverse(horizontalRotation) *
                    firstRotation;
                var normalizedTime = time / clipDuration;
                if (index > 0 &&
                    Quaternion.Dot(
                        previousCleanedRotation,
                        cleanedRotation) < 0f)
                {
                    cleanedRotation = new Quaternion(
                        -cleanedRotation.x,
                        -cleanedRotation.y,
                        -cleanedRotation.z,
                        -cleanedRotation.w);
                }

                var progress = Mathf.Max(
                    previousProgress,
                    Mathf.Clamp01(Mathf.Abs(unwrappedYaw) / 180f));
                if (Mathf.Approximately(turnDirectionSign, 0f) &&
                    Mathf.Abs(unwrappedYaw) > 0.01f)
                {
                    turnDirectionSign = Mathf.Sign(unwrappedYaw);
                }

                cleanedXKeys[index] = new Keyframe(
                    time,
                    cleanedRotation.x);
                cleanedYKeys[index] = new Keyframe(
                    time,
                    cleanedRotation.y);
                cleanedZKeys[index] = new Keyframe(
                    time,
                    cleanedRotation.z);
                cleanedWKeys[index] = new Keyframe(
                    time,
                    cleanedRotation.w);
                turnKeys[index] = new Keyframe(
                    normalizedTime,
                    progress *
                    (Mathf.Approximately(turnDirectionSign, 0f)
                        ? 1f
                        : turnDirectionSign));
                previousProgress = progress;
                previousCleanedRotation = cleanedRotation;
            }

            SetCurveTangents(turnKeys);
            AnimationUtility.SetEditorCurve(
                animationClip,
                xBinding,
                CreateRotationCurve(cleanedXKeys, xCurve));
            AnimationUtility.SetEditorCurve(
                animationClip,
                yBinding,
                CreateRotationCurve(cleanedYKeys, yCurve));
            AnimationUtility.SetEditorCurve(
                animationClip,
                zBinding,
                CreateRotationCurve(cleanedZKeys, zCurve));
            AnimationUtility.SetEditorCurve(
                animationClip,
                wBinding,
                CreateRotationCurve(cleanedWKeys, wCurve));
            animationClip.EnsureQuaternionContinuity();
            return new AnimationCurve(turnKeys);
        }

        /// <summary>
        /// 在指定时间读取并归一化 Bip001 的局部四元数。
        /// </summary>
        /// <param name="xCurve">四元数 X 分量曲线。</param>
        /// <param name="yCurve">四元数 Y 分量曲线。</param>
        /// <param name="zCurve">四元数 Z 分量曲线。</param>
        /// <param name="wCurve">四元数 W 分量曲线。</param>
        /// <param name="time">需要采样的动画秒数。</param>
        /// <returns>归一化后的局部旋转。</returns>
        private static Quaternion EvaluateRotation(
            AnimationCurve xCurve,
            AnimationCurve yCurve,
            AnimationCurve zCurve,
            AnimationCurve wCurve,
            float time)
        {
            return new Quaternion(
                xCurve.Evaluate(time),
                yCurve.Evaluate(time),
                zCurve.Evaluate(time),
                wCurve.Evaluate(time)).normalized;
        }

        /// <summary>
        /// 从相对旋转中只提取绕父节点 Y 轴的 Twist 角度。
        /// Swing-Twist 分解不会把骨架根的俯仰、侧倾或非标准本地前轴误判成角色水平转身。
        /// </summary>
        /// <param name="relativeRotation">当前骨架根相对首帧的父空间旋转。</param>
        /// <returns>范围为负一百八十度到正一百八十度的水平角度。</returns>
        private static float CalculateHorizontalTwist(
            Quaternion relativeRotation)
        {
            var twistMagnitude = Mathf.Sqrt(
                relativeRotation.y * relativeRotation.y +
                relativeRotation.w * relativeRotation.w);
            if (twistMagnitude <= 0.000001f)
            {
                return 0f;
            }

            var twistY = relativeRotation.y / twistMagnitude;
            var twistW = relativeRotation.w / twistMagnitude;
            var angle = 2f * Mathf.Atan2(twistY, twistW) * Mathf.Rad2Deg;
            return float.IsFinite(angle)
                ? Mathf.DeltaAngle(0f, angle)
                : 0f;
        }

        /// <summary>
        /// 使用原曲线的循环方式创建新的四元数分量曲线。
        /// </summary>
        /// <param name="keys">已经移除水平 Twist 的四元数分量关键帧。</param>
        /// <param name="sourceCurve">提供循环方式的原始分量曲线。</param>
        /// <returns>可写回 TurnBack 动画副本的四元数分量曲线。</returns>
        private static AnimationCurve CreateRotationCurve(
            Keyframe[] keys,
            AnimationCurve sourceCurve)
        {
            return new AnimationCurve(keys)
            {
                preWrapMode = sourceCurve.preWrapMode,
                postWrapMode = sourceCurve.postWrapMode,
            };
        }

        /// <summary>
        /// 根据相邻关键帧计算转向进度曲线的连续切线。
        /// </summary>
        /// <param name="keys">按归一化时间排序的转向进度关键帧。</param>
        private static void SetCurveTangents(Keyframe[] keys)
        {
            for (var index = 0; index < keys.Length; index++)
            {
                var incomingTangent = index == 0
                    ? 0f
                    : CalculateLinearTangent(
                        keys[index - 1],
                        keys[index]);
                var outgoingTangent = index == keys.Length - 1
                    ? 0f
                    : CalculateLinearTangent(
                        keys[index],
                        keys[index + 1]);
                var key = keys[index];
                key.inTangent = incomingTangent;
                key.outTangent = outgoingTangent;
                keys[index] = key;
            }
        }

        /// <summary>
        /// 计算两个相邻转向关键帧之间的线性斜率。
        /// 相邻关键帧在同一段上使用相同斜率，可避免自动切线把零到一进度过冲成单帧九十度跳变。
        /// </summary>
        /// <param name="from">当前线性区间的起始关键帧。</param>
        /// <param name="to">当前线性区间的结束关键帧。</param>
        /// <returns>该区间每单位归一化时间的进度变化量。</returns>
        private static float CalculateLinearTangent(
            Keyframe from,
            Keyframe to)
        {
            var duration = to.time - from.time;
            return duration <= 0.0001f
                ? 0f
                : (to.value - from.value) / duration;
        }

        /// <summary>
        /// 在源动画曲线异常时提供可预测的安全转向节奏。
        /// </summary>
        /// <returns>在 TurnComplete 前完成旋转并保持到结尾的备用曲线。</returns>
        private static AnimationCurve CreateFallbackTurnCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.60f, 1f),
                new Keyframe(1f, 1f));
        }

        /// <summary>
        /// 将目录定义的循环规则写入可编辑动画副本，使 TransitionAsset 的循环状态与玩法配置保持一致。
        /// </summary>
        /// <param name="animationClip">需要更新循环设置的生成动画。</param>
        /// <param name="loop">是否在播放到结尾后继续循环。</param>
        private static void ConfigureLooping(AnimationClip animationClip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(animationClip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(animationClip, settings);
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
