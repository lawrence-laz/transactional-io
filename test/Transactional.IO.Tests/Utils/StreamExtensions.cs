namespace Transactional.IO.Tests.Utils;

public static class StreamExtensions
{
    public static void ShouldBeEquivalent(this Stream actualStream, Stream expectedStream)
    {
        using var actualReader = new StreamReader(actualStream);
        using var expectedReader = new StreamReader(expectedStream);
        var actual = actualReader.ReadToEnd();
        var expected = expectedReader.ReadToEnd();
        actual.Should().Be(expected);
    }
}
