namespace Train.Buffs.Core
{
    /// <summary>
    /// 统一维护元素 Buff 的稳定 ID，避免装备、技能和配置表直接散落魔法字符串。
    /// </summary>
    public static class ElementalBuffIds
    {
        /// <summary>火属性易伤。</summary>
        public const string FireVulnerability = "fire_vulnerability";

        /// <summary>水属性易伤。</summary>
        public const string WaterVulnerability = "water_vulnerability";

        /// <summary>风属性易伤。</summary>
        public const string WindVulnerability = "wind_vulnerability";

        /// <summary>地属性易伤。</summary>
        public const string EarthVulnerability = "earth_vulnerability";

        /// <summary>燃烧标记；当前阶段表现为火伤增幅，后续可扩展为持续伤害。</summary>
        public const string Burning = "burning";

        /// <summary>潮湿标记；当前阶段提高雷属性传导伤害。</summary>
        public const string Wet = "wet";

        /// <summary>风痕标记；提高风属性伤害。</summary>
        public const string WindMark = "wind_mark";

        /// <summary>破碎标记；提高物理伤害。</summary>
        public const string Fracture = "fracture";
    }
}
