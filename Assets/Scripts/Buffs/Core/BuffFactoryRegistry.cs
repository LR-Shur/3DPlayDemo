using System;
using System.Collections.Generic;

namespace Train.Buffs.Core
{
    /// <summary>
    /// 按稳定 Buff 标识保存具体工厂的注册表。
    /// 注册发生在启动层，战斗代码只需调用 Create(BuffInfo)。
    /// </summary>
    public sealed class BuffFactoryRegistry
    {
        private readonly Dictionary<string, IBuffFactory> _factories =
            new Dictionary<string, IBuffFactory>(
                StringComparer.Ordinal);

        /// <summary>
        /// 当前已注册工厂数量。
        /// </summary>
        public int Count => _factories.Count;

        /// <summary>
        /// 注册一个 Buff 工厂；重复标识会直接失败，避免启动配置被静默覆盖。
        /// </summary>
        public void Register(IBuffFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (string.IsNullOrWhiteSpace(factory.BuffId))
            {
                throw new ArgumentException(
                    "Buff 工厂的稳定标识不能为空。",
                    nameof(factory));
            }

            if (_factories.ContainsKey(factory.BuffId))
            {
                throw new InvalidOperationException(
                    $"Buff 工厂 '{factory.BuffId}' 已经注册，不能重复注册。");
            }

            _factories.Add(factory.BuffId, factory);
        }

        /// <summary>
        /// 判断某个稳定标识是否已经注册。
        /// </summary>
        public bool Contains(string buffId)
        {
            ValidateBuffId(buffId);
            return _factories.ContainsKey(buffId);
        }

        /// <summary>
        /// 根据 BuffInfo 创建具体 Buff。
        /// 未注册标识会抛出包含标识的清晰异常，便于定位数据配置问题。
        /// </summary>
        public IBuff Create(BuffInfo info)
        {
            info.Validate();
            if (!_factories.TryGetValue(info.BuffId, out var factory))
            {
                throw new KeyNotFoundException(
                    $"没有为 Buff '{info.BuffId}' 注册工厂。请在启动层完成注册。");
            }

            var buff = factory.Create(info);
            ValidateCreatedBuff(info, buff);
            return buff;
        }

        /// <summary>
        /// 尝试根据 BuffInfo 创建具体 Buff。
        /// 返回 false 只代表标识未注册；无效参数或错误工厂仍会抛出异常。
        /// </summary>
        public bool TryCreate(
            BuffInfo info,
            out IBuff buff)
        {
            info.Validate();
            if (!_factories.TryGetValue(info.BuffId, out var factory))
            {
                buff = null;
                return false;
            }

            buff = factory.Create(info);
            ValidateCreatedBuff(info, buff);
            return true;
        }

        /// <summary>
        /// 创建并注册项目当前内置 Buff 的默认注册表。
        /// </summary>
        public static BuffFactoryRegistry CreateWithBuiltIns()
        {
            var registry = new BuffFactoryRegistry();
            registry.Register(
                new LightningVulnerabilityBuffFactory());
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.FireVulnerability,
                    DamageElement.Fire));
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.WaterVulnerability,
                    DamageElement.Water));
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.WindVulnerability,
                    DamageElement.Wind));
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.EarthVulnerability,
                    DamageElement.Earth));
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.Burning,
                    DamageElement.Fire));
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.Wet,
                    DamageElement.Electric));
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.WindMark,
                    DamageElement.Wind));
            registry.Register(
                new ElementalDamageModifierBuffFactory(
                    ElementalBuffIds.Fracture,
                    DamageElement.Physical));
            return registry;
        }

        private static void ValidateCreatedBuff(
            BuffInfo info,
            IBuff buff)
        {
            if (buff == null)
            {
                throw new InvalidOperationException(
                    $"Buff 工厂 '{info.BuffId}' 返回了 null。");
            }

            if (!string.Equals(
                    buff.BuffId,
                    info.BuffId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Buff 工厂 '{info.BuffId}' 创建了标识为 " +
                    $"'{buff.BuffId}' 的错误实例。");
            }

            if (!string.Equals(
                    buff.SourceId,
                    info.SourceId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Buff '{info.BuffId}' 创建后的来源标识 " +
                    $"'{buff.SourceId}' 与请求来源 '{info.SourceId}' 不一致。");
            }
        }

        private static void ValidateBuffId(string buffId)
        {
            if (string.IsNullOrWhiteSpace(buffId))
            {
                throw new ArgumentException(
                    "Buff 标识不能为空。",
                    nameof(buffId));
            }
        }
    }
}
