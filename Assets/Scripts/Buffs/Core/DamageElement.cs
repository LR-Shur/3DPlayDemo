namespace Train.Buffs.Core
{
    /// <summary>
    /// 伤害的元素类型。元素只描述伤害属性，不负责阵营、暴击或生命值结算。
    /// </summary>
    public enum DamageElement
    {
        /// <summary>
        /// 物理伤害。
        /// </summary>
        Physical = 0,

        /// <summary>
        /// 火属性伤害。
        /// </summary>
        Fire = 1,

        /// <summary>
        /// 冰属性伤害。
        /// </summary>
        Ice = 2,

        /// <summary>
        /// 雷属性伤害。
        /// </summary>
        Electric = 3,

        /// <summary>水属性伤害。</summary>
        Water = 5,

        /// <summary>风属性伤害。</summary>
        Wind = 6,

        /// <summary>地属性伤害。</summary>
        Earth = 7,

        /// <summary>
        /// 真实伤害，通常跳过元素易伤和防御修正。
        /// </summary>
        True = 4
    }
}
