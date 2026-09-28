using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
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
    public void Update_ShouldPreserveCfxId()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("My Server", "149.56.120.52:30320", cfxId: "y4lg95"));
        var renamed = SavedServer.Create("Renamed", "149.56.120.52:30320", cfxId: "y4lg95");

        // When
        repository.Update(renamed);

        // Then
        Assert.Equal("y4lg95", Assert.Single(repository.GetAll()).CfxId);
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

    [Fact]
    public void FindByAddress_WhenAddressSaved_ShouldReturnServerCaseInsensitively()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        var server = SavedServer.Create("My Server", "abc123", requiresDiscord: true);
        repository.Add(server);

        // When
        var result = repository.FindByAddress("ABC123");

        // Then
        Assert.NotNull(result);
        Assert.Equal("My Server", result.Name);
        Assert.True(result.RequiresDiscord);
    }

    [Fact]
    public void FindByAddress_WhenAddressNotSaved_ShouldReturnNull()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        var result = repository.FindByAddress("abc123");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public void FindByAddress_WhenFileContainsInvalidEntries_ShouldIgnoreThem()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, """[null, {}]""");
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        var result = repository.FindByAddress("abc123");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public void AddThenReopen_WhenServerHasCfxId_ShouldPersistCfxId()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var first = new FileServerRepository(tempDir.FilePath);
        first.Add(SavedServer.Create("My Server", "cfx.re/join/abc123"));

        // When (a fresh instance reads the same file)
        var second = new FileServerRepository(tempDir.FilePath);
        var result = second.GetAll();

        // Then
        var server = Assert.Single(result);
        Assert.Equal("abc123", server.CfxId);
    }

    [Fact]
    public void FindByCfxId_WhenIdSaved_ShouldReturnServerCaseInsensitively()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("My Server", "149.56.120.52:30320", cfxId: "y4lg95"));
        repository.Add(SavedServer.Create("Other Server", "localhost:30120"));

        // When
        var result = repository.FindByCfxId("Y4LG95");

        // Then
        Assert.NotNull(result);
        Assert.Equal("My Server", result.Name);
        Assert.Equal("149.56.120.52:30320", result.Address);
    }

    [Fact]
    public void FindByCfxId_WhenIdNotSaved_ShouldReturnNull()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("My Server", "149.56.120.52:30320"));

        // When
        var result = repository.FindByCfxId("y4lg95");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public void FindByCfxId_WhenMultipleRowsShareId_ShouldReturnFirstMatch()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.Add(SavedServer.Create("First", "149.56.120.52:30320", cfxId: "y4lg95"));
        repository.Add(SavedServer.Create("Second", "localhost:30120", cfxId: "y4lg95"));

        // When
        var result = repository.FindByCfxId("y4lg95");

        // Then
        Assert.NotNull(result);
        Assert.Equal("First", result.Name);
    }

    [Fact]
    public void AddThenReopen_WhenServerHasManualOverrides_ShouldPersistThem()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var first = new FileServerRepository(tempDir.FilePath);
        first.Add(SavedServer.Create(
            "Local Dev", "localhost:30120",
            cfxId: "8y6354", gameBuild: 3258, pureMode: 1, gameClient: GameClient.RedM));

        // When (a fresh instance reads the same file)
        var second = new FileServerRepository(tempDir.FilePath);
        var server = Assert.Single(second.GetAll());

        // Then
        Assert.Equal("8y6354", server.CfxId);
        Assert.Equal(3258, server.GameBuild);
        Assert.Equal(1, server.PureMode);
        Assert.Equal(GameClient.RedM, server.GameClient);
    }

    [Fact]
    public void GetAll_WhenFileLacksOverrideFields_ShouldDeserializeWithNulls()
    {
        // Given — pre-v1.2 files carry no override fields.
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(
            tempDir.FilePath,
            """[{"Name": "My Server", "Address": "localhost:30120", "CfxId": "8y6354"}]""");
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        var result = repository.GetAll();

        // Then
        var server = Assert.Single(result);
        Assert.Equal("8y6354", server.CfxId);
        Assert.Null(server.GameBuild);
        Assert.Null(server.PureMode);
        Assert.Null(server.GameClient);
    }

    [Fact]
    public void GetAll_WhenFileLacksCfxIdField_ShouldDeserializeWithNullCfxId()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, """[{"Name": "My Server", "Address": "abc123"}]""");
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        var result = repository.GetAll();

        // Then
        var server = Assert.Single(result);
        Assert.Null(server.CfxId);
    }
}