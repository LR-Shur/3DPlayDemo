using NUnit.Framework;
using Train.Buffs.Core;

namespace Train.Tests.EditMode.Buffs
{
    /// <summary>
    /// 验证元素装备使用的扩展 Buff 标识都能通过统一工厂创建。
    /// </summary>
    public sealed class EquipmentElementalEffectTests
    {
        [Test]
        public void EquipmentEffectIds_AreRegistered()
        {
            var registry = BuffFactoryRegistry.CreateWithBuiltIns();
            var ids = new[]
            {
                "heat_guard", "afterburn", "scorch", "phoenix_guard",
                "abyssal_guard", "flow", "mist", "tide_echo",
                "air_current", "zephyr", "gale_expose", "cyclone",
                "bastion", "quake_step", "fortify", "shatter"
            };

            foreach (var id in ids)
            {
                Assert.That(registry.Contains(id), Is.True, id);
                Assert.That(
                    registry.Create(new BuffInfo(id, "test", 1f, 0.1f, 1, 1)),
                    Is.Not.Null);
            }

            Assert.That(registry.Contains("earth_guard"), Is.True);
            Assert.That(
                registry.Create(new BuffInfo(
                    "earth_guard", "item.earth_guard_kit", 8f, -0.25f, 1, 1)),
                Is.Not.Null);
        }
    }
}
