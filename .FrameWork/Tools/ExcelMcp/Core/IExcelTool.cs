using System.Text.Json.Nodes;

namespace ET.Tools;

public interface IExcelTool
{
    string Description { get; }

    object InputSchema { get; }

    Task<string> ExecuteAsync(JsonObject? arguments);
}

public interface IAnnotatedTool : IExcelTool
{
    bool? IsReadOnly { get; }

    bool? IsDestructive { get; }
}
