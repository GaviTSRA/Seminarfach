using UnityEngine;
using System;

/// <summary>
/// Klasse die einen 3 dimensionalen Vektor representiert.
/// Im gegensatz zu Unity's Vector3 nutzt diese Klasse double anstelle von float zum Speichern der Werte.
/// Dies macht ihn nutzbar für sehr große Werte, ohne Genauigkeit zu verlieren.
/// </summary>
[System.Serializable]
public class Vector3d
{
    public double x;
    public double y;
    public double z;

    public Vector3d(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vector3d operator *(Vector3d a, double d) => new(a.x * d, a.y * d, a.z * d);
    public static Vector3d operator /(Vector3d a, double d) => new(a.x / d, a.y / d, a.z / d);

    public double Magnitude => Math.Sqrt(x * x + y * y + z * z);
    public double SqrMagnitude => x * x + y * y + z * z;

    public Vector3d Normalized
    {
        get
        {
            return Magnitude > 1e-9 ? this / Magnitude : new Vector3d(0, 0, 0);
        }
    }

    public static explicit operator Vector3(Vector3d v)
    {
        return new Vector3((float)v.x, (float)v.y, (float)v.z);
    }

    public static Vector3d Zero => new(0, 0, 0);

    public override bool Equals(object obj)
    {
        if (obj is not Vector3d v) return false;
        return Math.Abs(x - v.x) < 1e-9 && Math.Abs(y - v.y) < 1e-9 && Math.Abs(z - v.z) < 1e-9;
    }

    public override int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2;
    }


    public static bool operator ==(Vector3d a, Vector3d b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Vector3d a, Vector3d b)
    {
        return !(a == b);
    }

    public static double Distance(Vector3d a, Vector3d b)
    {
        double dx = a.x - b.x;
        double dy = a.y - b.y;
        double dz = a.z - b.z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
