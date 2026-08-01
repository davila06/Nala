// Disables parallel test class execution — WebApplicationFactory instances conflict when run concurrently.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace PawTrack.IntegrationTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }
}
