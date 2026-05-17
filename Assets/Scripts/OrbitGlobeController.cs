using UnityEngine;
using UnityEngine.InputSystem;
using CesiumForUnity;
using Unity.Mathematics;
using System.Globalization;

public class OrbitGlobeController : MonoBehaviour
{
    [Header("Cesium")]
    public CesiumGlobeAnchor cameraAnchor;
    public CesiumGeoreference georeference;

    [Header("Current Position")]
    public double longitude = 115;
    public double latitude = 0;
    public double height = 18000000;

    [Header("Startup View")]
    public bool startInOverview = true;
    public double overviewHeight = 18000000;
    public bool lookAtEarthCenter = true;
    public bool transparentBackground = true;
    public bool addStarfield = false;

    [Header("View Framing")]
    public bool applyFieldOfView = true;
    public float fieldOfView = 45f;

    [Header("Limits")]
    public double minHeight = 3000;
    public double maxHeight = 60000000;
    public double minLatitude = -85;
    public double maxLatitude = 85;

    [Header("Speed")]
    public double rotateSpeed = 0.03;
    public double zoomSpeed = 300000;
    public float smoothSpeed = 10;
    public float dragSmoothing = 18f;
    public float inertiaDamping = 8f;

    [Header("Interaction Control")]
    public bool gesturesEnabled = true;
    public bool autoRotateEnabled = false;
    public double autoRotateSpeed = 2.0;
    public bool autoRotatePausesWhileDragging = true;

    [Header("Flutter Integration")]
    public double cityViewHeight = 1000000;
    public double poiViewHeight = 50000;
    public bool sendCameraUpdates = true;
    public float cameraUpdateInterval = 0.5f;

    private double targetLongitude;
    private double targetLatitude;
    private double targetHeight;

    private Vector2 lastMousePosition;
    private Vector2 dragVelocity;
    private bool isDragging;

    private float _cameraUpdateTimer;

    // ════════════════════════════════════════════════════════════
    //                  Flutter → Unity
    //   public + 一个 string 参数 = 可被 Flutter 调用
    // ════════════════════════════════════════════════════════════

    /// Flutter: sendToUnity('GlobeCamera', 'FocusOnCity', '{"id":"tokyo","lng":139.69,"lat":35.68}')
    public void FocusOnCity(string json)
    {
        Debug.Log($"[OrbitGlobe] FocusOnCity: {json}");
        try
        {
            var data = JsonUtility.FromJson<CityFocusData>(json);
            FlyTo(data.lng, data.lat, cityViewHeight);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FocusOnCity parse error: {e.Message}");
        }
    }

    public void FocusOnPoi(string json)
    {
        Debug.Log($"[OrbitGlobe] FocusOnPoi: {json}");
        try
        {
            var data = JsonUtility.FromJson<PoiFocusData>(json);
            FlyTo(data.lng, data.lat, poiViewHeight);

            SendMsgToFlutter("poi.focused", new PoiFocusedData { id = data.id });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FocusOnPoi parse error: {e.Message}");
        }
    }

    public void ResetView(string _ = "")
    {
        FlyTo(115, 0, overviewHeight);
    }

    public void SetCamera(string json)
    {
        try
        {
            var data = JsonUtility.FromJson<CameraData>(json);
            targetLongitude = NormalizeLongitude(data.lng);
            targetLatitude = Clamp(data.lat, minLatitude, maxLatitude);
            targetHeight = Clamp(data.height, minHeight, maxHeight);

            longitude = targetLongitude;
            latitude = targetLatitude;
            height = targetHeight;

            Apply();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SetCamera parse error: {e.Message}");
        }
    }

    public void SetCameraHeight(string value)
    {
        if (TryParseDoubleControl(value, out double nextHeight))
        {
            SetCameraHeightValue(nextHeight);
            return;
        }

        Debug.LogWarning($"SetCameraHeight parse error: {value}");
    }

    public void SetCameraHeightValue(double nextHeight)
    {
        targetHeight = Clamp(nextHeight, minHeight, maxHeight);
    }

    public void SetOverviewHeight(string value)
    {
        if (TryParseDoubleControl(value, out double nextHeight))
        {
            overviewHeight = Clamp(nextHeight, minHeight, maxHeight);
            return;
        }

        Debug.LogWarning($"SetOverviewHeight parse error: {value}");
    }

