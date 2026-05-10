using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;

public class MarkerManager : MonoBehaviour
{
    public GameObject markerPrefab;

    void Start()
    {
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
        PlaceList list = JsonUtility.FromJson<PlaceList>(json);

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
        GameObject marker = Instantiate(markerPrefab);

        marker.name = name;

        marker.transform.SetParent(transform, false);

        var anchor = marker.AddComponent<CesiumGlobeAnchor>();

        anchor.longitudeLatitudeHeight = new double3(
            lon,
            lat,
            height
        );

        // 点击
        // var click = marker.AddComponent<MarkerClick>();

        // click.longitude = lon;
        // click.latitude = lat;
    }
}
