using BenchmarkDotNet.Attributes;

namespace FaFPlayground
{
    public class BenchmarkAlgos
    {
        private readonly NavLabelCache _cache;

        public BenchmarkAlgos()
        {
            var data = new List<bool[]>(512);

            var random = new Random(48873);

            for (int z = 0; z < 512; z++)
            {
                var entry = new bool[512];
                for (int x = 0; x < 512; x++)
                {
                    entry[x] = random.Next(100) <= 90;
                }

                data.Add(entry);
            }

            _cache = new NavLabelCache(data);
        }

        [Benchmark]
        public void Existing()
        {
            var t = GetTree();
            t.CompressReference(_cache, 8);
        }

        [Benchmark]
        public void Recursive()
        {
            var t = GetTree();
            t.Compress(_cache, 8);
        }

        private CompressedLabelTree GetTree()
        {
            Globals.NavLayerData = new NavLayerData();
            return new(NavLayers.Air, 0, 0, 512, 0, 0);
        }
    }
}
