namespace Transactional.IO.Tests;

public class FileStreamEquivalencyTests
{
    [Theory, AutoData]
    public void File_mode_open_throws_when_file_does_not_exists(string fileName)
    {
        var fileStreamConstructor = () => new FileStream(fileName, FileMode.Open);
        fileStreamConstructor.Should().ThrowExactly<FileNotFoundException>();

        var transactionalFileStreamConstructor = () => new TransactionalFileStream(fileName, FileMode.Open);
        transactionalFileStreamConstructor.Should().ThrowExactly<FileNotFoundException>();
    }

    [Theory, AutoData]
    public void File_mode_create_new_throws_when_file_already_exists(string fileName)
    {
        using (File.Create(fileName))
        {
            // Create a file and close the stream immediately. 
        }

        var fileStreamConstructor = () => new FileStream(fileName, FileMode.CreateNew);
        fileStreamConstructor.Should().ThrowExactly<IOException>();

        var transactionalFileStreamConstructor = () => new TransactionalFileStream(fileName, FileMode.CreateNew);
        transactionalFileStreamConstructor.Should().ThrowExactly<IOException>();
    }

    [Theory]
    [InlineAutoData(FileMode.Append)]
    [InlineAutoData(FileMode.Create)]
    [InlineAutoData(FileMode.OpenOrCreate)]
    public void Some_modes_create_when_file_does_not_exists(FileMode mode, string fileName1, string fileName2)
    {
        using var fileStream = new FileStream(fileName1, mode);
        File.Exists(fileName1).Should().BeTrue();

        {
            using var transcationalFileStream = new TransactionalFileStream(fileName2, mode);
            // As expected, the transaction has to be commited and disposed before the file appears.
            transcationalFileStream.Commit();
        }
        File.Exists(fileName2).Should().BeTrue();
    }

    [Theory]
    [InlineAutoData(FileMode.Append)]
    [InlineAutoData(FileMode.Create)]
    [InlineAutoData(FileMode.OpenOrCreate)]
    [InlineAutoData(FileMode.Truncate)]
    public void Write_modes_modify_file_same_way_as_file_stream_does(
        FileMode mode,
        string fileNameExpected,
        string fileNameActual,
        string initialContent,
        string modifiedContent)
    {
        File.WriteAllText(fileNameExpected, initialContent);
        File.WriteAllText(fileNameActual, initialContent);

        using (var fileStream = new FileStream(fileNameExpected, mode))
        {
            using var writer = new StreamWriter(fileStream);
            writer.WriteLine(modifiedContent);
        }

        using (var transactionalFileStream = new TransactionalFileStream(fileNameActual, mode))
        {
            using var writer = new StreamWriter(transactionalFileStream);
            writer.WriteLine(modifiedContent);
            transactionalFileStream.Commit();
        }

        var expected = File.ReadAllText(fileNameExpected);
        var actual = File.ReadAllText(fileNameActual);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineAutoData(FileMode.Open)]
    [InlineAutoData(FileMode.OpenOrCreate)]
    public void Open_modes_read_file_same_way_as_file_stream_does(
        FileMode mode,
        string fileNameExpected,
        string fileNameActual,
        string content)
    {
        File.WriteAllText(fileNameExpected, content);
        File.WriteAllText(fileNameActual, content);

        using var fileStream = new FileStream(fileNameExpected, mode);
        using var transactionalFileStream = new TransactionalFileStream(fileNameActual, mode);

        transactionalFileStream.ShouldBeEquivalent(fileStream);
    }
}
