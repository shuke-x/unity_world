using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;

public class MarkerManager : MonoBehaviour
{
    public GameObject markerPrefab;
    public float markerScale = 120000f;
    public bool useMockData = true;
    public bool enableMarkerClick = true;
    public bool createRuntimePin = true;

    void Start()
    {
        if (useMockData)
        {
            LoadPlaces(MockPlaceDataSource.LoadPlaces());
            return;
        }

        LoadPlacesFromFile();
    }

    void LoadPlacesFromFile()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("places");

        if (jsonFile == null)
        {
            Debug.LogError("Failed to load places.json from Resources folder.");
            return;
        }

        LoadPlacesFromJson(jsonFile.text);
    }

    public void LoadPlacesFromJson(string json)
    {
        LoadPlaces(JsonUtility.FromJson<PlaceList>(json));
    }

    void LoadPlaces(PlaceList list)
    {
        if (list == null || list.places == null)
        {
            Debug.LogError("No place data available.");
            return;
        }

        foreach (PlaceData place in list.places)
        {
            AddMarker(
                place.name,
                place.longitude,
                place.latitude,
                place.height
            );
        }
    }

    void AddMarker(string name, double lon, double lat, double height)
    {
        GameObject marker;

        if (createRuntimePin)
        {
            marker = CreateMapPin(name);
            marker.transform.SetParent(transform, false);
        }
        else
        {
            if (markerPrefab == null)
            {
                Debug.LogError("Marker prefab is not assigned.");
                return;
            }

            marker = Instantiate(markerPrefab, transform);
        }

        marker.name = name;
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localRotation = Quaternion.identity;
        marker.transform.localScale = Vector3.one * markerScale;

        var anchor = marker.GetComponent<CesiumGlobeAnchor>();
        if (anchor == null)
            anchor = marker.AddComponent<CesiumGlobeAnchor>();

        anchor.longitudeLatitudeHeight = new double3(
            lon,
            lat,
            height
        );

        var orientation = marker.GetComponent<GlobeMarkerOrientation>();
        if (orientation == null)
            marker.AddComponent<GlobeMarkerOrientation>();

        if (enableMarkerClick)
        {
            var click = marker.GetComponent<MarkerClick>();
            if (click == null)
                click = marker.AddComponent<MarkerClick>();

            click.longitude = lon;
            click.latitude = lat;
        }
    }

    GameObject CreateMapPin(string markerName)
    {
        GameObject pin = new GameObject(markerName);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Pin Head";
        head.transform.SetParent(pin.transform, false);
        head.transform.localPosition = new Vector3(0, 0.55f, 0);
        head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

        GameObject tip = new GameObject("Pin Tip");
        tip.name = "Pin Tip";
        tip.transform.SetParent(pin.transform, false);
        tip.transform.localPosition = Vector3.zero;
        tip.transform.localScale = new Vector3(0.38f, 0.9f, 0.38f);
        tip.AddComponent<MeshFilter>().sharedMesh = CreateConeMesh();
        tip.AddComponent<MeshRenderer>();

        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        center.name = "Pin Center";
        center.transform.SetParent(pin.transform, false);
        center.transform.localPosition = new Vector3(0, 0.58f, -0.03f);
        center.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);

        SetMarkerMaterial(head, new Color(0.9f, 0.05f, 0.03f));
        SetMarkerMaterial(tip, new Color(0.9f, 0.05f, 0.03f));
        SetMarkerMaterial(center, Color.white);

        SphereCollider collider = pin.AddComponent<SphereCollider>();
        collider.radius = 0.8f;
        collider.center = new Vector3(0, 0.35f, 0);

        return pin;
    }

    void SetMarkerMaterial(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;
        renderer.material = material;
    }

    Mesh CreateConeMesh()
    {
        const int segments = 32;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = new Vector3(0, -0.7f, 0);
        vertices[1] = new Vector3(0, 0.35f, 0);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 2] = new Vector3(
                Mathf.Cos(angle) * 0.5f,
                0.35f,
                Mathf.Sin(angle) * 0.5f
            );
        }

        int triangle = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i + 2;
            int next = i == segments - 1 ? 2 : current + 1;

            triangles[triangle++] = 0;
            triangles[triangle++] = next;
            triangles[triangle++] = current;

            triangles[triangle++] = 1;
            triangles[triangle++] = current;
            triangles[triangle++] = next;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Runtime Pin Cone";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

public class GlobeMarkerOrientation : MonoBehaviour
{
    private CesiumGeoreference georeference;
    private CesiumGlobeAnchor anchor;

    void Start()
    {
        georeference = FindObjectOfType<CesiumGeoreference>();
        anchor = GetComponent<CesiumGlobeAnchor>();
        AlignToGlobe();
    }

    void LateUpdate()
    {
        AlignToGlobe();
    }

    void AlignToGlobe()
    {
        if (georeference == null || anchor == null)
            return;

        double3 llh = anchor.longitudeLatitudeHeight;
        double3 surfaceEcef = georeference.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(
            new double3(llh.x, llh.y, 0)
        );
        double3 surfaceUnity = georeference.TransformEarthCenteredEarthFixedPositionToUnity(surfaceEcef);

        Vector3 surface = new Vector3(
            (float)surfaceUnity.x,
            (float)surfaceUnity.y,
            (float)surfaceUnity.z
        );
        Vector3 up = (transform.position - surface).normalized;

        if (up.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
    }
}
