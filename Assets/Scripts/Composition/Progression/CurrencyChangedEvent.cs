namespace Train.Composition.Progression
{
    /// <summary>
    /// 金币变化事件；只传递结果，不把钱包对象暴露给 UI。
    /// </summary>
    public readonly struct CurrencyChangedEvent
    {
        public CurrencyChangedEvent(int coins, int delta, string reason)
        {
            Coins = coins;
            Delta = delta;
            Reason = reason;
        }

        public int Coins { get; }
        public int Delta { get; }
        public string Reason { get; }
    }
}
