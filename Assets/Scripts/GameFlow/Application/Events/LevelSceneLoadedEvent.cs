namespace Train.GameFlow.Application.Events
{
    /// <summary>
    /// 表示关卡场景已经加载完成。
    /// </summary>
    public readonly struct LevelSceneLoadedEvent
    {
        public LevelSceneLoadedEvent(string levelId, string sceneLocation)
        {
            LevelId = levelId;
            SceneLocation = sceneLocation;
        }

        public string LevelId { get; }
        public string SceneLocation { get; }
    }
}
