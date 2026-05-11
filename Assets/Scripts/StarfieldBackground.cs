using UnityEngine;

[RequireComponent(typeof(Camera))]
public class StarfieldBackground : MonoBehaviour
{
    public Color backgroundColor = Color.black;

    void Start()
    {
        Camera camera = GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;
        RenderSettings.skybox = null;
    }
}
