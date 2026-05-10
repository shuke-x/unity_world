using UnityEngine;

public class MarkerClick : MonoBehaviour
{
    public double longitude;
    public double latitude;
    
     void OnMouseDown()
    {
        Debug.Log("Clicked marker: " + gameObject.name);

        var flyTo = FindObjectOfType<GlobeFlyToController>();
        flyTo.FlyTo(longitude, latitude, 5000);
    }

    [ContextMenu("Test Click")]
    void TestClick()
    {
        Debug.Log("Clicked marker: " + gameObject.name);
    }
    
}
