using UnityEngine;
using DG.Tweening;

public class HoleController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private FloatingJoystick joystick;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private Rigidbody rb;

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }


void Start()
{
    transform
        .DOScale(1.04f, 1.2f)
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine)
        .SetUpdate(true);
}
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

        int newLevel = 1 + (Xp / 100);
        if (newLevel != Level)
        {
            Level = newLevel;
        }
        SfxClipRouter.Instance?.Play(SfxKey.Item);
    }

}