using UnityEngine;

public class MarkerClick : MonoBehaviour
{
    public double longitude;
    public double latitude;
    public double flyToHeight = 2000000;
    
     void OnMouseDown()
    {
        Debug.Log("Clicked marker: " + gameObject.name);

        var flyTo = FindObjectOfType<GlobeFlyToController>();
        if (flyTo == null)
        {
            Debug.LogWarning("No GlobeFlyToController found in scene.");
            return;
        }

        flyTo.FlyTo(longitude, latitude, flyToHeight);
    }

    [ContextMenu("Test Click")]
    void TestClick()
    {
        Debug.Log("Clicked marker: " + gameObject.name);
    }
    
}
