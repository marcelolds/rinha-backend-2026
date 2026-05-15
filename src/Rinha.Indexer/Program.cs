using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rinha.Core.Fraud;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Rinha.Indexer <references.json.gz> <references.bin>");
    return 2;
}

var inputPath = args[0];
var outputPath = args[1];

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 3;
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

await using var file = File.OpenRead(inputPath);
await using var gzip = new GZipStream(file, CompressionMode.Decompress);
await using var output = File.Create(outputPath);
using var writer = new BinaryWriter(output);

writer.Write(ReferenceIndex.Magic);
writer.Write(0);

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = false
};

var count = 0;
await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<ReferenceJsonRecord>(gzip, options))
{
    if (item is null)
    {
        continue;
    }

    if (item.Vector.Length != DetectionConstants.Dimensions)
    {
        throw new InvalidDataException($"Reference {count} has {item.Vector.Length} dimensions.");
    }

    for (var i = 0; i < DetectionConstants.Dimensions; i++)
    {
        writer.Write(ReferenceIndex.QuantizeValue(item.Vector[i]));
    }

    writer.Write(string.Equals(item.Label, "fraud", StringComparison.Ordinal) ? (byte)1 : (byte)0);
    count++;

    if (count % 250_000 == 0)
    {
        Console.WriteLine($"Indexed {count:N0} references...");
    }
}

writer.Flush();
output.Position = sizeof(ulong);
writer.Write(count);
writer.Flush();

Console.WriteLine($"Wrote {count:N0} references to {outputPath}.");
return 0;

internal sealed class ReferenceJsonRecord
{
    [JsonPropertyName("vector")]
    public float[] Vector { get; set; } = [];

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
}
