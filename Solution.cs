using AutoMapper;
using BenchmarkDotNet.Attributes;
using Mapster;

namespace PerformanceTest
{
    public class BenchmarkClass
    {
        private IMapper autoMapper;
        private TypeAdapterConfig mapsterConfig;
        private List<Source> sourceList;

        [Params(1000, 10000, 100000)] // 定义不同的输入数据数量级
        public int DataSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // AutoMapper configuration
            var autoMapperConfig = new MapperConfiguration(cfg => cfg.CreateMap<Source, Destination>());
            autoMapper = autoMapperConfig.CreateMapper();

            // Mapster configuration
            mapsterConfig = new TypeAdapterConfig();
            mapsterConfig.NewConfig<Source, Destination>();

            // 生成输入数据
            sourceList = GenerateSourceData(DataSize);
        }

        [Benchmark]
        public List<Destination> AutoMapperMap()
        {
            return autoMapper.Map<List<Source>, List<Destination>>(sourceList);
        }

        [Benchmark]
        public List<Destination> MapsterAdapt()
        {
            return sourceList.Adapt<List<Destination>>(mapsterConfig);
        }

        private List<Source> GenerateSourceData(int dataSize)
        {
            var list = new List<Source>();
            for (int i = 0; i < dataSize; i++)
            {
                list.Add(new Source { Id = i, Name = $"Test {i}" });
            }
            return list;
        }
    }

    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Destination
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

}
