using UnityEngine;

public class BoundaryRenderer : MonoBehaviour
{
    [Header("Boundary")]
    public Vector2 boundarySize = new Vector2(50f, 50f);
    public Vector2 boundaryCenter = Vector2.zero;

    [Header("Visual")]
    public Color borderColor = Color.red;
    public float lineWidth = 0.05f;

    void Start()
    {
        CreateLines();
    }

    void CreateLines()
    {
        Vector2 min = boundaryCenter - boundarySize / 2f;
        Vector2 max = boundaryCenter + boundarySize / 2f;

        Vector2 bl = new Vector2(min.x, min.y);
        Vector2 br = new Vector2(max.x, min.y);
        Vector2 tr = new Vector2(max.x, max.y);
        Vector2 tl = new Vector2(min.x, max.y);

        CreateLine("Bottom", bl, br);
        CreateLine("Right", br, tr);
        CreateLine("Top", tr, tl);
        CreateLine("Left", tl, bl);
    }

    void CreateLine(string name, Vector2 start, Vector2 end)
    {
        GameObject obj = new GameObject(name);
        obj.transform.parent = transform;
        obj.transform.position = new Vector3(start.x, start.y, 0);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = borderColor;
        lr.endColor = borderColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.sortingOrder = 1;
    }
}
