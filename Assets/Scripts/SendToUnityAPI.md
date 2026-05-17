# Flutter sendToUnity 调用清单

本文档整理当前项目里 Flutter 可以通过 `sendToUnity` 调用的 Unity 方法。

通用格式：

```dart
sendToUnity('Unity对象名', 'Unity方法名', '字符串参数');
```

注意：

- `sendToUnity` 的第三个参数建议始终传字符串；没有参数时传空字符串 `''`。
- JSON 参数用 `jsonEncode(...)` 生成，避免手写 JSON 时引号转义出错。
- 地球相机控制对象名是 `GlobeOverviewCamera`。
- Marker 数据管理对象名是 `MarkerManager`。

## 地球相机控制

目标对象：

```dart
const globeObject = 'GlobeOverviewCamera';
```

### FocusOnCity

```dart
// 聚焦到城市，经纬度来自 Flutter；高度使用 Unity 里的 cityViewHeight。
sendToUnity(
  globeObject,
  'FocusOnCity',
  jsonEncode({
    'id': 'tokyo',
    'lng': 139.6917,
    'lat': 35.6895,
  }),
);
```

参数说明：

- `id`：城市 ID，当前 Unity 侧只接收，不参与相机计算。
- `lng`：目标经度。
- `lat`：目标纬度。

### FocusOnPoi

```dart
// 聚焦到 POI，经纬度来自 Flutter；高度使用 Unity 里的 poiViewHeight。
// Unity 完成接收后会回发 poi.focused 事件给 Flutter。
sendToUnity(
  globeObject,
  'FocusOnPoi',
  jsonEncode({
    'id': 'poi_001',
    'lng': 116.397,
    'lat': 39.908,
  }),
);
```

参数说明：

- `id`：POI ID，Unity 会在 `poi.focused` 事件中回传。
- `lng`：目标经度。
- `lat`：目标纬度。

### ResetView

```dart
// 重置到默认全貌视角；默认高度由 overviewHeight 控制。
sendToUnity(globeObject, 'ResetView', '');
```

### SetCamera

```dart
// 一次性设置相机经度、纬度和高度。
// height 越小，地球看起来越大；height 越大，地球看起来越小。
sendToUnity(
  globeObject,
  'SetCamera',
  jsonEncode({
    'lng': 115,
    'lat': 0,
    'height': 18000000,
  }),
);
```

参数说明：

- `lng`：相机目标经度，会自动归一化到 `-180` 到 `180`。
- `lat`：相机目标纬度，会被限制在 Unity 的 `minLatitude` 到 `maxLatitude`。
- `height`：相机高度，会被限制在 Unity 的 `minHeight` 到 `maxHeight`。

### SetCameraHeight

```dart
// 只调整相机高度，用于外部控制地球显示大小。
sendToUnity(globeObject, 'SetCameraHeight', '12000000');
```

参数说明：

- 数值越小，地球越大。
- 数值越大，地球越小。
- 推荐全貌范围：`12000000` 到 `25000000`。

### SetOverviewHeight

```dart
// 修改 ResetView 使用的默认全貌高度。
sendToUnity(globeObject, 'SetOverviewHeight', '18000000');
```

参数说明：

- 只改变默认全貌高度，不会立刻移动相机。
- 修改后调用 `ResetView` 会使用新的高度。

### SetFieldOfView

```dart
// 调整相机视角；数值越小越像放大，数值越大看到范围越广。
sendToUnity(globeObject, 'SetFieldOfView', '45');
```

参数说明：

- Unity 侧会限制在 `15` 到 `90`。
- 推荐值：`35` 到 `55`。

### SetGesturesEnabled

```dart
// 禁用手势，Flutter 外部接管相机控制时使用。
sendToUnity(globeObject, 'SetGesturesEnabled', 'false');

// 启用手势，允许用户拖拽和缩放地球。
sendToUnity(globeObject, 'SetGesturesEnabled', 'true');
```

参数说明：

- 可传：`true`、`false`、`1`、`0`、`on`、`off`、`enable`、`disable`。
- 禁用时会清掉当前拖拽惯性，避免镜头继续滑动。

### EnableGestures

```dart
// 快捷启用手势。
sendToUnity(globeObject, 'EnableGestures', '');
```

### DisableGestures

```dart
// 快捷禁用手势。
sendToUnity(globeObject, 'DisableGestures', '');
```

