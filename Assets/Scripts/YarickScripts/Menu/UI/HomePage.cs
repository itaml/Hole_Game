using UnityEngine;
using DG.Tweening;

public class HomePage : UIPageBase
{
    [Header("Refs")]
    public CanvasGroup rootCG;          // на весь page root
    public RectTransform main;
    public RectTransform[] buttons;

    private Vector2 headerPos, mainPos, footerPos;

    void Awake()
    {
        mainPos = main.anchoredPosition;
    }

    public override void PrepareShow()
    {
        base.PrepareShow();

        // Стартовое состояние перед IN
        rootCG.alpha = 0f;

        main.anchoredPosition = mainPos + new Vector2(0, -80);

        rootCG.interactable = false;
        rootCG.blocksRaycasts = false;
    }

    public override Sequence PlayIn()
    {
        Kill();

        seq = DOTween.Sequence();

        seq.Join(rootCG.DOFade(1f, inDuration));

        seq.Join(main.DOAnchorPos(mainPos, inDuration).SetEase(Ease.OutCubic));

        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            var p = b.anchoredPosition;
            b.anchoredPosition = p + new Vector2(0, -30);

            seq.Insert(0.12f + i * 0.05f, b.DOAnchorPos(p, 0.2f).SetEase(Ease.OutBack));
        }

        seq.OnComplete(() =>
        {
            rootCG.interactable = true;
            rootCG.blocksRaycasts = true;
        });

        return seq;
    }

    public override Sequence PlayOut()
    {
        Kill();

        rootCG.interactable = false;
        rootCG.blocksRaycasts = false;

        seq = DOTween.Sequence();

        // Уезжаем обратно
        seq.Join(rootCG.DOFade(0f, outDuration));
        seq.Join(main.DOAnchorPos(mainPos + new Vector2(0, -80), outDuration).SetEase(Ease.InCubic));

        return seq;
    }

    public override void PrepareHideInstant()
    {
        // Мгновенно скрыть (чтобы при следующем открытии не мигало)
        Kill();
        if (rootCG) { rootCG.alpha = 0f; rootCG.interactable = false; rootCG.blocksRaycasts = false; }
        base.PrepareHideInstant();
    }
}