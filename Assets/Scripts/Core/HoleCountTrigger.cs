using System.Collections.Generic;
using UnityEngine;

public class HoleCountTrigger : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RunController run;
    [SerializeField] private FloatingXpTextSpawner xpText;
    [SerializeField] private FlyToUiIconSpawner flyToUi;

    [Header("Swallow rule")]
    [Tooltip("Насколько ниже центра CountTrigger должен быть ВЕРХ объекта, чтобы считалось 'полностью проглочен'.")]
    [SerializeField] private float swallowOffsetY = 0.05f;

    // чтобы не засчитывать один и тот же объект много раз
    private readonly HashSet<AbsorbablePhysicsItem> _counted = new();

    private void Reset()
    {
        run = FindFirstObjectByType<RunController>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (run == null || !run.IsRunning) return;

        var item = other.GetComponentInParent<AbsorbablePhysicsItem>();
        if (item == null) return;

        if (_counted.Contains(item)) return;

        // если нет коллайдера, не можем корректно проверить "полностью"
        if (item.Col == null) return;

        float swallowY = transform.position.y - swallowOffsetY;

        // объект считается проглоченным, когда его верхняя граница ниже swallowY
        float topY = item.Col.bounds.max.y;

        if (topY > swallowY) return;

        // ✅ полностью проглочен -> засчитываем
        _counted.Add(item);

        run.OnItemCollected(item);
        xpText?.Spawn(item.transform.position, item.XpValue);

        if (run.IsGoalItem(item.Type))
        {
            var target = run.GetGoalIconTarget(item.Type);
            if (target != null && item.UiIcon != null)
                flyToUi?.Spawn(item.transform.position, item.UiIcon, target);
        }

        if (run.IsGoalItem(item.Type))
{
    var target = run.GetGoalIconTarget(item.Type);
    if (target != null && item.UiIcon != null)
        flyToUi.Spawn(item.transform.position, item.UiIcon, target);
}

        Destroy(item.gameObject);
    }

    private void LateUpdate()
    {
        // чистка уничтоженных, чтобы HashSet не пух (и не ловить MissingReference)
        _counted.RemoveWhere(x => x == null);
    }
}