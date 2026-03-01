using DG.Tweening;
using UnityEngine;

namespace Menu.UI
{
    public sealed class HomePageEntranceController : MonoBehaviour
    {
        [SerializeField] private UIEntranceItem[] itemsInOrder;

        [Header("Global")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float globalDelay = 0.05f;
        [SerializeField] private float extraStagger = 0.06f;

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        [ContextMenu("Play")]
        public void Play()
        {
            DOTween.Kill(this);

            // Подготовка: спрятать всё мгновенно
            if (itemsInOrder != null)
            {
                foreach (var it in itemsInOrder)
                    it?.PrepareInstantHidden();
            }

            // Каскад: добавляем задержки сверху, чтобы реально было “по очереди”
            float t = globalDelay;
            if (itemsInOrder != null)
            {
                foreach (var it in itemsInOrder)
                {
                    if (it == null) continue;

                    // Костыль без доступа к delay: запускаем через Sequence
                    DOTween.Sequence()
                        .SetUpdate(true)
                        .SetId(this)
                        .AppendInterval(t)
                        .AppendCallback(() => it.Play());

                    t += extraStagger;
                }
            }
        }
    }
}