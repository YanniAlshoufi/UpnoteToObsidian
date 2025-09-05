using UpnoteToObsidian;

const string inputPath = "input";
const string outputPath = "output";

Console.WriteLine("🚀 Starting UpnoteToObsidian conversion...");

var result = FileProcessing.ProcessAllInputFolders(inputPath, outputPath);

if (result.IsSuccess)
{
    Console.WriteLine("✅ Conversion completed successfully!");
}
else
{
    Console.WriteLine($"❌ Conversion failed: {result.Error}");
    return 1;
}

return 0;
