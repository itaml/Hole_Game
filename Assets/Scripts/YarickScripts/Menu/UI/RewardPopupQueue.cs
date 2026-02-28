using System;
using System.Collections.Generic;
using UnityEngine;

namespace Menu.UI
{
    public sealed class RewardPopupQueue : MonoBehaviour
    {
        private readonly Queue<Action> _queue = new();
        private bool _isShowing;

        public bool IsBusy => _isShowing || _queue.Count > 0;

        public void Enqueue(Action show)
        {
            _queue.Enqueue(show);
            TryShowNext();
        }

        public void NotifyClosed()
        {
            _isShowing = false;
            TryShowNext();
        }

        private void TryShowNext()
        {
            if (_isShowing) return;
            if (_queue.Count == 0) return;

            _isShowing = true;
            _queue.Dequeue()?.Invoke();
        }
    }
}