namespace ZYQDManager.DataDtos;

public sealed class LibreOfficeOptions
{
    public const string SectionName = "LibreOffice";

    public string SofficePath { get; set; } = @"C:\Program Files\LibreOffice\program\soffice.com";
    public int TimeoutSeconds { get; set; } = 90;
}
