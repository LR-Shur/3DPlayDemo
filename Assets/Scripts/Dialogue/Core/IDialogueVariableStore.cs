namespace Train.Dialogue.Core
{
    /// <summary>
    /// 为条件和命令提供与具体存档系统解耦的字符串变量读写能力。
    /// </summary>
    public interface IDialogueVariableStore
    {
        /// <summary>尝试读取一个稳定键对应的值。</summary>
        bool TryGetValue(string key, out string value);

        /// <summary>写入或覆盖一个稳定键对应的值。</summary>
        void SetValue(string key, string value);
    }
}
