using UnityEngine;

[RequireComponent(typeof(Camera))]
public class StarfieldBackground : MonoBehaviour
{
    public Color backgroundColor = new Color(0f, 0f, 0f, 0f);

    void Start()
    {
        Camera camera = GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;
        RenderSettings.skybox = null;
    }
}
