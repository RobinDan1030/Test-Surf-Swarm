using UnityEngine;
using System.Collections.Generic;

public class BoidManager : MonoBehaviour
{
    [Header("Boid Prefab")]
    public GameObject boidPrefab;

    [Header("Boundary")]
    public Vector2 boundarySize = new Vector2(10f, 10f);
    public Vector2 boundaryCenter = Vector2.zero;

    [Header("Initial")]
    public int initialBoidCount = 50;

    private List<Boid2D> allBoids = new List<Boid2D>();
    private Vector2 boundaryMin;
    private Vector2 boundaryMax;

    public List<Boid2D> AllBoids => allBoids;
    public Vector2 BoundaryMin => boundaryMin;
    public Vector2 BoundaryMax => boundaryMax;

    void Start()
    {
        boundaryMin = boundaryCenter - boundarySize / 2f;
        boundaryMax = boundaryCenter + boundarySize / 2f;
        SpawnBoids(initialBoidCount);
    }

    void SpawnBoids(int count)
    {
        for (int i = 0; i < count; i++)
            SpawnSingleBoid();
    }

    public Boid2D SpawnSingleBoid()
    {
        Vector2 pos = new Vector2(
            Random.Range(boundaryMin.x + 2f, boundaryMax.x - 2f),
            Random.Range(boundaryMin.y + 2f, boundaryMax.y - 2f)
        );
        Vector2 vel = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 2f;

        GameObject obj = Instantiate(boidPrefab, pos, Quaternion.identity);
        obj.SetActive(true);
        Boid2D boid = obj.GetComponent<Boid2D>();
        boid.Initialize(this, vel);
        allBoids.Add(boid);
        return boid;
    }

    void Update()
    {
    }

    public void SetBoidCount(int count)
    {
        while (allBoids.Count < count)
            SpawnSingleBoid();

        while (allBoids.Count > count)
        {
            Boid2D last = allBoids[allBoids.Count - 1];
            allBoids.RemoveAt(allBoids.Count - 1);
            Destroy(last.gameObject);
        }
    }

    public void UpdateSettings(float maxSpeed)
    {
        foreach (var boid in allBoids)
        {
            if (maxSpeed >= 0) boid.maxSpeed = maxSpeed;
        }
    }
}
