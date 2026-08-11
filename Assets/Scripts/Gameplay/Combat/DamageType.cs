namespace Train.Gameplay.Combat
{
    /// <summary>
    /// 伤害类型。后续可据此扩展护甲、抗性和元素反应。
    /// </summary>
    public enum DamageType
    {
        Physical,
        Fire,
        Ice,
        Electric,
        True = 4,
        Water = 5,
        Wind = 6,
        Earth = 7,
    }
}
