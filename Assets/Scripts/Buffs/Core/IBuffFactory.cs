namespace Train.Buffs.Core
{
    /// <summary>
    /// 一个具体 Buff 类型的创建工厂。
    /// 工厂将稳定标识映射为实现类，使调用层只需提交 BuffInfo。
    /// </summary>
    public interface IBuffFactory
    {
        /// <summary>
        /// 此工厂能够创建的 Buff 稳定标识。
        /// </summary>
        string BuffId { get; }

        /// <summary>
        /// 按施加信息创建一个新的具体 Buff。
        /// </summary>
        IBuff Create(BuffInfo info);
    }
}
