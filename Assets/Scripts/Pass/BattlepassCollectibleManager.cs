using UnityEngine;
using TMPro;

public class BattlepassCollectibleManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RunController run;
    [SerializeField] private AbsorbablePhysicsItem tokenPrefab;

    [Header("UI")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private TMP_Text progressText;

    [Header("Spawn Field")]
    [SerializeField] private Vector3 fieldCenter = Vector3.zero;
    [SerializeField] private Vector2 fieldSize = new Vector2(40,40);
    [SerializeField] private Transform hole;
    [SerializeField] private float minDistanceFromHole = 6f;

    [Header("Spawn Count")]
    [SerializeField] private int minSpawn = 1;
    [SerializeField] private int maxSpawn = 5;

    private int _target;
    private int _collected;

    public int Collected => _collected;

    private void Awake()
    {
        if (run == null)
            run = FindFirstObjectByType<RunController>();
    }

    public void ResetForNewRun()
    {
        _target = 0;
        _collected = 0;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        UpdateUI();
    }

    public void SpawnIfNeeded()
    {
        var cfg = run.PendingConfig;

        if (cfg == null)
            return;

        if (!cfg.isBattlepasOpen)
        {
            if (uiRoot) uiRoot.SetActive(false);
            return;
        }

        if (uiRoot) uiRoot.SetActive(true);

        _target = Random.Range(minSpawn, maxSpawn + 1);
        _collected = 0;

        for (int i = 0; i < _target; i++)
        {
            Vector3 pos = GetRandomPosition();

            var item = Instantiate(tokenPrefab, pos, Quaternion.identity, transform);
        }

        UpdateUI();
    }

    private Vector3 GetRandomPosition()
    {
        for (int i = 0; i < 20; i++)
        {
            float x = Random.Range(-fieldSize.x/2, fieldSize.x/2);
            float z = Random.Range(-fieldSize.y/2, fieldSize.y/2);

            Vector3 p = fieldCenter + new Vector3(x,0,z);

            if (hole == null) return p;

            if (Vector3.Distance(p, hole.position) > minDistanceFromHole)
                return p;
        }

        return fieldCenter;
    }

    public void OnTokenCollected()
    {
        _collected++;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (progressText)
            progressText.text = $"{_collected}/{_target}";
    }
}