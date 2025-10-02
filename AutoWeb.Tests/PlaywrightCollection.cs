using Xunit;

namespace AutoWeb.Tests;

/// <summary>
/// Collection definition for Playwright tests.
/// Ensures all tests that use [Collection("Playwright")] share a single PlaywrightFixture instance.
/// This prevents multiple server startups and ensures the server stays alive for all tests.
/// </summary>
[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    // This class is never instantiated - it's just a marker for xUnit
}
