namespace Rinha.Core.Fraud;

public sealed class ReferenceIndex
{
    public const ulong Magic = 0x31514449484E4952; // RINHIDQ1, little-endian
    private const int BucketCount = 1 << 10;
    private const int MaxCandidatesPerBucket = 1024;

    private readonly byte[] _vectors;
    private readonly byte[] _labels;
    private readonly int[] _bucketOffsets;
    private readonly int[] _bucketRows;

    private ReferenceIndex(byte[] vectors, byte[] labels, int[] bucketOffsets, int[] bucketRows)
    {
        _vectors = vectors;
        _labels = labels;
        _bucketOffsets = bucketOffsets;
        _bucketRows = bucketRows;
        Count = labels.Length;
    }

    public int Count { get; }

    public static ReferenceIndex Empty { get; } = new([], [], new int[BucketCount + 1], []);

    public static ReferenceIndex Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadUInt64();
        if (magic != Magic)
        {
            throw new InvalidDataException($"Invalid index file magic at '{path}'.");
        }

        var count = reader.ReadInt32();
        if (count < 0)
        {
            throw new InvalidDataException($"Invalid index vector count at '{path}'.");
        }

        var vectors = new byte[count * DetectionConstants.Dimensions];
        var labels = new byte[count];

        for (var i = 0; i < count; i++)
        {
            var offset = i * DetectionConstants.Dimensions;
            for (var d = 0; d < DetectionConstants.Dimensions; d++)
            {
                vectors[offset + d] = reader.ReadByte();
            }

            labels[i] = reader.ReadByte();
        }

        var (bucketOffsets, bucketRows) = BuildBuckets(vectors, count);
        return new ReferenceIndex(vectors, labels, bucketOffsets, bucketRows);
    }

    public static void Write(string path, IReadOnlyList<float[]> vectors, IReadOnlyList<byte> labels)
    {
        if (vectors.Count != labels.Count)
        {
            throw new ArgumentException("Vector and label counts must match.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(Magic);
        writer.Write(vectors.Count);

        for (var i = 0; i < vectors.Count; i++)
        {
            if (vectors[i].Length != DetectionConstants.Dimensions)
            {
                throw new InvalidDataException($"Vector {i} has {vectors[i].Length} dimensions.");
            }

            for (var d = 0; d < DetectionConstants.Dimensions; d++)
            {
                writer.Write(QuantizeValue(vectors[i][d]));
            }

            writer.Write(labels[i]);
        }
    }

    public byte GetFraudCountAmongNearest5(ReadOnlySpan<float> query)
    {
        if (Count == 0)
        {
            return 0;
        }

        Span<byte> quantizedQuery = stackalloc byte[DetectionConstants.Dimensions];
        for (var i = 0; i < DetectionConstants.Dimensions; i++)
        {
            quantizedQuery[i] = QuantizeValue(query[i]);
        }

        Span<int> bestDistances = stackalloc int[DetectionConstants.K];
        Span<byte> bestLabels = stackalloc byte[DetectionConstants.K];

        for (var i = 0; i < bestDistances.Length; i++)
        {
            bestDistances[i] = int.MaxValue;
        }

        var bucket = GetBucket(quantizedQuery);

        SearchBucket(bucket, quantizedQuery, bestDistances, bestLabels);
        return CountFrauds(bestLabels);
    }

    private void SearchBucket(int bucket, ReadOnlySpan<byte> query, Span<int> bestDistances, Span<byte> bestLabels)
    {
        var start = _bucketOffsets[bucket];
        var end = _bucketOffsets[bucket + 1];

        if (end - start < DetectionConstants.K)
        {
            return;
        }

        var candidateCount = end - start;
        if (candidateCount <= MaxCandidatesPerBucket)
        {
            for (var i = start; i < end; i++)
            {
                var row = _bucketRows[i];
                var distance = SquaredDistance(query, row);
                InsertIfBetter(distance, _labels[row], bestDistances, bestLabels);
            }

            return;
        }

        for (var i = 0; i < MaxCandidatesPerBucket; i++)
        {
            var index = start + (i * candidateCount / MaxCandidatesPerBucket);
            var row = _bucketRows[index];
            var distance = SquaredDistance(query, row);
            InsertIfBetter(distance, _labels[row], bestDistances, bestLabels);
        }
    }

    private int SquaredDistance(ReadOnlySpan<byte> query, int row)
    {
        var offset = row * DetectionConstants.Dimensions;
        var sum = 0;

        for (var d = 0; d < DetectionConstants.Dimensions; d++)
        {
            var diff = query[d] - _vectors[offset + d];
            sum += diff * diff;
        }

        return sum;
    }

    private static void InsertIfBetter(int distance, byte label, Span<int> bestDistances, Span<byte> bestLabels)
    {
        var worstIndex = 0;
        var worstDistance = bestDistances[0];

        for (var i = 1; i < bestDistances.Length; i++)
        {
            if (bestDistances[i] > worstDistance)
            {
                worstDistance = bestDistances[i];
                worstIndex = i;
            }
        }

        if (distance < worstDistance)
        {
            bestDistances[worstIndex] = distance;
            bestLabels[worstIndex] = label;
        }
    }

    public static byte QuantizeValue(float value)
    {
        if (value < 0f)
        {
            return 0;
        }

        var clamped = DetectionConstants.Clamp01(value);
        return (byte)(1 + MathF.Round(clamped * 254f));
    }

    private static (int[] Offsets, int[] Rows) BuildBuckets(byte[] vectors, int count)
    {
        var offsets = new int[BucketCount + 1];

        for (var row = 0; row < count; row++)
        {
            var bucket = GetBucket(vectors.AsSpan(row * DetectionConstants.Dimensions, DetectionConstants.Dimensions));
            offsets[bucket + 1]++;
        }

        for (var i = 1; i < offsets.Length; i++)
        {
            offsets[i] += offsets[i - 1];
        }

        var rows = new int[count];
        var cursor = new int[BucketCount];
        Array.Copy(offsets, cursor, BucketCount);

        for (var row = 0; row < count; row++)
        {
            var bucket = GetBucket(vectors.AsSpan(row * DetectionConstants.Dimensions, DetectionConstants.Dimensions));
            rows[cursor[bucket]++] = row;
        }

        return (offsets, rows);
    }

    private static int GetBucket(ReadOnlySpan<byte> vector)
    {
        var lastTransactionNull = vector[5] == 0 && vector[6] == 0 ? 1 : 0;
        var online = vector[9] > 128 ? 1 : 0;
        var cardPresent = vector[10] > 128 ? 1 : 0;
        var unknownMerchant = vector[11] > 128 ? 1 : 0;
        var flags = (((lastTransactionNull << 1) | online) << 1 | cardPresent) << 1 | unknownMerchant;
        var amountBin = Bin4(vector[0]);
        var txCountBin = Bin4(vector[8]);
        var mccRiskBin = Bin4(vector[12]);

        return EncodeBucket(flags, amountBin, txCountBin, mccRiskBin);
    }

    private static int Bin4(byte value)
    {
        return Math.Min(3, value >> 6);
    }

    private static int EncodeBucket(int flags, int amountBin, int txCountBin, int mccRiskBin)
    {
        return (((flags << 2) | amountBin) << 2 | txCountBin) << 2 | mccRiskBin;
    }

    private static byte CountFrauds(ReadOnlySpan<byte> bestLabels)
    {
        byte frauds = 0;
        for (var i = 0; i < bestLabels.Length; i++)
        {
            frauds += bestLabels[i];
        }

        return frauds;
    }
}
