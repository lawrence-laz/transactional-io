namespace Transactional.IO.Tests;

public class FileStreamTests
{
    [Theory, AutoData]
    public void FileStream_changes_file_contents_right_after_writing(
        string fileName,
        string contentBefore,
        string expectedModified)
    {
        {
            // Arrange
            File.WriteAllText(fileName, contentBefore);
            using var fileStream = new FileStream(fileName, FileMode.Open);
            using var writer = new StreamWriter(fileStream);

            // Act
            writer.Write(expectedModified);
        }

        // Assert
        File.ReadAllText(fileName)
            .Should()
            .Be(expectedModified, "because FileStream modifies files directly");
    }
}

