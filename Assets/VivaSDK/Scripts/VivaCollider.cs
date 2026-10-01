using UnityEngine;

public class VivaCollider : MonoBehaviour
{
    public enum ShapeType { Sphere, Capsule }
    public enum AxisOrientation { X, Y, Z }

    [Header("Settings")]
    public ShapeType shapeToDraw = ShapeType.Capsule;
    public AxisOrientation direction = AxisOrientation.Y;
    public Color gizmoColor = Color.cyan;

    [Header("Dimensions")]
    [Range(0.05f, 2f)]
    public float length = 0.2f;
    [Range(0.05f, 0.5f)]
    public float radius = 0.04f;

    [Header("Resolution")]
    [Range(3, 16)]
    public int segments = 8;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        if (shapeToDraw == ShapeType.Sphere)
        {
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        else
        {
            DrawWireCapsule();
        }
    }

    private void DrawWireCapsule()
    {
        Vector3 mainAxis = Vector3.zero;
        Vector3 sideA = Vector3.zero;
        Vector3 sideB = Vector3.zero;

        switch (direction)
        {
            case AxisOrientation.X:
                mainAxis = transform.right;
                sideA = transform.up;
                sideB = transform.forward;
                break;
            case AxisOrientation.Y:
                mainAxis = transform.up;
                sideA = transform.right;
                sideB = transform.forward;
                break;
            case AxisOrientation.Z:
                mainAxis = transform.forward;
                sideA = transform.right;
                sideB = transform.up;
                break;
        }

        float cylinderHeight = Mathf.Max(0, length - (2 * radius));

        Vector3 topCapCenter = transform.position + mainAxis * (cylinderHeight / 2f);
        Vector3 bottomCapCenter = transform.position - mainAxis * (cylinderHeight / 2f);

        Gizmos.DrawWireSphere(topCapCenter, radius);
        Gizmos.DrawWireSphere(bottomCapCenter, radius);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            float nextAngle = (i + 1) * Mathf.PI * 2 / segments;

            Vector3 dir = (sideA * Mathf.Cos(angle) + sideB * Mathf.Sin(angle));
            Vector3 nextDir = (sideA * Mathf.Cos(nextAngle) + sideB * Mathf.Sin(nextAngle));

            Gizmos.DrawLine(topCapCenter + dir * radius, bottomCapCenter + dir * radius);

            Gizmos.DrawLine(topCapCenter + dir * radius, topCapCenter + nextDir * radius);
            Gizmos.DrawLine(bottomCapCenter + dir * radius, bottomCapCenter + nextDir * radius);
        }
    }
}
