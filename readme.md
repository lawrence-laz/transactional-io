[![NuGet Version](https://img.shields.io/nuget/v/Transactional.IO?label=NuGet)](https://www.nuget.org/packages/Poem/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Transactional.IO?label=Downloads)](https://www.nuget.org/packages/Poem/)
[![Build](https://github.com/lawrence-laz/transactional-io/workflows/Build/badge.svg)](https://github.com/lawrence-laz/poem/actions?query=workflow%3ABuild)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/7ff922b8f755431ea5a1fa59e59c534a)](https://www.codacy.com/gh/lawrence-laz/transactional-io/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=lawrence-laz/transactional-io&amp;utm_campaign=Badge_Grade)
[![Codacy Badge](https://app.codacy.com/project/badge/Coverage/7ff922b8f755431ea5a1fa59e59c534a)](https://www.codacy.com/gh/lawrence-laz/transactional-io/dashboard?utm_source=github.com&utm_medium=referral&utm_content=lawrence-laz/transactional-io&utm_campaign=Badge_Coverage)

# Transactional.IO
A dead simple way to manage your `FileStream` in a transactional way to ensure 
that the file does not get corrupted if things don't go as planned.

Take an example with `XmlWriter` (note that any `StreamWriter` is compatible, including the direct access!)
```csharp
using Transactional.IO;
using System.Xml;

using var stream = new TransactionalFileStream("my-file.xml", FileMode.Truncate);
using var writer = XmlWriter.Create(stream);

var userFullName = "";

writer.WriteStartDocument();
writer.WriteStartElement("User");
if (userFullName == "")
{
    // ❌ This interrupts execution and writer does not finish.
    // Your file would be left corrupted with half of XML missing.
    throw new Exception("Uh-oh!"); 
}
writer.WriteValue(userFullName);
writer.WriteEndElement();

writer.WriteEndDocument();

// ✅ But don't fret! 
// As long as .Commit() was not called, the original file is left unchanged.
stream.Commit();
```

## Get started
Download from [nuget.org](https://www.nuget.org/packages/Transactional.IO/):
```powershell
dotnet add package Transactional.IO
```

