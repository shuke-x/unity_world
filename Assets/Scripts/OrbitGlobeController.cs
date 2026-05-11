using UnityEngine;
using UnityEngine.InputSystem;
using CesiumForUnity;
using Unity.Mathematics;

public class OrbitGlobeController : MonoBehaviour
{
    [Header("Cesium")]
    public CesiumGlobeAnchor cameraAnchor;
    public CesiumGeoreference georeference;

    [Header("Current Position")]
    public double longitude = 115;
    public double latitude = 0;
    public double height = 30000000;

    [Header("Startup View")]
    public bool startInOverview = true;
    public double overviewHeight = 50000000;
    public bool lookAtEarthCenter = true;
    public bool addStarfield = false;

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

    private double targetLongitude;
    private double targetLatitude;
    private double targetHeight;

    private Vector2 lastMousePosition;
    private Vector2 dragVelocity;
    private bool isDragging;

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
    }

    void Update()
    {
        HandleDrag();
        HandleZoom();
        SmoothMove();
        Apply();
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
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, 0.5f);
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 200000000f);

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
}
