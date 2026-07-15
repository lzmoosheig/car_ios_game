using System;
using System.Collections.Generic;

namespace Overhaul.Core
{
    /// <summary>
    /// Minimal typed publish/subscribe bus. Decouples systems: workstations, economy,
    /// UI, audio, haptics, tutorial and analytics all subscribe to the same events
    /// rather than calling each other directly (Doc 06 §2).
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();

        public void Subscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            _handlers[key] = _handlers.TryGetValue(key, out var d)
                ? Delegate.Combine(d, handler)
                : handler;
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var key = typeof(T);
            if (!_handlers.TryGetValue(key, out var d)) return;
            var result = Delegate.Remove(d, handler);
            if (result == null) _handlers.Remove(key);
            else _handlers[key] = result;
        }

        public void Publish<T>(T evt)
        {
            if (_handlers.TryGetValue(typeof(T), out var d))
                ((Action<T>)d)?.Invoke(evt);
        }
    }
}
