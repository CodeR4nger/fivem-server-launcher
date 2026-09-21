using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Domain;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class FileServerRepositoryTests
{
    [Fact]
    public void GetAll_WhenFileDoesNotExist_ShouldReturnEmptyList()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        var result = repository.GetAll();

        // Then
        Assert.Empty(result);
    }

    [Fact]
    public void Add_ThenGetAll_ShouldReturnTheSavedServer()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        var server = SavedServer.Create("My Server", "abc123", requiresSteam: true);

        // When
        repository.Add(server);

        // Then
        var result = Assert.Single(repository.GetAll());
        Assert.Equal("My Server", result.Name);
        Assert.Equal("abc123", result.Address);
        Assert.True(result.RequiresSteam);
    }

    [Fact]
    public void Add_WhenAddressAlreadySaved_ShouldThrowArgumentException()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("First", "abc123"));

        // When / Then
        Assert.Throws<ArgumentException>(() => repository.Add(SavedServer.Create("Second", "abc123")));
    }

    [Fact]
    public void Add_WhenDirectoryDoesNotExist_ShouldPersistTheServer()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        var server = SavedServer.Create("My Server", "cfx.re/join/abc123");

        // When
        repository.Add(server);

        // Then
        Assert.True(File.Exists(tempDir.FilePath));
    }

    [Fact]
    public void Update_ShouldReplaceServerWithSameAddress()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("Old Name", "abc123"));
        var renamed = SavedServer.Create("New Name", "abc123", requiresDiscord: true);

        // When
        repository.Update(renamed);

        // Then
        var result = Assert.Single(repository.GetAll());
        Assert.Equal("New Name", result.Name);
        Assert.True(result.RequiresDiscord);
    }

    [Fact]
    public void Update_WhenAddressNotSaved_ShouldThrowArgumentException()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);

        // When / Then
        Assert.Throws<ArgumentException>(() => repository.Update(SavedServer.Create("My Server", "abc123")));
    }

    [Fact]
    public void Remove_ShouldDropTheServer()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("My Server", "abc123"));
        repository.Add(SavedServer.Create("Other Server", "def456"));

        // When
        repository.Remove("abc123");

        // Then
        Assert.Single(repository.GetAll());
    }

    [Fact]
    public void Remove_WhenAddressNotSaved_ShouldBeNoOp()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("My Server", "abc123"));

        // When
        repository.Remove("unknown");

        // Then
        Assert.Single(repository.GetAll());
    }

    [Fact]
    public void AddThenReopen_ShouldSurviveRestartAcrossInstances()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var first = new FileServerRepository(tempDir.FilePath);
        first.Add(SavedServer.Create("My Server", "abc123", requiresSteam: true));

        // When (a fresh instance reads the same file)
        var second = new FileServerRepository(tempDir.FilePath);
        var result = second.GetAll();

        // Then
        var server = Assert.Single(result);
        Assert.Equal("My Server", server.Name);
        Assert.Equal("abc123", server.Address);
        Assert.True(server.RequiresSteam);
    }

    [Fact]
    public void GetAll_WhenFileContainsInvalidJson_ShouldReturnEmptyList()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, "{ invalid json");

        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        var result = repository.GetAll();

        // Then
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_WhenFileContainsInvalidEntries_ShouldDropThem()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(
            tempDir.FilePath,
            """[null, {}, {"Name": "  ", "Address": "abc123"}, {"Name": "My Server", "Address": "abc123"}]""");

        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        var result = repository.GetAll();

        // Then
        var server = Assert.Single(result);
        Assert.Equal("My Server", server.Name);
    }
}