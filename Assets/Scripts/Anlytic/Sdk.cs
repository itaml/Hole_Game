
public static class Sdk
{
    
    public static IAnalyticsService Analytics { get; private set; }
    public static IRemoteConfigService RemoteConfig { get; private set; }
    public static IAdsService Ads { get; private set; }
    public static IIapService Iap { get; private set; }


    private static void Bootstrap()
    {
        Analytics = new AnalyticsServiceStub();
        RemoteConfig = new RemoteConfigServiceStub();
        Ads = new AdsServiceStub();
        Iap = new IapServiceStub();
    }
}