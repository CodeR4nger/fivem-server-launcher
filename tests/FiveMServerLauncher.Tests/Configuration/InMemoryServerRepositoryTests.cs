using FiveMServerLauncher.Domain;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class InMemoryServerRepositoryTests
{
    [Fact]
    public void Mutation_WhenWriteFailureIsSet_ShouldThrowItAndKeepTheStore()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("First", "abc123"));
        repository.Add(SavedServer.Create("Second", "def456"));
        repository.WriteFailure = new IOException("locked");

        // When / Then
        Assert.Throws<IOException>(() => repository.Add(SavedServer.Create("Third", "ghi789")));
        Assert.Throws<IOException>(() => repository.Remove("abc123"));
        Assert.Throws<IOException>(() => repository.Move("def456", 0));
        Assert.Equal(new[] { "abc123", "def456" }, repository.GetAll().Select(s => s.Address));
    }

    [Fact]
    public void Add_WhenAddressAlreadySaved_ShouldThrowArgumentException()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("First", "abc123"));

        // When / Then
        Assert.Throws<ArgumentException>(() => repository.Add(SavedServer.Create("Second", "ABC123")));
    }

    [Fact]
    public void GetAll_ShouldReturnASnapshot()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var snapshot = repository.GetAll();

        // When
        repository.Add(SavedServer.Create("First", "abc123"));

        // Then
        Assert.Empty(snapshot);
    }
}
