namespace WaterOps.Repositories.Helpers;

public static class PathHelper
{
    public static string BasePath
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WaterOS"
            );

            // Ensure the directory exists on the file system
            Directory.CreateDirectory(path);

            return path;
        }
    }
}
