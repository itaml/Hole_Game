using UnityEngine;
using DG.Tweening;

public abstract class UIPageBase : MonoBehaviour
{
    public float inDuration = 0.35f;
    public float outDuration = 0.25f;

    protected Sequence seq;

    public virtual void PrepareShow()
    {
        gameObject.SetActive(true);
    }

    public abstract Sequence PlayIn();
    public abstract Sequence PlayOut();

    public virtual void PrepareHideInstant()
    {
        Kill();
        gameObject.SetActive(false);
    }

    protected void Kill()
    {
        if (seq != null && seq.IsActive()) seq.Kill();
        // На всякий, если ты где-то твинишь конкретные объекты:
        // DOTween.Kill(gameObject, true);
    }
}