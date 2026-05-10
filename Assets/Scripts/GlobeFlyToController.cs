using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Collections;

public class GlobeFlyToController : MonoBehaviour
{
    public CesiumGlobeAnchor cameraAnchor;

    public void FlyTo(double lon, double lat, double height)
    {
        StopAllCoroutines();
        StartCoroutine(FlyToCoroutine(lon, lat, height));
    }

    IEnumerator FlyToCoroutine(double lon, double lat, double height)
    {
        double3 start = cameraAnchor.longitudeLatitudeHeight;
        double3 end = new double3(lon, lat, height);

        float duration = 1.5f;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / duration;

            double3 current = math.lerp(start, end, t);

            cameraAnchor.longitudeLatitudeHeight = current;

            yield return null;
        }

        cameraAnchor.longitudeLatitudeHeight = end;
    }
}
