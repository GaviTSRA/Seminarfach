using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CelestialObject : MonoBehaviour
{
    public double mass;
    public float radius;

    public Vector3d position;
    public Vector3d initialVelocity;

    [ReadOnly]
    public Vector3d velocity;

    public Color previewColor = Color.white;

    private Queue<Vector3d> positions;

    private void Awake()
    {
        velocity = initialVelocity;
        positions = new();
    }

    public void UpdateVelocity(CelestialObject[] allObjects, float timeStep)
    {
        foreach (CelestialObject other in allObjects)
        {
            if (other == this) continue;
            velocity += CalculateNewVelocity(this.position, other.position, this.mass, other.mass, timeStep);
        }
    }

    public static Vector3d CalculateNewVelocity(Vector3d ownPosition, Vector3d otherPosition, double ownMass, double otherMass, float timeStep)
    {
        // F = G * ((m1 * m2) / r^2)
        double distanceSquared = (ownPosition - otherPosition).SqrMagnitude;
        Vector3d delta = otherPosition - ownPosition;

        if (distanceSquared <= 0.05)
        {
            // Too close, TODO
            return Vector3d.Zero;
        }

        Vector3d forceDirection = delta.Normalized;
        Vector3d force = forceDirection * (Universe.GRAVITATIONAL_CONSTANT * ownMass * otherMass / distanceSquared);

        // F = m * a -> a = F / m
        Vector3d acceleration = force / ownMass;
        return acceleration * timeStep;
    }

    public void UpdatePosition(float timeStep)
    {
        this.position += this.velocity * timeStep;
    }

    public void SavePosition()
    {
        positions.Enqueue(this.position);

        if (positions.Count > Universe.HISTORY_SIZE)
        {
            positions.Dequeue();
        }
        this.transform.position = (Vector3)this.position / Universe.SCALE_FACTOR;

    }

    void OnDrawGizmos()
    {
        Gizmos.color = previewColor;
        Gizmos.DrawSphere(transform.position, this.radius / Universe.SCALE_FACTOR * Universe.SIZE_MULTIPLIER);
        Handles.Label(transform.position + Vector3.up * (radius + 1), gameObject.name);

        if (positions == null || positions.Count == 0) return;
        Vector3d lastPosition = positions.First();
        foreach (Vector3d position in positions)
        {
            Gizmos.DrawLine((Vector3) lastPosition / Universe.SCALE_FACTOR, (Vector3) position / Universe.SCALE_FACTOR);
            lastPosition = position;
        }
    }

    private void OnValidate()
    {
        transform.position = (Vector3) (position / Universe.SCALE_FACTOR);
    }
}
