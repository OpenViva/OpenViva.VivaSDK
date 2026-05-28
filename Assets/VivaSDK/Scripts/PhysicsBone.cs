using UnityEngine;

public class PhysicsBone : MonoBehaviour
{
    // Preset System

    [Header("Preset")]
    public BonePreset preset = BonePreset.LongHair;

    public enum BonePreset
    {
        Skirt,
        ShortHair,
        LongHair,
        AnimalTail,
        AnimalEars
    }

    [Header("Preset Values")]
    public float gravity = 2f;
    public float damping = 0.05f;
    public float distanceCompression = 0.5f;
    public float stiffnessValue = 0.15f;
    public bool useStiffnessCurve = true;
    public float stiffnessCurveStart = 1f;
    public float stiffnessCurveEnd = 0.15f;
    public float velocityAttenuation = 0.6f;
    public bool useLimit = true;
    public float speedLimit = 3f;

    public Color markerColor = Color.green;

    private void OnValidate()
    {
        // Auto-configure values based on preset
        ApplyPresetDefaults();
    }

    private void ApplyPresetDefaults()
    {
        switch (preset)
        {
            case BonePreset.Skirt:
                gravity = 2f;
                damping = 0.05f;
                stiffnessValue = 0.15f;
                break;
            case BonePreset.ShortHair:
                gravity = 1.5f;
                damping = 0.1f;
                stiffnessValue = 0.3f;
                break;
            case BonePreset.LongHair:
                gravity = 1.0f;
                damping = 0.08f;
                stiffnessValue = 0.2f;
                break;
            case BonePreset.AnimalTail:
                gravity = 2.5f;
                damping = 0.15f;
                stiffnessValue = 0.25f;
                break;
            case BonePreset.AnimalEars:
                gravity = 1.8f;
                damping = 0.07f;
                stiffnessValue = 0.35f;
                break;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = markerColor;
        Gizmos.DrawSphere(transform.position, 0.1f);

        // Draw a line to parent bone if exists
        if (transform.parent != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.parent.position);
        }
    }
}
