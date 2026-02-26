using System;

namespace Core.Time
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}
