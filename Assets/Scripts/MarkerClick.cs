using UnityEngine;

public class MarkerClick : MonoBehaviour
{
    public string cityId;
    public string displayName;
    public double longitude;
    public double latitude;
    public double flyToHeight = 2000000;

    void OnMouseDown()
    {
        Debug.Log("Clicked marker: " + displayName);

        var flyTo = FindObjectOfType<GlobeFlyToController>();
        if (flyTo == null)
        {
            Debug.LogWarning("No GlobeFlyToController found in scene.");
            return;
        }

        flyTo.FlyTo(longitude, latitude, flyToHeight);

        SendToFlutter.Send(JsonUtility.ToJson(new CityTapMessage
        {
            type = "cityTap",
            id = string.IsNullOrEmpty(cityId) ? gameObject.name : cityId
        }));
    }

    [ContextMenu("Test Click")]
    void TestClick()
    {
        Debug.Log("Clicked marker: " + gameObject.name);
    }

    [System.Serializable]
    public class CityTapMessage
    {
        public string type;
        public string id;
    }
}
