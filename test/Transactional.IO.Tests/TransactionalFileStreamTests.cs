using System.Linq;
using System.Threading.Tasks;

namespace Transactional.IO.Tests;

public class TransactionalFileStreamTests
{
    [Theory, AutoData]
    public void TransactionalFileStream_does_not_modify_file_if_Commit_is_not_called(
        string fileName,
        string unmodifiedContent,
        string modifiedContent)
    {
        {
            // Arrange
            File.WriteAllText(fileName, unmodifiedContent);
            using var fileStream = new TransactionalFileStream(fileName, FileMode.Truncate);
            using var writer = new StreamWriter(fileStream);

            // Act
            writer.Write(modifiedContent);
        }

        // Assert
        File.ReadAllText(fileName)
            .Should()
            .Be(unmodifiedContent, "because .Commit() was not called");
        AssertThatTemporaryFilesWereCleanedUp(fileName);
    }

    [Theory, AutoData]
    public void TransactionalFileStream_recovers_if_temporary_files_get_corrupted(
        string fileName,
        string unmodifiedContent,
        string modifiedContent)
    {
        // Arrange
        File.WriteAllText(fileName, unmodifiedContent);
        var act = new Action(() =>
        {
            using var fileStream = new TransactionalFileStream(fileName, FileMode.Truncate);
            using var writer = new StreamWriter(fileStream);

            writer.Write(modifiedContent);
            writer.Flush();

            var tempFileName = Directory
                .GetFiles(Directory.GetCurrentDirectory(), $"{fileName}.*.tmp")
                .First();
            File.Delete(tempFileName);

            fileStream.Commit();
        });

        // Act & Assert
        act.Should().Throw<FileStreamCompleteTransactionException>();
        File.ReadAllText(fileName)
            .Should()
            .Be(unmodifiedContent, "because transaction failed to complete");
        AssertThatTemporaryFilesWereCleanedUp(fileName);
    }


    [Theory, AutoData]
    public void TransactionalFileStream_does_modify_file_when_Commit_is_called(
        string fileName,
        string unmodifiedContent,
        string modifiedContent)
    {
        {
            // Arrange
            File.WriteAllText(fileName, unmodifiedContent);
            using var fileStream = new TransactionalFileStream(fileName, FileMode.Truncate);
            using var writer = new StreamWriter(fileStream);

            // Act
            writer.Write(modifiedContent);
            fileStream.Commit();
        }

        // Assert
        File.ReadAllText(fileName)
            .Should()
            .Be(modifiedContent, "because .Commit() was called");

        AssertThatTemporaryFilesWereCleanedUp(fileName);
    }

    [Theory, AutoData]
    public void TransactionalFileStream_throws_when_Commit_is_called_twice(
        string fileName,
        string content)
    {
        {
            // Arrange
            File.WriteAllText(fileName, content);
            using var fileStream = new TransactionalFileStream(fileName, FileMode.Truncate);

            // Act & Assert
            fileStream.Commit();
            fileStream
                .Invoking(x => x.Commit())
                .Should()
                .Throw<InvalidOperationException>("because .Commit() was called twice");
        }

        AssertThatTemporaryFilesWereCleanedUp(fileName);
    }

    [Theory, AutoData]
    public void TransactionalFileStream_throws_when_Commit_is_called_on_disposed_stream(
        string fileName,
        string content)
    {
        {
            // Arrange
            File.WriteAllText(fileName, content);
            using var fileStream = new TransactionalFileStream(fileName, FileMode.Truncate);
            fileStream.Dispose();

            // Act & Assert
            fileStream
                .Invoking(x => x.Commit())
                .Should()
                .Throw<InvalidOperationException>("because cannot commit a closed stream");
        }

        AssertThatTemporaryFilesWereCleanedUp(fileName);
    }

    private static void AssertThatTemporaryFilesWereCleanedUp(string fileName, int expectedCount = 1)
    {
        Directory.GetFiles(Directory.GetCurrentDirectory(), fileName + "*")
            .Should()
            .HaveCount(expectedCount, "because temporary files are cleaned up");
    }

    [Theory]
    [InlineAutoData(FileMode.Append)]
    [InlineAutoData(FileMode.Create)]
    [InlineAutoData(FileMode.OpenOrCreate)]
    public async Task TransactionalFileStreamTests_creates_a_new_file_on_commit_for_some_modes(
        FileMode mode,
        string fileName,
        string expected)
    {
        {
            // Arrange
            using var fileStream = new TransactionalFileStream(fileName, mode);
            using var writer = new StreamWriter(fileStream);

            // Act
            await writer.WriteAsync(expected);
            fileStream.Commit();
        }

        // Assert
        File.ReadAllText(fileName).Should().Be(expected);
        AssertThatTemporaryFilesWereCleanedUp(fileName);
    }

    [Theory]
    [InlineAutoData(FileMode.Append)]
    [InlineAutoData(FileMode.Create)]
    [InlineAutoData(FileMode.OpenOrCreate)]
    public async Task TransactionalFileStreamTests_does_not_create_a_new_file_without_commit(
        FileMode mode,
        string fileName,
        string expected)
    {
        {
            // Arrange
            using var fileStream = new TransactionalFileStream(fileName, mode);
            using var writer = new StreamWriter(fileStream);

            // Act
            await writer.WriteAsync(expected);
        }

        // Assert
        File.Exists(fileName).Should().BeFalse();
        AssertThatTemporaryFilesWereCleanedUp(fileName, expectedCount: 0);
    }
}
