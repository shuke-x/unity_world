using UnityEngine;

public class OverviewLookAtEarth : MonoBehaviour
{
    void LateUpdate()
    {
        transform.LookAt(Vector3.zero);
    }
}
