using NUnit.Framework;
using Train.Buffs.Core;

namespace Train.Tests.EditMode.Buffs
{
    /// <summary>
    /// 验证火、水、风、地元素共用同一套 Buff 工厂和伤害修正规则。
    /// </summary>
    public sealed class ElementalBuffTests
    {
        [Test]
        public void BuiltInRegistry_ContainsAllElementalBuffs()
        {
            var registry = BuffFactoryRegistry.CreateWithBuiltIns();

            Assert.That(registry.Contains(ElementalBuffIds.FireVulnerability), Is.True);
            Assert.That(registry.Contains(ElementalBuffIds.WaterVulnerability), Is.True);
            Assert.That(registry.Contains(ElementalBuffIds.WindVulnerability), Is.True);
            Assert.That(registry.Contains(ElementalBuffIds.EarthVulnerability), Is.True);
            Assert.That(registry.Contains(ElementalBuffIds.Burning), Is.True);
            Assert.That(registry.Contains(ElementalBuffIds.Wet), Is.True);
            Assert.That(registry.Contains(ElementalBuffIds.WindMark), Is.True);
            Assert.That(registry.Contains(ElementalBuffIds.Fracture), Is.True);
        }

        [Test]
        public void FireVulnerability_OnlyAmplifiesFireDamage()
        {
            var handle = new BuffHandle(
                BuffFactoryRegistry.CreateWithBuiltIns());
            handle.Apply(
                new BuffInfo(
                    ElementalBuffIds.FireVulnerability,
                    "weapon.fire",
                    5f,
                    0.2f,
                    2,
                    3));

            Assert.That(
                handle.ModifyIncomingDamage(
                    new DamageContext(100f, DamageElement.Fire)).Amount,
                Is.EqualTo(140f).Within(0.0001f));
            Assert.That(
                handle.ModifyIncomingDamage(
                    new DamageContext(100f, DamageElement.Wind)).Amount,
                Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void Wet_AmplifiesElectricDamageForFutureConductiveReaction()
        {
            var handle = new BuffHandle(
                BuffFactoryRegistry.CreateWithBuiltIns());
            handle.Apply(
                new BuffInfo(
                    ElementalBuffIds.Wet,
                    "weapon.water",
                    5f,
                    0.15f,
                    1,
                    1));

            Assert.That(
                handle.ModifyIncomingDamage(
                    new DamageContext(100f, DamageElement.Electric)).Amount,
                Is.EqualTo(115f).Within(0.0001f));
        }

        [Test]
        public void ElementalBuffs_StackPerSourceWithoutSharingState()
        {
            var first = new BuffHandle(
                BuffFactoryRegistry.CreateWithBuiltIns());
            var second = new BuffHandle(
                BuffFactoryRegistry.CreateWithBuiltIns());
            var info = new BuffInfo(
                ElementalBuffIds.EarthVulnerability,
                "weapon.earth",
                4f,
                0.1f,
                1,
                5);

            first.Apply(info);
            first.Apply(info);

            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(second.Count, Is.Zero);
            Assert.That(
                first.ModifyIncomingDamage(
                    new DamageContext(100f, DamageElement.Earth)).Amount,
                Is.EqualTo(120f).Within(0.0001f));
        }
    }
}
