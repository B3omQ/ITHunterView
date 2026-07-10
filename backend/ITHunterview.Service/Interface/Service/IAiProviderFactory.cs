namespace ITHunterview.Service.Interface.Service
{
    public interface IAiProviderFactory
    {
        IAiProvider GetProvider(string providerName);
    }
}
