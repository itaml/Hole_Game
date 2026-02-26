using System;

namespace Core.Time
{
    public sealed class DeviceTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
