# Asset Replacement Pipeline

目标：把当前程序生成的低模原型，逐步替换成可以上架演示的中模/高模资产。

## 当前策略

1. 先保留现有玩法和碰撞盒，避免换模型后重新破坏移动、躲藏、进食逻辑。
2. 每个家具先做中模版本：圆角、分件、明确材质、可辨认轮廓。
3. 贴图先从基础材质开始：木纹、布料、瓷器、金属、墙面污渍、食物残渣。
4. Unity 里只替换视觉模型，碰撞仍由代码里的简化盒子控制。

## 文件分层

- `Assets/Tools/Blender/create_environment_models.py`
  当前的 Blender 自动建模脚本，负责生成可直接导入 Unity 的 FBX。
- `Assets/Resources/Models/Environment/`
  Unity 实际读取的家具模型位置。
- `Assets/Art/Textures/`
  后续 Krita 或程序生成的贴图源文件和导出图。
- `Assets/Art/Source/`
  后续存放 `.blend`、`.kra` 等源文件。

## 下一批替换优先级

1. 沙发、床、餐桌：玩家经常贴近，最影响真实感。
2. 厨房：冰箱、灶台、洗手台、台面食物。
3. 人类和宠物：需要更好的身体比例、衣服、动作。
4. 墙面、地面、门、窗：增加真实住宅质感。
5. 食物残渣：需要贴图和更小颗粒，避免像彩色石头。

## 已开始替换

- 沙发、床、餐桌已经从纯方块推进到圆角、椭圆软垫、圆柱桌腿和分件模型。
- 厨房台面已经从场景方块替换为 `KitchenCounter_LowPoly.fbx`，包含柜门、抽屉、台面、背板、锅具、切菜板、刀、污渍和碎屑。

## 工具路径

- Blender: `D:\Blender\blender.exe`
- Krita: `D:\Krita (x64)\bin\krita.exe`
- Audacity: `D:\Audacity\audacity.exe`

## 工作规则

每次只替换一类资产，进 Unity 试玩确认没有悬浮、穿模、遮挡、卡住，再继续下一类。
