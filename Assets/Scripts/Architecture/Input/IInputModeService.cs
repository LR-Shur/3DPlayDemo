using System;

namespace Train.Architecture.Input
{
    /// <summary>
    /// 协调模态界面与游戏输入。
    /// 每个模态界面持有一份租约，仅当最后一份租约释放后才恢复游戏输入。
    /// </summary>
    public interface IInputModeService
    {
        /// <summary>
        /// 获取当前是否至少有一个模态界面处于打开状态。
        /// </summary>
        bool IsModalActive { get; }

        /// <summary>
        /// 为指定界面申请一份模态输入租约。
        /// </summary>
        /// <param name="owner">便于诊断的租约持有者名称。</param>
        /// <returns>关闭界面时需要释放的租约。</returns>
        IDisposable AcquireModal(string owner);
    }
}
