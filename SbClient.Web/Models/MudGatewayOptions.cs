namespace SbClient.Web.Models;

public sealed class MudGatewayOptions
{
    public const string SectionName = "MudGateway";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 4040;

    public int ScrollbackLineLimit { get; set; } = 500;
}
