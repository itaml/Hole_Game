using UnityEngine;

public class AbsorbDetector : MonoBehaviour
{
    [SerializeField] private HoleController hole;
    [SerializeField] private RunController run;
    [SerializeField] private FloatingXpTextSpawner xpText;
    [SerializeField] private FlyToUiIconSpawner flyToUi;

    private void Reset()
    {
        hole = GetComponentInParent<HoleController>();
        run = FindFirstObjectByType<RunController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!run || !run.IsRunning) return;

        var item = other.GetComponentInParent<AbsorbableItem>();
        if (!item) return;

        if (!item.CanBeAbsorbedBy(hole)) return;

        // начисления
        hole.AddXp(item.XpValue);
        run.OnItemCollected(item);

        // VFX: цифра опыта
        if (xpText) xpText.Spawn(item.transform.position, item.XpValue);

        // VFX: иконка в UI, если это цель
        if (run.IsGoalItem(item.Type) && flyToUi && item.UiIcon != null)
        {
            flyToUi.Spawn(item.transform.position, item.UiIcon, run.GetGoalIconTarget(item.Type));
        }

        Destroy(item.gameObject);
    }
}