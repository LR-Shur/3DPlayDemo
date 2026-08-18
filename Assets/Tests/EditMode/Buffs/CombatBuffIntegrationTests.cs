using System.Collections.Generic;
using NUnit.Framework;
using Train.Buffs.Core;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Combat.Factions;
using Train.Gameplay.Combat.HitEffects;
using UnityEngine;

namespace Train.Tests.EditMode.Buffs
{
    /// <summary>
    /// 验证纯 C# Buff 领域层与 Unity 生命值、伤害入口和雷剑命中特效的连接。
    /// </summary>
    public sealed class CombatBuffIntegrationTests
    {
        private readonly List<GameObject> _createdObjects = new();

        /// <summary>
        /// 每个测试结束后销毁临时对象，避免污染场景和后续用例。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.DestroyImmediate(createdObject);
                }
            }

            _createdObjects.Clear();
        }

        /// <summary>
        /// 验证 DamageInfo 替换数值时会保留其余命中上下文。
        /// </summary>
        [Test]
        public void DamageInfo_WithAmount_PreservesContext()
        {
            var original = new DamageInfo(
                20f,
                null,
                new Vector3(1f, 2f, 3f),
                Vector3.forward,
                4f,
                DamageType.Electric);

            var modified = original.WithAmount(35f);

            Assert.That(modified.Amount, Is.EqualTo(35f));
            Assert.That(modified.HitPoint, Is.EqualTo(original.HitPoint));
            Assert.That(modified.HitDirection, Is.EqualTo(original.HitDirection));
            Assert.That(modified.ImpactForce, Is.EqualTo(4f));
            Assert.That(modified.DamageType, Is.EqualTo(DamageType.Electric));
        }

        /// <summary>
        /// 验证统一 DamageHandler 会读取目标 BuffHandle 并放大雷属性伤害。
        /// </summary>
        [Test]
        public void DamageHandler_ElectricDamage_UsesTargetBuffHandle()
        {
            var target = CreateTarget(500f, out var health, out var buffs);
            buffs.Apply(CreateLightningVulnerability(2));

            var result = DamageHandler.Apply(
                health,
                new DamageInfo(
                    100f,
                    new DamageSourceStub(CombatFaction.Player),
                    target.transform.position,
                    Vector3.forward,
                    damageType: DamageType.Electric),
                new FactionStub(CombatFaction.Enemy));

            Assert.That(result.AppliedDamage, Is.EqualTo(124f).Within(0.001f));
            Assert.That(health.CurrentHealth, Is.EqualTo(376f).Within(0.001f));
        }

        /// <summary>
        /// 验证雷易伤不会错误放大物理伤害。
        /// </summary>
        [Test]
        public void DamageHandler_PhysicalDamage_IgnoresLightningVulnerability()
        {
            var target = CreateTarget(500f, out var health, out var buffs);
            buffs.Apply(CreateLightningVulnerability(5));

            var result = DamageHandler.Apply(
                health,
                new DamageInfo(
                    100f,
                    new DamageSourceStub(CombatFaction.Player),
                    target.transform.position,
                    Vector3.forward,
                    damageType: DamageType.Physical),
                new FactionStub(CombatFaction.Enemy));

            Assert.That(result.AppliedDamage, Is.EqualTo(100f).Within(0.001f));
        }

        /// <summary>
        /// 验证雷剑每次有效命中叠一层，五层后下一次雷伤获得 60% 增幅。
        /// </summary>
        [Test]
        public void LightningWeaponHitEffect_FiveHits_StacksToSixtyPercent()
        {
            var target = CreateTarget(1000f, out var health, out var buffs);
            var weaponObject = new GameObject("TestLightningWeapon");
            _createdObjects.Add(weaponObject);
            var effect = weaponObject.AddComponent<LightningWeaponHitEffect>();
            var damageInfo = new DamageInfo(
                10f,
                null,
                Vector3.zero,
                Vector3.forward,
                damageType: DamageType.Electric);

            for (var index = 0; index < 5; index++)
            {
                effect.OnDamageApplied(
                    health,
                    damageInfo,
                    new DamageResult(10f, 990f, false));
            }

            Assert.That(
                buffs.Snapshot.Buffs[0].StackCount,
                Is.EqualTo(5));
            Assert.That(
                buffs.ModifyIncomingDamage(100f, DamageElement.Electric),
                Is.EqualTo(160f).Within(0.001f));
        }

        /// <summary>
        /// 验证 100 点防御会把普通入伤降低为一半。
        /// </summary>
        [Test]
        public void CombatStats_OneHundredDefense_HalvesNormalDamage()
        {
            var target = CreateTarget(500f, out var health, out _);
            var stats = target.AddComponent<CombatStatModifierComponent>();
            stats.SetDefense(100f);

            var result = DamageHandler.Apply(
                health,
                new DamageInfo(
                    100f,
                    new DamageSourceStub(CombatFaction.Player),
                    Vector3.zero,
                    Vector3.forward,
                    damageType: DamageType.Physical),
                new FactionStub(CombatFaction.Enemy));

            Assert.That(result.AppliedDamage, Is.EqualTo(50f).Within(0.001f));
        }

        /// <summary>
        /// 验证真实伤害不会被装备防御力减免。
        /// </summary>
        [Test]
        public void CombatStats_TrueDamage_BypassesDefense()
        {
            var target = CreateTarget(500f, out var health, out _);
            var stats = target.AddComponent<CombatStatModifierComponent>();
            stats.SetDefense(999f);

            var result = DamageHandler.Apply(
                health,
                new DamageInfo(
                    100f,
                    new DamageSourceStub(CombatFaction.Player),
                    Vector3.zero,
                    Vector3.forward,
                    damageType: DamageType.True),
                new FactionStub(CombatFaction.Enemy));

            Assert.That(result.AppliedDamage, Is.EqualTo(100f).Within(0.001f));
        }

        /// <summary>
        /// 创建带生命值和独立 BuffHandle 的临时目标。
        /// </summary>
        private GameObject CreateTarget(
            float maxHealth,
            out Health health,
            out BuffHandleComponent buffs)
        {
            var target = new GameObject("TestBuffTarget");
            _createdObjects.Add(target);
            health = target.AddComponent<Health>();
            health.SetMaxHealth(maxHealth);
            buffs = target.AddComponent<BuffHandleComponent>();
            return target;
        }

        /// <summary>
        /// 创建测试用雷易伤信息。
        /// </summary>
        private static BuffInfo CreateLightningVulnerability(int stacks)
        {
            return new BuffInfo(
                "lightning_vulnerability",
                "test.weapon",
                8f,
                0.12f,
                stacks,
                5);
        }

        private sealed class DamageSourceStub : IDamageSource, IFactionMember
        {
            public DamageSourceStub(CombatFaction faction)
            {
                Faction = faction;
            }

            public Transform SourceTransform => null;
            public Transform OwnerTransform => null;
            public float Damage => 100f;
            public DamageType DamageType => DamageType.Physical;
            public bool IsDamageActive => true;
            public CombatFaction Faction { get; }
        }

        private sealed class FactionStub : IFactionMember
        {
            public FactionStub(CombatFaction faction)
            {
                Faction = faction;
            }

            public CombatFaction Faction { get; }
        }
    }
}