### SetAutoRotateEnabled

```dart
// 开启地球自动自转。
sendToUnity(globeObject, 'SetAutoRotateEnabled', 'true');

// 关闭地球自动自转。
sendToUnity(globeObject, 'SetAutoRotateEnabled', 'false');
```

参数说明：

- 可传：`true`、`false`、`1`、`0`、`on`、`off`、`enable`、`disable`。
- 开启后会持续改变目标经度，让地球自动转动。

### EnableAutoRotate

```dart
// 快捷开启自转。
sendToUnity(globeObject, 'EnableAutoRotate', '');
```

### DisableAutoRotate

```dart
// 快捷关闭自转。
sendToUnity(globeObject, 'DisableAutoRotate', '');
```

### SetAutoRotateSpeed

```dart
// 设置自转速度，单位近似为每秒经度变化量。
sendToUnity(globeObject, 'SetAutoRotateSpeed', '2');
```

参数说明：

- 正数：向一个方向自转。
- 负数：向反方向自转。
- `0`：等同于停止自转效果。
- 推荐值：`0.5` 到 `5`。

## Marker 数据控制

目标对象：

```dart
const markerManagerObject = 'MarkerManager';
```

### LoadPlacesFromJson

```dart
// 从 Flutter 动态传入地点列表，Unity 会清空旧 Marker 并重新生成。
sendToUnity(
  markerManagerObject,
  'LoadPlacesFromJson',
  jsonEncode({
    'places': [
      {
        'id': 'tokyo',
        'name': 'Tokyo',
        'longitude': 139.6917,
        'latitude': 35.6895,
        'height': 1000,
      },
      {
        'id': 'bali',
        'name': 'Bali',
        'longitude': 115.1889,
        'latitude': -8.4095,
        'height': 1000,
      },
    ],
  }),
);
```

参数说明：

- `places`：地点数组。
- `id`：Marker ID；为空时 Unity 会使用 `name`。
- `name`：Marker 显示/对象名称；为空时 Unity 会使用 `id`。
- `longitude`：Marker 经度。
- `latitude`：Marker 纬度。
- `height`：Marker 高度。

## 常用组合

### 初始化为只展示地球、禁用手势、开启慢速自转

```dart
// 初始化展示完整地球。
sendToUnity(globeObject, 'SetCameraHeight', '18000000');

// 禁用 Unity 内部手势，避免和 Flutter 外层手势冲突。
sendToUnity(globeObject, 'DisableGestures', '');

// 设置慢速自转。
sendToUnity(globeObject, 'SetAutoRotateSpeed', '1');

// 开启自转。
sendToUnity(globeObject, 'EnableAutoRotate', '');
```

### 用户进入详情页时拉近到某个城市

```dart
// 关闭自转，避免聚焦后镜头继续移动。
sendToUnity(globeObject, 'DisableAutoRotate', '');

// 聚焦城市。
sendToUnity(
  globeObject,
  'FocusOnCity',
  jsonEncode({
    'id': 'tokyo',
    'lng': 139.6917,
    'lat': 35.6895,
  }),
);
```

### 用户退出详情页时恢复全貌

```dart
// 回到默认全貌视角。
sendToUnity(globeObject, 'ResetView', '');

// 重新开启自转。
sendToUnity(globeObject, 'EnableAutoRotate', '');
```

## Unity 回 Flutter 事件

这些不是 Flutter 主动 `sendToUnity`，而是 Unity 主动回传给 Flutter 的事件。

### ready

```json
{
  "evt": "ready",
  "data": {
    "version": "1.0.0"
  }
}
```

中文说明：

- Unity 场景和地球控制器已经初始化完成。
- Flutter 建议等收到这个事件后，再发送初始化配置。

### camera.changed

```json
{
  "evt": "camera.changed",
  "data": {
    "lng": 115,
    "lat": 0,
    "height": 18000000
  }
}
```

中文说明：

- 用户拖拽或惯性移动时，Unity 定时回传相机状态。
- 回传间隔由 `cameraUpdateInterval` 控制。

### poi.focused

```json
{
  "evt": "poi.focused",
  "data": {
    "id": "poi_001"
  }
}
```

中文说明：

- Flutter 调用 `FocusOnPoi` 后，Unity 会回传当前聚焦的 POI ID。
