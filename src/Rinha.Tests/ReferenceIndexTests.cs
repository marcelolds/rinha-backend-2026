using Rinha.Core.Fraud;
using Xunit;

namespace Rinha.Tests;

public sealed class ReferenceIndexTests
{
    [Fact]
    public void Load_AndFindNearestFraudCount()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rinha-index-{Guid.NewGuid():N}.bin");

        try
        {
            var vectors = new List<float[]>
            {
                Vector(0.10f),
                Vector(0.11f),
                Vector(0.12f),
                Vector(0.13f),
                Vector(0.14f),
                Vector(0.90f)
            };
            var labels = new List<byte> { 1, 1, 0, 0, 0, 1 };

            ReferenceIndex.Write(path, vectors, labels);
            var index = ReferenceIndex.Load(path);

            Span<float> query = stackalloc float[DetectionConstants.Dimensions];
            Vector(0.105f).AsSpan().CopyTo(query);

            var frauds = index.GetFraudCountAmongNearest5(query);

            Assert.Equal(2, frauds);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static float[] Vector(float value)
    {
        var vector = new float[DetectionConstants.Dimensions];
        Array.Fill(vector, value);
        vector[5] = -1f;
        vector[6] = -1f;
        vector[9] = 0f;
        vector[10] = 1f;
        vector[11] = 0f;
        vector[12] = 0.15f;
        return vector;
    }
}
