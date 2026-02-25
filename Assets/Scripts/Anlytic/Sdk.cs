public static class Sdk
{
    public static IAnalyticsService Analytics { get; private set; }
    public static IRemoteConfigService RemoteConfig { get; private set; }
    public static IAdsService Ads { get; private set; }
    public static IIapService Iap { get; private set; }

    private static bool _inited;

    public static void EnsureInitialized()
    {
        if (_inited) return;

        Analytics = new AnalyticsServiceStub();
        RemoteConfig = new RemoteConfigServiceStub();
        Ads = new AdsServiceStub();
        Iap = new IapServiceStub();

        _inited = true;
    }
}