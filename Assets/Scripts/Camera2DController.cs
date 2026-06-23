using UnityEngine;

public class Camera2DController : MonoBehaviour
{
    [Header("References")]
    public BoidManager boidManager;

    [Header("Camera")]
    public float orthoSize = 28f;

    void Start()
    {
        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);

        if (boidManager != null)
        {
            Vector2 center = (boidManager.BoundaryMin + boidManager.BoundaryMax) / 2f;
            transform.position = new Vector3(center.x, center.y, -10);
        }
        else
        {
            transform.position = new Vector3(0, 0, -10);
        }
    }
}
