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

    [Header("Limits")]
    public double minHeight = 3000;
    public double maxHeight = 50000000;
    public double minLatitude = -85;
    public double maxLatitude = 85;

    [Header("Speed")]
    public double rotateSpeed = 0.03;
    public double zoomSpeed = 300000;
    public float smoothSpeed = 10;

    private double targetLongitude;
    private double targetLatitude;
    private double targetHeight;

    private Vector2 lastMousePosition;

    void Start()
    {
        if (cameraAnchor == null)
            cameraAnchor = GetComponent<CesiumGlobeAnchor>();

        if (georeference == null)
            georeference = FindObjectOfType<CesiumGeoreference>();

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
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 current = Mouse.current.position.ReadValue();
            Vector2 delta = current - lastMousePosition;

            double dx = Mathf.Clamp(delta.x, -20f, 20f);
            double dy = Mathf.Clamp(delta.y, -20f, 20f);

            targetLongitude -= dx * rotateSpeed;
            targetLatitude += dy * rotateSpeed;

            targetLatitude = Clamp(targetLatitude, minLatitude, maxLatitude);
            targetLongitude = NormalizeLongitude(targetLongitude);

            lastMousePosition = current;
        }
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
            targetHeight -= zoomDelta * zoomSpeed;
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
        double3 targetLlh = new double3(longitude, latitude, 0);

        double3 targetEcef =
            georeference.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(targetLlh);

        double3 targetUnity =
            georeference.TransformEarthCenteredEarthFixedPositionToUnity(targetEcef);

        Vector3 target = new Vector3(
            (float)targetUnity.x,
            (float)targetUnity.y,
            (float)targetUnity.z
        );

        Vector3 direction = target - transform.position;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, transform.up);
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
