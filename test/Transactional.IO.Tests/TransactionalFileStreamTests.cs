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

    private static void AssertThatTemporaryFilesWereCleanedUp(string fileName)
    {
        Directory.GetFiles(Directory.GetCurrentDirectory(), fileName + "*")
            .Should()
            .HaveCount(1, "because temporary files are cleaned up");
    }
}
