using System.Text.Json.Nodes;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Domain;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class FileServerRepositorySafetyTests
{
    private const string TwoServers =
        """[{"Name":"First","Address":"abc123"},{"Name":"Second","Address":"def456"}]""";

    public static TheoryData<string> Mutations => new()
    {
        nameof(IServerRepository.Add),
        nameof(IServerRepository.Update),
        nameof(IServerRepository.Replace),
        nameof(IServerRepository.Remove),
        nameof(IServerRepository.Move)
    };

    private static void Mutate(IServerRepository repository, string mutation)
    {
        switch (mutation)
        {
            case nameof(IServerRepository.Add):
                repository.Add(SavedServer.Create("Third", "ghi789"));
                break;
            case nameof(IServerRepository.Update):
                repository.Update(SavedServer.Create("Renamed", "abc123"));
                break;
            case nameof(IServerRepository.Replace):
                repository.Replace("abc123", SavedServer.Create("Moved", "ghi789"));
                break;
            case nameof(IServerRepository.Remove):
                repository.Remove("abc123");
                break;
            case nameof(IServerRepository.Move):
                repository.Move("def456", 0);
                break;
        }
    }

    private static TempSettingsDirectory WithFile(string content)
    {
        var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, content);
        return tempDir;
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Mutation_WhenReadFailsWithIOException_ShouldThrowAndLeaveFileByteIdentical(string mutation)
    {
        // Given — an AV sharing lock makes every read fail while the file is intact.
        using var tempDir = WithFile(TwoServers);
        var before = File.ReadAllBytes(tempDir.FilePath);
        var repository = new FileServerRepository(
            tempDir.FilePath, _ => throw new IOException("locked"));

        // When
        var act = () => Mutate(repository, mutation);

        // Then — the mutation fails visibly; nothing replaces the unread file.
        Assert.Throws<StoreUnavailableException>(act);
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Mutation_WhenFileIsLockedForReal_ShouldThrowAndLeaveFileByteIdentical(string mutation)
    {
        // Given
        using var tempDir = WithFile(TwoServers);
        var before = File.ReadAllBytes(tempDir.FilePath);
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        using (File.Open(tempDir.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Empty(repository.GetAll());
            Assert.Throws<StoreUnavailableException>(() => Mutate(repository, mutation));
        }

        // Then
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
    }

    [Fact]
    public void GetAll_WhenReadIsDenied_ShouldReturnEmptyNotThrow()
    {
        // Given — a permission fault is an unreadable store, never a startup crash.
        using var tempDir = WithFile(TwoServers);
        var repository = new FileServerRepository(
            tempDir.FilePath, _ => throw new UnauthorizedAccessException());

        // When / Then
        Assert.Empty(repository.GetAll());
        Assert.Throws<StoreUnavailableException>(() => repository.Add(SavedServer.Create("Third", "ghi789")));
    }

    [Fact]
    public void Add_WhenReadRecoversAfterTransientFailure_ShouldKeepTheExistingServers()
    {
        // Given — the first read hits the lock, the retry inside the mutation succeeds.
        using var tempDir = WithFile(TwoServers);
        var calls = 0;
        var repository = new FileServerRepository(
            tempDir.FilePath,
            path => ++calls == 1 ? throw new IOException("locked") : File.ReadAllText(path));
        Assert.Empty(repository.GetAll());

        // When
        repository.Add(SavedServer.Create("Third", "ghi789"));

        // Then
        var reopened = new FileServerRepository(tempDir.FilePath);
        Assert.Equal(new[] { "abc123", "def456", "ghi789" }, reopened.GetAll().Select(s => s.Address));
    }

    [Theory]
    [InlineData("{ invalid json")]
    [InlineData("""{"Name":"not an array"}""")]
    [InlineData("null")]
    [InlineData("")]
    public void GetAll_WhenFileIsCorrupt_ShouldKeepAByteIdenticalBackup(string content)
    {
        // Given
        using var tempDir = WithFile(content);
        var before = File.ReadAllBytes(tempDir.FilePath);
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        Assert.Empty(repository.GetAll());

        // Then
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath + ".bak"));
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Mutation_WhenFileIsCorrupt_ShouldLeaveTheBackupByteIdentical(string mutation)
    {
        // Given
        using var tempDir = WithFile("{ invalid json");
        var before = File.ReadAllBytes(tempDir.FilePath);
        var repository = new FileServerRepository(tempDir.FilePath);

        // When — a corrupt store starts empty; mutations on it may fail as "unknown address".
        try
        {
            Mutate(repository, mutation);
        }
        catch (ArgumentException)
        {
        }

        // Then
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath + ".bak"));
    }

    [Fact]
    public void Add_WhenFileIsCorrupt_ShouldReplaceItWithTheNewState()
    {
        // Given
        using var tempDir = WithFile("{ invalid json");
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        repository.Add(SavedServer.Create("Third", "ghi789"));

        // Then
        var reopened = new FileServerRepository(tempDir.FilePath);
        Assert.Equal("ghi789", Assert.Single(reopened.GetAll()).Address);
    }

    [Fact]
    public void Add_WhenFileIsCorruptAndBackupFails_ShouldRefuseTheWrite()
    {
        // Given — the .bak path is a directory, so the backup copy cannot be made.
        using var tempDir = WithFile("{ invalid json");
        Directory.CreateDirectory(tempDir.FilePath + ".bak");
        var before = File.ReadAllBytes(tempDir.FilePath);
        var repository = new FileServerRepository(tempDir.FilePath);

        // When / Then — without a backup the corrupt file is the only copy: never replace it.
        Assert.Throws<StoreUnavailableException>(() => repository.Add(SavedServer.Create("Third", "ghi789")));
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
    }

    [Fact]
    public void Add_WhenFileContainsInvalidEntries_ShouldPreserveThemVerbatimAfterTheValidOnes()
    {
        // Given
        using var tempDir = WithFile(
            """[null, {"Name":"First","Address":"abc123"}, {"Name":"  ","Address":"x"}, {"Name":5,"Address":"def456"}, {"Name":"Bad","Address":"not an address!"}]""");
        var repository = new FileServerRepository(tempDir.FilePath);

        // When
        repository.Add(SavedServer.Create("Second", "ghi789"));

        // Then — the valid ones lead in order, the invalid ones follow, untouched.
        var written = JsonNode.Parse(File.ReadAllText(tempDir.FilePath))!.AsArray();
        Assert.Equal(6, written.Count);
        Assert.Equal("abc123", written[0]!["Address"]!.GetValue<string>());
        Assert.Equal("ghi789", written[1]!["Address"]!.GetValue<string>());
        Assert.Null(written[2]);
        Assert.Equal("""{"Name":"  ","Address":"x"}""", written[3]!.ToJsonString());
        Assert.Equal("""{"Name":5,"Address":"def456"}""", written[4]!.ToJsonString());
        Assert.Equal("""{"Name":"Bad","Address":"not an address!"}""", written[5]!.ToJsonString());
        Assert.Equal(new[] { "abc123", "ghi789" }, repository.GetAll().Select(s => s.Address));
    }

    [Fact]
    public void Reads_ShouldParseTheFileOnlyOnce()
    {
        // Given
        using var tempDir = WithFile(TwoServers);
        var reads = 0;
        var repository = new FileServerRepository(
            tempDir.FilePath,
            path =>
            {
                reads++;
                return File.ReadAllText(path);
            });

        // When
        repository.GetAll();
        repository.FindByAddress("abc123");
        repository.FindByCfxId("abc123");
        repository.Add(SavedServer.Create("Third", "ghi789"));
        repository.GetAll();

        // Then
        Assert.Equal(1, reads);
        Assert.Equal(3, repository.GetAll().Count);
    }

    [Fact]
    public void GetAll_ShouldReturnASnapshotUnaffectedByLaterMutations()
    {
        // Given
        using var tempDir = WithFile(TwoServers);
        var repository = new FileServerRepository(tempDir.FilePath);
        var snapshot = repository.GetAll();

        // When
        repository.Add(SavedServer.Create("Third", "ghi789"));

        // Then
        Assert.Equal(2, snapshot.Count);
    }

    [Fact]
    public void Mutation_WhenWriteFails_ShouldLeaveTheInMemoryStoreUnchanged()
    {
        // Given — a read-only file makes the atomic replace throw.
        using var tempDir = WithFile(TwoServers);
        var repository = new FileServerRepository(tempDir.FilePath);
        repository.GetAll();
        File.SetAttributes(tempDir.FilePath, FileAttributes.ReadOnly);

        try
        {
            // When
            var thrown = Record.Exception(() => repository.Add(SavedServer.Create("Third", "ghi789")));

            // Then — memory still equals disk.
            Assert.True(thrown is IOException or UnauthorizedAccessException, thrown?.ToString());
            Assert.Equal(new[] { "abc123", "def456" }, repository.GetAll().Select(s => s.Address));
            Assert.Null(repository.FindByAddress("ghi789"));
        }
        finally
        {
            File.SetAttributes(tempDir.FilePath, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task Add_WhenCalledConcurrently_ShouldKeepEveryServer()
    {
        // Given — the background cfx-id capture writes off the UI thread.
        using var tempDir = new TempSettingsDirectory();
        var repository = new FileServerRepository(tempDir.FilePath);
        var addresses = Enumerable.Range(1, 20).Select(i => $"10.0.0.{i}:30120").ToArray();

        // When
        await Task.WhenAll(addresses.Select(a => Task.Run(() => repository.Add(SavedServer.Create(a, a)))));

        // Then
        var reopened = new FileServerRepository(tempDir.FilePath);
        Assert.Equal(addresses.Order(), reopened.GetAll().Select(s => s.Address).Order());
    }
}
