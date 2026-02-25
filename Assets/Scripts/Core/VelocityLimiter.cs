using UnityEngine;

public class VelocityLimiter : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float maxUpVelocity = 1.5f; // сколько можно лететь вверх

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!rb) return;

        var v = rb.linearVelocity;
        if (v.y > maxUpVelocity)
        {
            v.y = maxUpVelocity;
            rb.linearVelocity = v;
        }
    }
}