using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Komponente, die alle Objekte in der Szene verwaltet.
/// Sie aktualisiert die Positionen und Geschwindigkeiten der Objekte.
/// Außerdem zeichnet sie im Editor die Vorschau der Umlaufbahnen.
/// </summary>
public class Universe : MonoBehaviour
{
    // Anzahl der Vorschau Iterationen
    public int previewIterations = 1000;
    // Anzahl der gespeicherten Positionen in der Vergangenheit, für große Objekte
    public const int MAJOR_HISTORY_SIZE= 100_000;
    // Anzahl der gespeicherten Positionen in der Vergangenheit, für kleine Objekte
    public const int MINOR_HISTORY_SIZE = 50;
    // Offset Objekt für die Vorschau der Umlaufbahnen
    public string offsetObject = "";
    // Skalierungsfaktor für die Position der Objekte
    public const long SCALE_FACTOR = 1_000_000_000;
    // Skalierungsfaktor für die Größe der Objekte
    public const int SIZE_MULTIPLIER = 100_000;
    // Gravitationskonstante in m^3 kg^-1 s^-2
    public const double GRAVITATIONAL_CONSTANT = 6.6743e-11;
    // Zeitintervall für die Simulation in Sekunden
    public float timeStep = 1f;
    // Geschwindigkeit der Simulation
    public int simulationSpeed = 1;

    public CelestialObject[] objects;
    Dictionary<string, CelestialObject[]> filteredObjects;

    /// <summary>
    /// Wird beim Start des Programms aufgerufen.
    /// </summary>
    private void Awake()
    {
        // Lade alle Objekte in der Szene
        objects = GetComponentsInChildren<CelestialObject>();

        // Berechne alle relevanten Objekte für jedes Objekt.
        // Relevante Objekte sind alle Objekte, die eine Wirkung auf das aktuelle Objekt haben.
        // Dieser Filter sorgt zum Beispiel dafür, das Saturns Monde keinen Einfluss auf die Erde haben.
        // Dies ist wichtig, um die Geschwindigkeit der Simulation zu verbessern.
        // Die dabei verlorenen Kräfte sind zu vernachlässigen, da nur kleine und gleichzeitig weit entfernte Objekte gefiltert werden.
        filteredObjects = new();
        foreach (CelestialObject obj in objects)
        {
            List<CelestialObject> relevantObjects = new();
            foreach (CelestialObject other in objects)
            {
                // Ignore objects that are too far away and too small to have any effect
                if (Vector3d.Distance(obj.position, other.position) > 100_000_000 && other.radius < 2_000) continue;
                relevantObjects.Add(other);
            }
            filteredObjects[obj.name] = relevantObjects.ToArray();
        }
    }

    /// <summary>
    /// Wird 60 mal pro Sekunde aufgerufen.
    /// Berechnet die Geschwindigkeiten und Positionen aller Objekte.
    /// simulationSpeed gibt an, wie oft die Simulation pro fixed Frame aktualisiert wird.
    /// </summary>
    public void FixedUpdate()
    {
        for (int i = 0; i < simulationSpeed; i++)
        {
            foreach (CelestialObject obj in objects)
            {
                obj.UpdateVelocity(filteredObjects[obj.name], timeStep);
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

    /// <summary>
    /// Zeichnet die Vorschauf für die Umlaufbahnen der Objekte.
    /// Dies ist wird im Editor genutzt.
    /// </summary>
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Punkte der Umlaufbahnen
        Dictionary<string, List<Vector3d>> points = new();
        // Positionen der Objekte
        Dictionary<string, Vector3d> positions = new();
        // Geschwindigkeiten der Objekte
        Dictionary<string, Vector3d> velocities = new();
        // Objekte in der Szene
        CelestialObject[] objects = GetComponentsInChildren<CelestialObject>();

        // Initialisiere die Punkte, Positionen und Geschwindigkeiten
        foreach (CelestialObject obj in objects)
        {
            points[obj.name] = new();
            positions[obj.name] = obj.position;
            velocities[obj.name] = obj.velocity != Vector3d.Zero ? obj.velocity : obj.initialVelocity;
        }

        // Für die Anzahl der gewollten previewIterations, Simuliere die Umlaufbahnen
        for (int i = 1; i <= previewIterations; i++)
        {
            // Berechne die neue Geschwindigkeit der Objekte
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

            // Berechne die neue Position der Objekte
            foreach (CelestialObject obj in objects)
            {
                Vector3d velocity = velocities[obj.name];
                Vector3d position = positions[obj.name] + velocity * timeStep;
                positions[obj.name] = position;

                Vector3d relativeOffset = positions[obj.name] - (offsetObject != "" ? positions[offsetObject] : Vector3d.Zero);
                points[obj.name].Add(relativeOffset);
            }
        }

        // Zeichne die Umlaufbahnen der Objekte
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