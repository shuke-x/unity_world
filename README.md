# unity_world

一个基于 Unity 和 Cesium for Unity 的 3D 地球浏览项目。

当前项目已经实现的内容只有仓库里实际落地的部分：

- 使用 Cesium 加载地球场景
- 使用 `Resources/places.json` 读取地点数据
- 运行时在指定经纬度生成地标 Marker
- 鼠标拖拽环绕地球
- 鼠标滚轮或 `W` / `S` 键控制相机远近
- 相机始终朝向当前经纬度对应的地表点
- 预留了点击 Marker 后飞行到目标点的脚本能力，但当前代码里没有接上

## 运行环境

- Unity `6000.4.5f1`
- Universal Render Pipeline
- Input System
- Cesium for Unity

项目当前的主场景已配置为：

- `Assets/Scenes/SampleScene.unity`

## 项目结构

只列当前实际参与功能的目录和文件。

```text
Assets/
  Mock/
    PlaceData.cs
    PlaceList.cs
  Resources/
    places.json
  Scenes/
    SampleScene.unity
  Scripts/
    GlobeFlyToController.cs
    MarkerClick.cs
    MarkerManager.cs
    OrbitGlobeController.cs
    OverviewLookAtEarth.cs
Packages/
  manifest.json
ProjectSettings/
```

## 已实现功能说明

### 1. 地点数据加载

文件：

- [Assets/Resources/places.json](/Users/shuke/first3D/Assets/Resources/places.json)
- [Assets/Mock/PlaceData.cs](/Users/shuke/first3D/Assets/Mock/PlaceData.cs)
- [Assets/Mock/PlaceList.cs](/Users/shuke/first3D/Assets/Mock/PlaceList.cs)

`places.json` 中维护地点列表，当前格式如下：

```json
{
  "places": [
    {
      "name": "Tokyo",
      "longitude": 139.6917,
      "latitude": 35.6895,
      "height": 1000
    }
  ]
}
```

字段说明：

- `name`：地点名称
- `longitude`：经度
- `latitude`：纬度
- `height`：高度

### 2. Marker 生成

文件：

- [Assets/Scripts/MarkerManager.cs](/Users/shuke/first3D/Assets/Scripts/MarkerManager.cs)

`MarkerManager` 在 `Start()` 中从 `Resources.Load<TextAsset>("places")` 读取地点数据，并为每个地点实例化一个 `markerPrefab`。

每个 Marker 会被挂上 `CesiumGlobeAnchor`，并把经纬高写入：

```csharp
anchor.longitudeLatitudeHeight = new double3(lon, lat, height);
```

这样 Marker 会直接定位到地球上的真实坐标。

### 3. 地球环绕与缩放

文件：

- [Assets/Scripts/OrbitGlobeController.cs](/Users/shuke/first3D/Assets/Scripts/OrbitGlobeController.cs)

`OrbitGlobeController` 是当前主要的相机控制脚本。

已实现的交互：

- 按住鼠标左键拖拽：改变经纬度，实现环绕地球
- 鼠标滚轮：缩放相机高度
- `W` 键：拉近
- `S` 键：拉远

当前脚本还做了这些限制与处理：

- 纬度限制在 `-85` 到 `85`
- 高度限制在 `3000` 到 `50000000`
- 经度自动归一化到 `-180` 到 `180`
- 使用平滑插值过渡相机位置
- 每帧根据当前目标点重新朝向地表

默认初始观察参数：

- `longitude = 115`
- `latitude = 0`
- `height = 30000000`

### 4. 相机飞行到目标点

文件：

- [Assets/Scripts/GlobeFlyToController.cs](/Users/shuke/first3D/Assets/Scripts/GlobeFlyToController.cs)

`GlobeFlyToController` 提供了：

- `FlyTo(double lon, double lat, double height)`

这个方法会在 `1.5` 秒内，把相机从当前位置插值飞行到目标经纬高。

当前状态：

- 这个脚本已经写好
- 但默认 Marker 创建时没有自动挂载点击逻辑
- 所以项目当前运行时并不会因为点击 Marker 而触发飞行

### 5. Marker 点击脚本

文件：

- [Assets/Scripts/MarkerClick.cs](/Users/shuke/first3D/Assets/Scripts/MarkerClick.cs)

`MarkerClick` 的逻辑是：

- 点击物体时输出日志
- 查找场景中的 `GlobeFlyToController`
- 调用 `FlyTo(longitude, latitude, 5000)`

但是在 [Assets/Scripts/MarkerManager.cs](/Users/shuke/first3D/Assets/Scripts/MarkerManager.cs) 中，这部分接线当前被注释掉了：

```csharp
// var click = marker.AddComponent<MarkerClick>();
// click.longitude = lon;
// click.latitude = lat;
```

所以这项能力目前属于“脚本已存在，但默认未启用”。

### 6. 始终朝向地球中心

文件：

- [Assets/Scripts/OverviewLookAtEarth.cs](/Users/shuke/first3D/Assets/Scripts/OverviewLookAtEarth.cs)

这个脚本每帧执行：

```csharp
transform.LookAt(Vector3.zero);
```

适用于需要始终看向地球中心的对象。

## 当前地点数据

当前仓库里的 `places.json` 只有两个地点：

- Tokyo
- Bali

如果需要增加地点，只需要编辑：

- [Assets/Resources/places.json](/Users/shuke/first3D/Assets/Resources/places.json)

保持字段结构不变即可。

## 依赖说明

项目里当前实际声明的核心依赖见：

- [Packages/manifest.json](/Users/shuke/first3D/Packages/manifest.json)

和当前功能直接相关的依赖主要有：

- `com.cesium.unity`
- `com.unity.inputsystem`
- `com.unity.render-pipelines.universal`

需要注意的一点：

`com.cesium.unity` 当前不是从公开 Registry 拉取，而是写成了本地文件路径：

```json
"com.cesium.unity": "file:/Users/shuke/Downloads/com.cesium.unity-1.21.0.tgz"
```

这意味着：

- 这个项目在你的机器上可以正常解析该包
- 其他人直接克隆仓库后，如果本机没有同一路径的 `.tgz` 文件，Unity 会缺少 Cesium 依赖

## 如何打开项目

1. 使用 Unity Hub 打开项目根目录
2. 确认 Unity 版本为 `6000.4.5f1`
3. 等待包解析完成
4. 打开场景 `Assets/Scenes/SampleScene.unity`
5. 点击 Play 运行

## 当前交互方式

- 鼠标左键拖拽：旋转浏览地球
- 鼠标滚轮：缩放
- `W`：拉近
- `S`：拉远

## 当前限制

只写当前仓库里已经明确存在的问题或限制：

- Marker 点击飞行逻辑默认未接入
- 地点数据目前只来自本地 `places.json`
- `com.cesium.unity` 依赖的是本地 `.tgz` 文件路径，不适合直接给其他机器开箱即用