    public void SetFieldOfView(string value)
    {
        if (TryParseFloatControl(value, out float nextFieldOfView))
        {
            SetFieldOfViewValue(nextFieldOfView);
            return;
        }

        Debug.LogWarning($"SetFieldOfView parse error: {value}");
    }

    public void SetFieldOfViewValue(float nextFieldOfView)
    {
        Camera camera = GetComponent<Camera>();
        if (camera == null) return;

        applyFieldOfView = true;
        fieldOfView = Mathf.Clamp(nextFieldOfView, 15f, 90f);
        camera.fieldOfView = fieldOfView;
    }

    public void SetGesturesEnabledValue(bool enabled)
    {
        gesturesEnabled = enabled;

        if (!gesturesEnabled)
            ResetGestureState();
    }

    public void SetGesturesEnabled(string value)
    {
        if (TryParseBoolControl(value, out bool enabled))
        {
            SetGesturesEnabledValue(enabled);
            return;
        }

        Debug.LogWarning($"SetGesturesEnabled parse error: {value}");
    }

    public void EnableGestures(string _ = "")
    {
        SetGesturesEnabledValue(true);
    }

    public void DisableGestures(string _ = "")
    {
        SetGesturesEnabledValue(false);
    }

    public void SetAutoRotateEnabledValue(bool enabled)
    {
        autoRotateEnabled = enabled;
    }

    public void SetAutoRotateEnabled(string value)
    {
        if (TryParseBoolControl(value, out bool enabled))
        {
            SetAutoRotateEnabledValue(enabled);
            return;
        }

        Debug.LogWarning($"SetAutoRotateEnabled parse error: {value}");
    }

    public void EnableAutoRotate(string _ = "")
    {
        SetAutoRotateEnabledValue(true);
    }

    public void DisableAutoRotate(string _ = "")
    {
        SetAutoRotateEnabledValue(false);
    }

    public void SetAutoRotateSpeedValue(double speed)
    {
        autoRotateSpeed = speed;
    }

    public void SetAutoRotateSpeed(string value)
    {
        if (TryParseDoubleControl(value, out double speed))
        {
            SetAutoRotateSpeedValue(speed);
            return;
        }

        Debug.LogWarning($"SetAutoRotateSpeed parse error: {value}");
    }

    // ════════════════════════════════════════════════════════════
    //                  Unity → Flutter
    // ════════════════════════════════════════════════════════════

    public void SendCameraStateToFlutter()
    {
        SendMsgToFlutter("camera.changed", new CameraData
        {
            lng = (float)longitude,
            lat = (float)latitude,
            height = (float)height,
        });
    }

    /// 把结构化事件 (evt, data) 包成 envelope 发给 Flutter
    void SendMsgToFlutter(string evt, object data)
    {
        var dataJson = JsonUtility.ToJson(data);
        var envelope = "{\"evt\":\"" + evt + "\",\"data\":" + dataJson + "}";
        SendToFlutter.Send(envelope);   // ← unitypackage 提供的全局 SendToFlutter 类
    }

    void FlyTo(double lng, double lat, double targetH)
    {
        targetLongitude = NormalizeLongitude(lng);
        targetLatitude = Clamp(lat, minLatitude, maxLatitude);
        targetHeight = Clamp(targetH, minHeight, maxHeight);
    }

    // ════════════════════════════════════════════════════════════
    //               以下是你原有的代码，未改动
    // ════════════════════════════════════════════════════════════

    void Start()
    {
        if (cameraAnchor == null)
            cameraAnchor = GetComponent<CesiumGlobeAnchor>();

        if (georeference == null)
            georeference = FindObjectOfType<CesiumGeoreference>();

        ConfigureCameraForSpaceView();

        if (startInOverview)
        {
            maxHeight = System.Math.Max(maxHeight, overviewHeight);
            height = overviewHeight;
        }

        targetLongitude = longitude;
        targetLatitude = latitude;
        targetHeight = height;

        ApplyImmediate();

        // 通知 Flutter, Unity 已经准备好
        SendMsgToFlutter("ready", new ReadyData { version = "1.0.0" });
    }

