using UnityEngine;
using DG.Tweening;

public class TeamPage : UIPageBase
{
    [Header("Refs")]
    public CanvasGroup rootCG;          // �� ���� page root
    public RectTransform header;
    public RectTransform main;
    public RectTransform[] buttons;

    private Vector2 headerPos, mainPos, footerPos;

    void Awake()
    {
        headerPos = header.anchoredPosition;
        mainPos = main.anchoredPosition;
    }

    public override void PrepareShow()
    {
        base.PrepareShow();

        // ��������� ��������� ����� IN
        rootCG.alpha = 0f;

        header.anchoredPosition = headerPos + new Vector2(0, 60);
        main.anchoredPosition = mainPos + new Vector2(0, -80);

        rootCG.interactable = false;
        rootCG.blocksRaycasts = false;
    }

    public override Sequence PlayIn()
    {
        Kill();

        seq = DOTween.Sequence();

        seq.Join(rootCG.DOFade(1f, inDuration));

        seq.Join(header.DOAnchorPos(headerPos, inDuration).SetEase(Ease.OutCubic));
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

        // ������� �������
        seq.Join(rootCG.DOFade(0f, outDuration));
        seq.Join(header.DOAnchorPos(headerPos + new Vector2(0, 60), outDuration).SetEase(Ease.InCubic));
        seq.Join(main.DOAnchorPos(mainPos + new Vector2(0, -80), outDuration).SetEase(Ease.InCubic));

        return seq;
    }

    public override void PrepareHideInstant()
    {
        // ��������� ������ (����� ��� ��������� �������� �� ������)
        Kill();
        if (rootCG) { rootCG.alpha = 0f; rootCG.interactable = false; rootCG.blocksRaycasts = false; }
        base.PrepareHideInstant();
    }
}