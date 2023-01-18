using System.Xml;
using Transactional.IO;

// Navigate to examples/XmlWriterExample and run `dotnet run`

// Let's read settings file and see what's inside
Console.WriteLine(File.ReadAllText("./bin/Debug/net6.0/Settings.xml"));
Console.WriteLine("------------------------------");

// Now, let's try to update settings with our new settings object
try
{
    using var stream = new TransactionalFileStream("./bin/Debug/net6.0/Settings.xml", FileMode.Truncate);
    using var writer = XmlWriter.Create(stream);

    var settings = new Settings
    {
        BackgroundColor = "Red"
    };

    writer.WriteStartDocument();
    writer.WriteStartElement("Settings");

    writer.WriteStartElement("BackgroundColor");
    writer.WriteValue(settings.BackgroundColor);
    writer.WriteEndElement();

    writer.WriteStartElement("ScreenSize");

    writer.WriteStartElement("X");
    writer.WriteValue(settings.ScreenSize.X);
    writer.WriteEndElement();

    writer.WriteStartElement("Y");
    writer.WriteValue(settings.ScreenSize.Y);
    writer.WriteEndElement();

    writer.WriteEndElement();

    writer.WriteEndElement();
    writer.WriteEndDocument();

    stream.Commit();
}
catch (Exception)
{
    // Whoops! I forgot to initialize Settings.ScreenSize and got object reference not set exception.
}

// Let's see how our settings file looks like after all this mess...
Console.WriteLine(File.ReadAllText("./bin/Debug/net6.0/Settings.xml"));
Console.WriteLine("------------------------------");

// It's unchanged! The file was not corrupted, because we did not commit the changes.
// Now try replacing the TransactionalFileStream with a simple FileStream and 
// you will see that Settings.xml becomes corruped.

public class Settings
{
    public ScreenSize? ScreenSize { get; set; }
    public string? BackgroundColor { get; set; }
}

public class ScreenSize
{
    public int X { get; set; }
    public int Y { get; set; }
}
