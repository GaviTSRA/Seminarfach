using System.Collections.Generic;
using UnityEngine;

public class Universe : MonoBehaviour
{
    public int previewIterations = 1000;
    public const int HISTORY_SIZE= 1_000;
    public string offsetObject = "";
    public const int SCALE_FACTOR = 1_000_000_000;
    public const int SIZE_MULTIPLIER = 100_000;
    public const double GRAVITATIONAL_CONSTANT = 6.6743e-11;
    public float timeStep = 1f;
    public int simulationSpeed = 1;

    public CelestialObject[] objects;

    private void Awake()
    {
        objects = GetComponentsInChildren<CelestialObject>();
    }

    public void FixedUpdate()
    {
        for (int i = 0; i < simulationSpeed; i++)
        {
            foreach (CelestialObject obj in objects)
            {
                obj.UpdateVelocity(objects, timeStep);
            }

            foreach (CelestialObject obj in objects)
            {
                obj.UpdatePosition(timeStep);
            }
        }

        foreach (CelestialObject obj in objects)
        {
            obj.SavePosition();
        }
    }

    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR

        Dictionary<string, List<Vector3d>> points = new();
        Dictionary<string, Vector3d> positions = new();
        Dictionary<string, Vector3d> velocities = new();
        CelestialObject[] objects = GetComponentsInChildren<CelestialObject>();

        foreach (CelestialObject obj in objects)
        {
            points[obj.name] = new();
            positions[obj.name] = obj.position;
            velocities[obj.name] = obj.velocity != Vector3d.Zero ? obj.velocity : obj.initialVelocity;
        }

        for (int i = 1; i <= previewIterations; i++)
        {
            Dictionary<string, Vector3> newVelocities = new();
            foreach (CelestialObject obj in objects)
            {
                Vector3d velocity = velocities[obj.name];
                foreach (CelestialObject other in objects)
                {
                    if (other == obj) continue;

                    velocity += CelestialObject.CalculateNewVelocity(positions[obj.name], positions[other.name], obj.mass, other.mass, timeStep);
                }
                velocities[obj.name] = velocity;
            }

            foreach (CelestialObject obj in objects)
            {
                Vector3d velocity = velocities[obj.name];
                Vector3d position = positions[obj.name] + velocity * timeStep;
                positions[obj.name] = position;

                Vector3d relativeOffset = positions[obj.name] - (offsetObject != "" ? positions[offsetObject] : Vector3d.Zero);
                points[obj.name].Add(relativeOffset);
            }
        }

        Vector3d offset = (offsetObject != "" ? points[offsetObject][0] : Vector3d.Zero) / Universe.SCALE_FACTOR;
        foreach (CelestialObject obj in objects)
        {
            Gizmos.color = obj.previewColor;
            List<Vector3d> data = points[obj.name];
            if (data.Count == 0) continue;

            Vector3d previousPosition = data[0] - offset;

            foreach (Vector3d position in data)
            {
                Vector3d relativePosition = position - offset;
                Gizmos.DrawLine((Vector3) previousPosition / Universe.SCALE_FACTOR, (Vector3) relativePosition / Universe.SCALE_FACTOR);
                previousPosition = relativePosition;
            }

            points[obj.name] = data;
        }
        #endif
    }
}