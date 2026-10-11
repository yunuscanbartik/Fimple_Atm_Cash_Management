namespace Fimple.Atm.Tests.Integration
{
    public abstract class IntegrationTestBase
    {
        protected IntegrationTestBase(ApiFixture fixture)
        {
            Fixture = fixture;
            Api = fixture.Api;
            Atms = new AtmTestHelper(fixture.Api);
        }

        protected ApiFixture Fixture { get; }

        protected ApiClient Api { get; }

        protected AtmTestHelper Atms { get; }
    }
}
