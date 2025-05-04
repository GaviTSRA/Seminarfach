using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Klasse für einen Körper in der Simulation.
/// </summary>
public class CelestialObject : MonoBehaviour
{
    public double mass;
    public float radius;

    /// <summary>
    /// Startposition / Momentane Position
    /// </summary>
    public Vector3d position;
    /// <summary>
    /// Startgeschwindigkeit
    /// </summary>
    public Vector3d initialVelocity;

    /// <summary>
    /// Farbe der Umlaufbahn    
    /// </summary>
    public Color previewColor = Color.white;

    /// <summary>
    /// Momentane Geschwindigkeit
    /// </summary>
    [ReadOnly]
    public Vector3d velocity;

    /// <summary>
    /// Speichert die Positionen des Körpers in der Vergangenheit, für die Visualisierung
    /// </summary>
    private Queue<Vector3d> positions;

    /// <summary>
    /// Wird beim Starten des Programmes ausgeführt
    /// </summary>
    private void Awake()
    {
        velocity = initialVelocity;
        positions = new();
        // Aktualisiert die Größe des Körpers, wenn das Programm gestartet wird
        transform.localScale = Vector3.one * this.radius / Universe.SCALE_FACTOR * Universe.SIZE_MULTIPLIER;
    }

    /// <summary>
    /// Berechnet die neue Geschwindigkeit des Körpers, basierend auf der Gravitation aller anderen (relevanten) Körper.
    /// Siehe <see cref="Universe"/> für die Berechnung der Gravitation
    /// </summary>
    /// <param name="allObjects">Alle Objekte, die eine Wirkung auf diesen Körper haben</param>
    /// <param name="timeStep">Zeitintervall für die Berechnung</param>
    public void UpdateVelocity(CelestialObject[] allObjects, float timeStep)
    {
        foreach (CelestialObject other in allObjects)
        {
            if (other == this) continue;
            velocity += CalculateNewVelocity(this.position, other.position, this.mass, other.mass, timeStep);
        }
    }

    /// <summary>
    /// Berechnet die neue Geschwindigkeit des Körpers, basierend auf der Gravitation eines anderen Körpers.
    /// </summary>
    /// <param name="ownPosition">Eigene Position</param>
    /// <param name="otherPosition">Position des anderen Körpers</param>
    /// <param name="ownMass">Eigene Masse</param>
    /// <param name="otherMass">Masse des anderen Körpers</param>
    /// <param name="timeStep">Zeitintervall für die Berechnung</param>
    /// <returns>Neue Geschwindigkeit des Körpers</returns>
    public static Vector3d CalculateNewVelocity(Vector3d ownPosition, Vector3d otherPosition, double ownMass, double otherMass, float timeStep)
    {
        // Abstand der Körper quadriert: r^2
        double distanceSquared = (ownPosition - otherPosition).SqrMagnitude;

        // Objekte sind zu nahe beieinander (keine Kollision), Gravitation wäre unrealistisch hoch
        // Stattdessen wird die Geschwindigkeit auf 0 gesetzt
        if (distanceSquared <= 0.05)
        {
            return Vector3d.Zero;
        }

        // Abstand der Körper: r
        Vector3d delta = otherPosition - ownPosition;
        // Richtung der Kraft
        Vector3d forceDirection = delta.Normalized;
        // Wirkende Kraft: F = G * m1 * m2 / r^2
        Vector3d force = forceDirection * (Universe.GRAVITATIONAL_CONSTANT * ownMass * otherMass / distanceSquared);

        // Daraus resultierende Beschleunigung: a = F / m
        Vector3d acceleration = force / ownMass;
        // Neue Geschwindigkeit: v = a * dt
        return acceleration * timeStep;
    }

    /// <summary>
    /// Aktualisiert die Position des Körpers, basierend auf der aktuellen Geschwindigkeit und dem Zeitintervall
    /// </summary>
    /// <param name="timeStep">Zeitintervall</param>
    public void UpdatePosition(float timeStep)
    {
        this.position += this.velocity * timeStep;
    }

    /// <summary>
    /// Aktualisiert die angezeigte Position des Körpers, basierend auf der aktuellen Geschwindigkeit und dem Zeitintervall.
    /// Speichert außerdem die vorherige Position zur darstellung der Umlaufbahn
    /// </summary>
    public void SavePosition()
    {
        // Speichert die aktuelle Position für die Darstellung der Umlaufbahn
        positions.Enqueue(this.position);

        // Wenn zu viele Positionen gespeichert sind, wird die älteste gelöscht
        // Größere Körper (Planeten) speichern mehr Positionen
        if (positions.Count > (this.radius > 1100 ? Universe.MAJOR_HISTORY_SIZE : Universe.MINOR_HISTORY_SIZE))
        {
            positions.Dequeue();
        }

        // Aktualisiert die angezeigte Position des Körpers
        this.transform.position = (Vector3)this.position / Universe.SCALE_FACTOR;
    }

    /// <summary>
    /// Von Unity ausgeführt. Zeichnet die Umlaufbahn des Körpers.
    /// </summary>
    void OnDrawGizmos()
    {
        // Farbe der Umlaufbahn
        Gizmos.color = previewColor;
        // Name des Körpers als Label
        Handles.Label(transform.position + Vector3.up * (radius / Universe.SCALE_FACTOR), gameObject.name);

        // Wenn der Körper keine Positionen gespeichert hat, wird nichts gezeichnet
        if (positions == null || positions.Count == 0) return;

        // Zeichnet die Umlaufbahn des Körpers
        Vector3d lastPosition = positions.First();
        foreach (Vector3d position in positions)
        {
            Gizmos.DrawLine((Vector3) lastPosition / Universe.SCALE_FACTOR, (Vector3) position / Universe.SCALE_FACTOR);
            lastPosition = position;
        }
    }

    /// <summary>
    /// Von Unity ausgeführt. Aktualisiert die Position des Körpers im Editor.
    /// Dies ist nötig, da das Positionsfeld der CelestialObject-Komponente einen deutlich genaueren Wert nutzt als die Transform-Komponente von Unity.
    /// Wird der Wert der CelestialObject-Komponente verändert, wird die tatsächliche Position des Körpers durch diese Funktion aktualisiert.
    /// Die tatsächliche Position ist um einen großen Faktor verkleinert, um Genauigkeitsfehler zu vermeiden.
    /// Alle Berechnungen nutzen den genauen Wert der Celestial-Object-Komponente.
    /// </summary>
    private void OnValidate()
    {
        transform.position = (Vector3) (position / Universe.SCALE_FACTOR);
    }
}
