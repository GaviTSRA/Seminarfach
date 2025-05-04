using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Klasse f�r einen K�rper in der Simulation.
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
    /// Speichert die Positionen des K�rpers in der Vergangenheit, f�r die Visualisierung
    /// </summary>
    private Queue<Vector3d> positions;

    /// <summary>
    /// Wird beim Starten des Programmes ausgef�hrt
    /// </summary>
    private void Awake()
    {
        velocity = initialVelocity;
        positions = new();
        // Aktualisiert die Gr��e des K�rpers, wenn das Programm gestartet wird
        transform.localScale = Vector3.one * this.radius / Universe.SCALE_FACTOR * Universe.SIZE_MULTIPLIER;
    }

    /// <summary>
    /// Berechnet die neue Geschwindigkeit des K�rpers, basierend auf der Gravitation aller anderen (relevanten) K�rper.
    /// Siehe <see cref="Universe"/> f�r die Berechnung der Gravitation
    /// </summary>
    /// <param name="allObjects">Alle Objekte, die eine Wirkung auf diesen K�rper haben</param>
    /// <param name="timeStep">Zeitintervall f�r die Berechnung</param>
    public void UpdateVelocity(CelestialObject[] allObjects, float timeStep)
    {
        foreach (CelestialObject other in allObjects)
        {
            if (other == this) continue;
            velocity += CalculateNewVelocity(this.position, other.position, this.mass, other.mass, timeStep);
        }
    }

    /// <summary>
    /// Berechnet die neue Geschwindigkeit des K�rpers, basierend auf der Gravitation eines anderen K�rpers.
    /// </summary>
    /// <param name="ownPosition">Eigene Position</param>
    /// <param name="otherPosition">Position des anderen K�rpers</param>
    /// <param name="ownMass">Eigene Masse</param>
    /// <param name="otherMass">Masse des anderen K�rpers</param>
    /// <param name="timeStep">Zeitintervall f�r die Berechnung</param>
    /// <returns>Neue Geschwindigkeit des K�rpers</returns>
    public static Vector3d CalculateNewVelocity(Vector3d ownPosition, Vector3d otherPosition, double ownMass, double otherMass, float timeStep)
    {
        // Abstand der K�rper quadriert: r^2
        double distanceSquared = (ownPosition - otherPosition).SqrMagnitude;

        // Objekte sind zu nahe beieinander (keine Kollision), Gravitation w�re unrealistisch hoch
        // Stattdessen wird die Geschwindigkeit auf 0 gesetzt
        if (distanceSquared <= 0.05)
        {
            return Vector3d.Zero;
        }

        // Abstand der K�rper: r
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
    /// Aktualisiert die Position des K�rpers, basierend auf der aktuellen Geschwindigkeit und dem Zeitintervall
    /// </summary>
    /// <param name="timeStep">Zeitintervall</param>
    public void UpdatePosition(float timeStep)
    {
        this.position += this.velocity * timeStep;
    }

    /// <summary>
    /// Aktualisiert die angezeigte Position des K�rpers, basierend auf der aktuellen Geschwindigkeit und dem Zeitintervall.
    /// Speichert au�erdem die vorherige Position zur darstellung der Umlaufbahn
    /// </summary>
    public void SavePosition()
    {
        // Speichert die aktuelle Position f�r die Darstellung der Umlaufbahn
        positions.Enqueue(this.position);

        // Wenn zu viele Positionen gespeichert sind, wird die �lteste gel�scht
        // Gr��ere K�rper (Planeten) speichern mehr Positionen
        if (positions.Count > (this.radius > 1100 ? Universe.MAJOR_HISTORY_SIZE : Universe.MINOR_HISTORY_SIZE))
        {
            positions.Dequeue();
        }

        // Aktualisiert die angezeigte Position des K�rpers
        this.transform.position = (Vector3)this.position / Universe.SCALE_FACTOR;
    }

    /// <summary>
    /// Von Unity ausgef�hrt. Zeichnet die Umlaufbahn des K�rpers.
    /// </summary>
    void OnDrawGizmos()
    {
        // Farbe der Umlaufbahn
        Gizmos.color = previewColor;
        // Name des K�rpers als Label
        Handles.Label(transform.position + Vector3.up * (radius / Universe.SCALE_FACTOR), gameObject.name);

        // Wenn der K�rper keine Positionen gespeichert hat, wird nichts gezeichnet
        if (positions == null || positions.Count == 0) return;

        // Zeichnet die Umlaufbahn des K�rpers
        Vector3d lastPosition = positions.First();
        foreach (Vector3d position in positions)
        {
            Gizmos.DrawLine((Vector3) lastPosition / Universe.SCALE_FACTOR, (Vector3) position / Universe.SCALE_FACTOR);
            lastPosition = position;
        }
    }

    /// <summary>
    /// Von Unity ausgef�hrt. Aktualisiert die Position des K�rpers im Editor.
    /// Dies ist n�tig, da das Positionsfeld der CelestialObject-Komponente einen deutlich genaueren Wert nutzt als die Transform-Komponente von Unity.
    /// Wird der Wert der CelestialObject-Komponente ver�ndert, wird die tats�chliche Position des K�rpers durch diese Funktion aktualisiert.
    /// Die tats�chliche Position ist um einen gro�en Faktor verkleinert, um Genauigkeitsfehler zu vermeiden.
    /// Alle Berechnungen nutzen den genauen Wert der Celestial-Object-Komponente.
    /// </summary>
    private void OnValidate()
    {
        transform.position = (Vector3) (position / Universe.SCALE_FACTOR);
    }
}
