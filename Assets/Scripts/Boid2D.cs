using UnityEngine;

public class Boid2D : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 2f;

    [Header("Avoidance")]
    public float avoidanceRadius = 1f;
    public float avoidanceStrength = 2f;
    public float perceptionAngle = 270f;

    [Header("Alignment")]
    public float alignmentRadius = 2f;
    public float alignmentStrength = 1f;

    [Header("Cohesion")]
    public float cohesionRadius = 2f;
    public float cohesionStrength = 1f;

    [Header("Wall Avoidance (Raycast)")]
    public float rayLength = 1.5f;
    public int rayCount = 11;
    public float raySteerSpeed = 5f;
    public bool showDebugRays = false;

    private Vector2 velocity;
    private BoidManager manager;
    private Vector2 lastBestDir;
    private float[] lastRayDistances;

    public Vector2 Velocity => velocity;

    public void Initialize(BoidManager mgr, Vector2 initVel)
    {
        manager = mgr;
        velocity = initVel;
    }

    void Update()
    {
        ApplyAvoidance();
        ApplyAlignment();
        ApplyCohesion();
        ApplyWallAvoidance();
        ClampVelocityNearWalls();

        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        Vector2 pos = (Vector2)transform.position + velocity * Time.deltaTime;
        transform.position = pos;
        ApplyBoundary();

        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }

    private void ApplyAvoidance()
    {
        Vector2 avoidanceForce = Vector2.zero;
        Vector2 myPos = (Vector2)transform.position;

        foreach (var other in manager.AllBoids)
        {
            if (other == this) continue;

            Vector2 otherPos = (Vector2)other.transform.position;
            Vector2 dir = myPos - otherPos;
            float dist = dir.magnitude;

            if (dist < avoidanceRadius && dist > 0.01f)
            {
                Vector2 toOther = otherPos - myPos;
                float angle = Vector2.Angle(velocity.normalized, toOther);
                if (angle > perceptionAngle / 2f) continue;

                float ratio = 1f - dist / avoidanceRadius;
                avoidanceForce += ratio * dir.normalized;
            }
        }

        velocity += avoidanceForce * avoidanceStrength;
    }

    private void ApplyAlignment()
    {
        Vector2 avgDirection = Vector2.zero;
        int count = 0;
        Vector2 myPos = (Vector2)transform.position;

        foreach (var other in manager.AllBoids)
        {
            if (other == this) continue;

            Vector2 otherPos = (Vector2)other.transform.position;
            Vector2 toOther = otherPos - myPos;
            float dist = toOther.magnitude;

            if (dist < alignmentRadius && dist > 0.01f)
            {
                float angle = Vector2.Angle(velocity.normalized, toOther);
                if (angle > perceptionAngle / 2f) continue;

                avgDirection += other.Velocity.normalized;
                count++;
            }
        }

        if (count > 0)
        {
            avgDirection /= count;
            velocity += avgDirection * alignmentStrength;
        }
    }

    private void ApplyCohesion()
    {
        Vector2 sumPositions = Vector2.zero;
        int count = 0;
        Vector2 myPos = (Vector2)transform.position;

        foreach (var other in manager.AllBoids)
        {
            if (other == this) continue;

            Vector2 otherPos = (Vector2)other.transform.position;
            Vector2 toOther = otherPos - myPos;
            float dist = toOther.magnitude;

            if (dist < cohesionRadius && dist > 0.01f)
            {
                float angle = Vector2.Angle(velocity.normalized, toOther);
                if (angle > perceptionAngle / 2f) continue;

                sumPositions += otherPos;
                count++;
            }
        }

        if (count > 0)
        {
            Vector2 centerOfMass = sumPositions / count;
            Vector2 cohesionForce = (centerOfMass - myPos).normalized;
            float distToCenter = (centerOfMass - myPos).magnitude;
            float ratio = Mathf.Clamp01(distToCenter / cohesionRadius);
            velocity += cohesionForce * cohesionStrength * ratio;
        }
    }

    private void ApplyWallAvoidance()
    {
        if (velocity.sqrMagnitude < 0.01f) return;

        Vector2 pos = (Vector2)transform.position;
        Vector2 forward = velocity.normalized;
        float halfAngle = perceptionAngle / 2f;
        float step = perceptionAngle / (rayCount - 1);

        lastRayDistances = new float[rayCount];

        Vector2 bestDir = forward;
        float bestDist = -1f;
        bool foundClear = false;

        for (int i = 0; i < rayCount; i++)
        {
            float angleOffset = -halfAngle + step * i;
            Vector2 dir = RotateVector2(forward, angleOffset);
            float hitDist = CastRay(pos, dir, rayLength);
            lastRayDistances[i] = hitDist;

            if (!foundClear && hitDist >= rayLength)
            {
                bestDir = dir;
                bestDist = hitDist;
                foundClear = true;
            }
            else if (!foundClear && hitDist > bestDist)
            {
                bestDir = dir;
                bestDist = hitDist;
            }
        }

        lastBestDir = bestDir;
        Vector2 targetVel = bestDir * maxSpeed;
        velocity = Vector2.Lerp(velocity, targetVel, raySteerSpeed * Time.deltaTime);
    }

    private float CastRay(Vector2 origin, Vector2 direction, float maxDist)
    {
        Vector2 min = manager.BoundaryMin;
        Vector2 max = manager.BoundaryMax;
        float closest = maxDist;

        // 左墙 (x = min.x)
        if (direction.x < -0.0001f)
        {
            float t = (min.x - origin.x) / direction.x;
            if (t > 0.01f && t < closest)
            {
                float hitY = origin.y + direction.y * t;
                if (hitY >= min.y && hitY <= max.y)
                    closest = t;
            }
        }
        // 右墙 (x = max.x)
        if (direction.x > 0.0001f)
        {
            float t = (max.x - origin.x) / direction.x;
            if (t > 0.01f && t < closest)
            {
                float hitY = origin.y + direction.y * t;
                if (hitY >= min.y && hitY <= max.y)
                    closest = t;
            }
        }
        // 下墙 (y = min.y)
        if (direction.y < -0.0001f)
        {
            float t = (min.y - origin.y) / direction.y;
            if (t > 0.01f && t < closest)
            {
                float hitX = origin.x + direction.x * t;
                if (hitX >= min.x && hitX <= max.x)
                    closest = t;
            }
        }
        // 上墙 (y = max.y)
        if (direction.y > 0.0001f)
        {
            float t = (max.y - origin.y) / direction.y;
            if (t > 0.01f && t < closest)
            {
                float hitX = origin.x + direction.x * t;
                if (hitX >= min.x && hitX <= max.x)
                    closest = t;
            }
        }

        return closest;
    }

    private static Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void ClampVelocityNearWalls()
    {
        Vector2 pos = (Vector2)transform.position;
        Vector2 min = manager.BoundaryMin;
        Vector2 max = manager.BoundaryMax;
        float detectDist = rayLength;

        // 左墙
        float distLeft = pos.x - min.x;
        if (distLeft < detectDist && velocity.x < 0)
        {
            float maxToward = (distLeft / detectDist) * maxSpeed;
            if (velocity.x < -maxToward) velocity.x = -maxToward;
        }
        // 右墙
        float distRight = max.x - pos.x;
        if (distRight < detectDist && velocity.x > 0)
        {
            float maxToward = (distRight / detectDist) * maxSpeed;
            if (velocity.x > maxToward) velocity.x = maxToward;
        }
        // 下墙
        float distBottom = pos.y - min.y;
        if (distBottom < detectDist && velocity.y < 0)
        {
            float maxToward = (distBottom / detectDist) * maxSpeed;
            if (velocity.y < -maxToward) velocity.y = -maxToward;
        }
        // 上墙
        float distTop = max.y - pos.y;
        if (distTop < detectDist && velocity.y > 0)
        {
            float maxToward = (distTop / detectDist) * maxSpeed;
            if (velocity.y > maxToward) velocity.y = maxToward;
        }
    }

    public void ApplyBoundary()
    {
        Vector2 pos = transform.position;
        Vector2 min = manager.BoundaryMin;
        Vector2 max = manager.BoundaryMax;

        if (pos.x < min.x) { pos.x = min.x; velocity.x = 0; }
        else if (pos.x > max.x) { pos.x = max.x; velocity.x = 0; }
        if (pos.y < min.y) { pos.y = min.y; velocity.y = 0; }
        else if (pos.y > max.y) { pos.y = max.y; velocity.y = 0; }

        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugRays || lastRayDistances == null || manager == null) return;

        Vector2 pos = (Vector2)transform.position;
        Vector2 forward = velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector2.up;
        float halfAngle = perceptionAngle / 2f;
        float step = perceptionAngle / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angleOffset = -halfAngle + step * i;
            Vector2 dir = RotateVector2(forward, angleOffset);
            float hitDist = lastRayDistances[i];

            if (hitDist >= rayLength)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(pos, dir * rayLength);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(pos, dir * hitDist);
                Gizmos.DrawSphere(pos + dir * hitDist, 0.05f);
            }
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(pos, lastBestDir * rayLength);
    }
}