    void Update()
    {
        if (gesturesEnabled)
        {
            HandleDrag();
            HandleZoom();
        }
        else
        {
            ResetGestureState();
        }

        HandleAutoRotate();
        SmoothMove();
        Apply();

        if (sendCameraUpdates)
        {
            _cameraUpdateTimer += Time.deltaTime;
            if (_cameraUpdateTimer >= cameraUpdateInterval)
            {
                _cameraUpdateTimer = 0;
                if (isDragging || dragVelocity.sqrMagnitude > 0.1f)
                {
                    SendCameraStateToFlutter();
                }
            }
        }
    }

    void HandleAutoRotate()
    {
        if (!autoRotateEnabled || System.Math.Abs(autoRotateSpeed) < 0.0001)
            return;

        if (autoRotatePausesWhileDragging && (isDragging || dragVelocity.sqrMagnitude > 0.01f))
            return;

        targetLongitude = NormalizeLongitude(targetLongitude + autoRotateSpeed * Time.deltaTime);
    }

    void HandleDrag()
    {
        if (TryReadPointer(out Vector2 pointerPosition, out bool pressedThisFrame, out bool isPressed))
        {
            if (pressedThisFrame)
            {
                lastMousePosition = pointerPosition;
                dragVelocity = Vector2.zero;
                isDragging = true;
                return;
            }

            if (isPressed && isDragging)
            {
                Vector2 delta = pointerPosition - lastMousePosition;
                Vector2 clampedDelta = Vector2.ClampMagnitude(delta, 35f);
                float smoothing = 1f - Mathf.Exp(-dragSmoothing * Time.deltaTime);

                dragVelocity = Vector2.Lerp(dragVelocity, clampedDelta, smoothing);
                ApplyDragDelta(dragVelocity);

                lastMousePosition = pointerPosition;
                return;
            }
        }

        isDragging = false;

        if (dragVelocity.sqrMagnitude > 0.01f)
        {
            ApplyDragDelta(dragVelocity);
            dragVelocity = Vector2.Lerp(
                dragVelocity,
                Vector2.zero,
                1f - Mathf.Exp(-inertiaDamping * Time.deltaTime)
            );
        }
    }

    void ResetGestureState()
    {
        isDragging = false;
        dragVelocity = Vector2.zero;
    }

