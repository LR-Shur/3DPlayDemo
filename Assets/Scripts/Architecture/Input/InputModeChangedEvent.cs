namespace Train.Architecture.Input
{
    /// <summary>
    /// 表示模态界面数量或游戏输入阻塞状态发生了变化。
    /// </summary>
    public readonly struct InputModeChangedEvent
    {
        /// <summary>
        /// 创建一条输入模式变化消息。
        /// </summary>
        /// <param name="isModalActive">当前是否存在模态界面。</param>
        /// <param name="modalCount">当前持有中的模态租约数量。</param>
        public InputModeChangedEvent(bool isModalActive, int modalCount)
        {
            IsModalActive = isModalActive;
            ModalCount = modalCount;
        }

        /// <summary>
        /// 获取当前是否存在模态界面。
        /// </summary>
        public bool IsModalActive { get; }

        /// <summary>
        /// 获取当前持有中的模态租约数量。
        /// </summary>
        public int ModalCount { get; }
    }
}
