using System.Text;
using ET;
using OfficeOpenXml;

ExcelPackage.License.SetNonCommercialOrganization("ETET");

try
{
    Console.OutputEncoding = Encoding.UTF8;
    Console.InputEncoding = Encoding.UTF8;

    if (args.Length > 0 && args[0].ToLower() == "cli")
    {
        var exitCode = await CliRunner.RunAsync(args);
        Environment.Exit(exitCode);
    }
    else
    {
        Console.Error.WriteLine("[INFO] EPPlus MCP Server - Excel专用服务器");
        await Console.Error.FlushAsync();

        var server = new McpServer();
        await server.RunAsync();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] Fatal error: {ex.GetType().Name}");
#if DEBUG
    Console.Error.WriteLine($"[ERROR] Details: {ex.Message}");
    Console.Error.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
#else
    Console.Error.WriteLine($"[ERROR] An internal error occurred. Check logs for details.");
#endif
    Environment.Exit(1);
}
