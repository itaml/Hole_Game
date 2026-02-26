using System;
using System.Collections.Generic;

namespace Core.Events
{
    /// <summary>
    /// Tiny event bus to decouple UI/services. Avoid overusing it; prefer direct calls inside MetaFacade.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_handlers.TryGetValue(t, out var list))
            {
                list = new List<Delegate>();
                _handlers[t] = list;
            }
            if (!list.Contains(handler)) list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0) _handlers.Remove(t);
            }
        }

        public void Publish<T>(T evt)
        {
            var t = typeof(T);
            if (_handlers.TryGetValue(t, out var list))
            {
                // copy to avoid modification during iteration
                var copy = list.ToArray();
                for (int i = 0; i < copy.Length; i++)
                    (copy[i] as Action<T>)?.Invoke(evt);
            }
        }
    }
}
