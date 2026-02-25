using UnityEngine;

public class HoleController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private FloatingJoystick joystick;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody rb;

    [Header("Size")]
    [SerializeField] private float baseRadius = 0.6f;
    [SerializeField] private float radiusPerLevel = 0.08f;
    [SerializeField] private Transform visualRoot;

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!joystick || rb == null) return;

        Vector3 dir = new Vector3(
            joystick.Horizontal,
            0f,
            joystick.Vertical
        );

        rb.linearVelocity = dir * moveSpeed;
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;

        Xp += amount;

        int newLevel = 1 + (Xp / 100); // формулу потом подгонишь под оригинал
        if (newLevel != Level)
        {
            Level = newLevel;
            ApplySize();
        }
    }

    private void ApplySize()
    {
        float radius = baseRadius + (Level - 1) * radiusPerLevel;
        if (visualRoot)
            visualRoot.localScale = Vector3.one * radius;
    }
}