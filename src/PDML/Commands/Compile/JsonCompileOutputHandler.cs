using PDML.Common;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Diagnostics.Abstractions;

namespace PDML.Commands.Compile;

internal class JsonCompileOutputHandler : JsonOutputHandler<CompileResult>
{
    public override void OnResult(CompileResult result)
    {
        WriteJson(new ResultDto
        {
            Diagnostics = result.Diagnostics.Select(DiagnosticDto.From).ToList(),
            OutputFiles = result.OutputFiles.ToDictionary(
                item => item.LocalName,
                item => item.Content
            )
        });
    }

    private record ResultDto
    {
        public required IReadOnlyList<DiagnosticDto> Diagnostics { get; init; }

        public required IReadOnlyDictionary<string, string> OutputFiles { get; init; }
    }

    private record DiagnosticDto
    {
        public required int Code { get; init; }
        public required DiagnosticSeverity Severity { get; init; }
        public required LocationDto Location { get; init; }
        public required IReadOnlyList<LocationDto> AdditionalLocations { get; init; }
        public required string Message { get; init; }

        public static DiagnosticDto From(Diagnostic diagnostic)
            => new DiagnosticDto
            {
                Code = diagnostic.Descriptor.Code,
                Severity = diagnostic.Descriptor.Severity,
                Location = LocationDto.From(diagnostic.Location),
                AdditionalLocations = diagnostic.AdditionalLocations.Select(LocationDto.From).ToList(),
                Message = diagnostic.Message
            };
    }

    private record LocationDto
    {
        public required string SourceFile { get; init; }
        public required CoordinateDto Start { get; init; }
        public required CoordinateDto End { get; init; }

        public static LocationDto From(Location location)
            => new LocationDto
            {
                SourceFile = location.SourceFile.FilePath,
                Start = CoordinateDto.From(location.Start),
                End = CoordinateDto.From(location.End)
            };
    }

    private record CoordinateDto
    {
        public required int Position { get; init; }
        public required int Line { get; init; }
        public required int Column { get; init; }

        public static CoordinateDto From(Coordinate coordinate)
            => new CoordinateDto
            {
                Position = coordinate.Position,
                Line = coordinate.Line,
                Column = coordinate.Column
            };
    }
}