    bool TryReadPointer(out Vector2 position, out bool pressedThisFrame, out bool isPressed)
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.isPressed || touch.press.wasPressedThisFrame)
            {
                position = touch.position.ReadValue();
                pressedThisFrame = touch.press.wasPressedThisFrame;
                isPressed = touch.press.isPressed;
                return true;
            }
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            isPressed = Mouse.current.leftButton.isPressed;
            return pressedThisFrame || isPressed;
        }

        position = Vector2.zero;
        pressedThisFrame = false;
        isPressed = false;
        return false;
    }

    void ApplyDragDelta(Vector2 delta)
    {
        double heightFactor = Mathf.Lerp(0.35f, 1.4f, Mathf.InverseLerp(
            (float)minHeight,
            (float)maxHeight,
            (float)targetHeight
        ));

        targetLongitude -= delta.x * rotateSpeed * heightFactor;
        targetLatitude -= delta.y * rotateSpeed * heightFactor;

        targetLatitude = Clamp(targetLatitude, minLatitude, maxLatitude);
        targetLongitude = NormalizeLongitude(targetLongitude);
    }

    void HandleZoom()
    {
        double zoomDelta = 0;

        if (Mouse.current != null)
            zoomDelta += Mouse.current.scroll.ReadValue().y;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                zoomDelta += 1;

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                zoomDelta -= 1;
        }

        if (System.Math.Abs(zoomDelta) > 0.01)
        {
            double zoomFactor = System.Math.Max(0.1, targetHeight / 30000000.0);
            targetHeight -= zoomDelta * zoomSpeed * zoomFactor;
            targetHeight = Clamp(targetHeight, minHeight, maxHeight);
        }
    }

    void SmoothMove()
    {
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        longitude = Mathf.LerpAngle(
            (float)longitude,
            (float)targetLongitude,
            t
        );

        latitude = LerpDouble(latitude, targetLatitude, t);
        height = LerpDouble(height, targetHeight, t);

        latitude = Clamp(latitude, minLatitude, maxLatitude);
        longitude = NormalizeLongitude(longitude);
        height = Clamp(height, minHeight, maxHeight);
    }

    void Apply()
    {
        if (cameraAnchor == null || georeference == null) return;

        cameraAnchor.longitudeLatitudeHeight = new double3(
            longitude,
            latitude,
            height
        );

        LookAtSurfacePoint();
    }

    void ApplyImmediate()
    {
        longitude = NormalizeLongitude(longitude);
        latitude = Clamp(latitude, minLatitude, maxLatitude);
        height = Clamp(height, minHeight, maxHeight);

        targetLongitude = longitude;
        targetLatitude = latitude;
        targetHeight = height;

        Apply();
    }

    void LookAtSurfacePoint()
    {
        Vector3 target;

        if (lookAtEarthCenter)
        {
            double3 centerUnity = georeference.TransformEarthCenteredEarthFixedPositionToUnity(double3.zero);
            target = new Vector3(
                (float)centerUnity.x,
                (float)centerUnity.y,
                (float)centerUnity.z
            );
        }
        else
        {
            double3 targetLlh = new double3(longitude, latitude, 0);

            double3 targetEcef =
                georeference.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(targetLlh);

            double3 targetUnity =
                georeference.TransformEarthCenteredEarthFixedPositionToUnity(targetEcef);

            target = new Vector3(
                (float)targetUnity.x,
                (float)targetUnity.y,
                (float)targetUnity.z
            );
        }

        Vector3 direction = target - transform.position;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, GetCameraUp(direction.normalized, target));
    }

    Vector3 GetCameraUp(Vector3 forward, Vector3 target)
    {
        double3 northPoleEcef = georeference.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(
            new double3(0, 90, 0)
        );

        double3 northPoleUnity = georeference.TransformEarthCenteredEarthFixedPositionToUnity(northPoleEcef);
        Vector3 north = new Vector3(
            (float)northPoleUnity.x,
            (float)northPoleUnity.y,
            (float)northPoleUnity.z
        );

        Vector3 up = Vector3.ProjectOnPlane(north - target, forward);
        if (up.sqrMagnitude < 0.001f)
            up = Vector3.ProjectOnPlane(Vector3.up, forward);

        return up.normalized;
    }

    void ConfigureCameraForSpaceView()
    {
        Camera camera = GetComponent<Camera>();
        if (camera == null) return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = transparentBackground ? new Color(0f, 0f, 0f, 0f) : Color.black;
        if (applyFieldOfView)
            camera.fieldOfView = Mathf.Clamp(fieldOfView, 15f, 90f);
        camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, 0.5f);
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 200000000f);
        RenderSettings.skybox = null;

        if (addStarfield && GetComponent<StarfieldBackground>() == null)
            gameObject.AddComponent<StarfieldBackground>();
    }

    double NormalizeLongitude(double value)
    {
        while (value > 180) value -= 360;
        while (value < -180) value += 360;
        return value;
    }

    double Clamp(double value, double min, double max)
    {
        return System.Math.Max(min, System.Math.Min(max, value));
    }

    double LerpDouble(double a, double b, float t)
    {
        return a + (b - a) * t;
    }

    bool TryParseBoolControl(string value, out bool result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = true;
            return true;
        }

        string normalized = value.Trim().Trim('"').ToLowerInvariant();

        if (normalized == "true" || normalized == "1" || normalized == "on" ||
            normalized == "yes" || normalized == "enable" || normalized == "enabled")
        {
            result = true;
            return true;
        }

        if (normalized == "false" || normalized == "0" || normalized == "off" ||
            normalized == "no" || normalized == "disable" || normalized == "disabled")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    bool TryParseDoubleControl(string value, out double result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return false;
        }

        string normalized = value.Trim().Trim('"');
        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result
        );
    }

    bool TryParseFloatControl(string value, out float result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return false;
        }

        string normalized = value.Trim().Trim('"');
        return float.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result
        );
    }

    // ════════════════════════════════════════════════════════════
    //                 数据契约（与 Flutter 端约定）
    // ════════════════════════════════════════════════════════════

    [System.Serializable]
    public class CityFocusData
    {
        public string id;
        public float lng;
        public float lat;
    }

    [System.Serializable]
    public class PoiFocusData
    {
        public string id;
        public float lng;
        public float lat;
    }

    [System.Serializable]
    public class CameraData
    {
        public float lng;
        public float lat;
        public float height;
    }

    [System.Serializable]
    public class PoiFocusedData
    {
        public string id;
    }

    [System.Serializable]
    public class ReadyData
    {
        public string version;
    }
}
