namespace ZYQDManager.DataDtos;

public class DbStatusDto
{
    public bool Connected { get; set; }
    public string Key { get; set; } = "Sunlight_Management_BU";
    public string Database { get; set; } = "";
    public long ElapsedMs { get; set; }
    public string? Error { get; set; }
}
