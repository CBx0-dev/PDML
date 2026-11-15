using PDML.Common;
using PDML.Generator.Abstractions;

namespace PDML.Commands.Generators.List;

internal sealed class JsonListOutputHandler : JsonOutputHandler<ListResult>
{
    public override void OnResult(ListResult result)
    {
        WriteJson(new ResultDto
        {
            Generators = result.Generators.Select(GeneratorDto.From).ToList()
        });
    }

    private record ResultDto
    {
        public required IReadOnlyList<GeneratorDto> Generators { get; init; }
    }

    private record GeneratorDto
    {
        public required string Name { get; init; }
        public required string Key { get; init; }
        public required string Assembly { get; init; }

        public static GeneratorDto From(CodeGeneratorBuilder generator)
            => new GeneratorDto
            {
                Name = generator.Name,
                Key = generator.Key,
                Assembly = generator.GetType().Assembly.ToString()
            };
    }
}