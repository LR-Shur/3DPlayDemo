# Luban 配置工作流

项目使用 Luban 作为静态游戏数据的单一来源。表格源文件位于 `Assets/Config/Luban/Data`，结构定义位于 `Assets/Config/Luban/Defines`。

在项目根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/GenerateLubanConfig.ps1
```

脚本会完成两件事：

1. 生成 `Assets/Scripts/Composition/Config/Generated` 下的 C# 类型和表访问器。
2. 生成 `Assets/StreamingAssets/Config/LubanBytes` 下的客户端二进制数据。

运行时由 `LubanConfigService` 异步加载这些 `.bytes` 文件，并通过服务注册表暴露 `cfg.Tables`。场景点位仍然由 `EnemySpawnPoint` / `PlayerSpawnPoint` 负责摆放；关卡规则、敌人原型、物品、装备和奖励由 Luban 表维护。
