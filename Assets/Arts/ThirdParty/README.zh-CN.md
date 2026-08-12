# 第三方素材准备区

本目录只存放已下载、尚未接入场景和运行逻辑的素材包。导入前请先解压到对应子目录，并在 Unity 中检查模型、贴图和动画的导入设置。

## 素材清单

| 分类 | 文件 | 来源 | 许可 | 用途 |
| --- | --- | --- | --- | --- |
| UI | `UI/Kenney_Adventure/kenney_ui_pack_adventure.zip` | [Kenney UI Pack - Adventure](https://opengameart.org/content/ui-pack-adventure) | CC0 | 面板、按钮、HUD、勾选和关闭图标 |
| UI | `UI/Kenney_Pixel/kenney_pixel_ui_pack.zip` | [Kenney Pixel UI Pack](https://opengameart.org/content/pixel-ui-pack-750-assets) | CC0 | 背包格子、滚动条、光标和状态条 |
| 武器 | `Weapons/Kenney_Blaster/kenney_blaster_kit.zip` | [Kenney Blaster Kit](https://opengameart.org/content/blaster-kit) | CC0 | 远程武器、弹匣、目标和战斗道具 |
| 场景 | `Environment/Kenney_Factory/kenney_factory_kit.zip` | [Kenney Factory Kit](https://opengameart.org/content/factory-kit) | CC0 | 工厂、仓库、流水线和工业装饰 |
| 场景 | `Environment/Kenney_Nature/kenney_nature_kit.zip` | [Kenney Nature Kit](https://opengameart.org/node/83240) | CC0 | 植物、岩石、地形和环境填充 |
| 敌人 | `Enemies/Kenney_Robot/robot_enemy_pack.zip` | [Robot Enemy Pack](https://opengameart.org/content/robot-enemy-pack) | CC0 | 5 个机器人敌人和移动、攻击、死亡动画 |
| 敌人/NPC | `Enemies/Stylized_Humanoid/stylized_humanoid_base.7z` | [Base Rigged Stylized Humanoid](https://opengameart.org/content/base-rigged-stylized-humanoid-character-yw) | CC0 | 可改造的人形 NPC/敌人基础模型 |

## 使用说明

当前只完成素材收集，没有改动任何预制体、场景或配置表。后续接入时建议按以下顺序：

1. 先解压并确认模型/贴图/动画可以在 Unity 中正常预览。
2. 再为武器和敌人建立项目内 Prefab，补齐碰撞体、动画器和现有接口。
3. 最后把 UI 图片复制到项目的 UI 资源目录，逐个替换现有占位图并截图校验。

下载时间：2026-08-12。素材包保留原始压缩文件，便于重新导入和追溯来源。
