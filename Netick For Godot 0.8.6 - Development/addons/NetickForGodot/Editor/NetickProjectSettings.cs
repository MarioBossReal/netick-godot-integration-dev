using Godot;

namespace NetickEditor;

public static class NetickProjectSettings
{
    private static readonly string DefaultConfigPathSetting = "netick/project/default_config_path";

    public static string FullGameAssemblyPath => $"res://.godot/mono//temp//bin//Debug//{ProjectSettings.GetSetting("dotnet/project/assembly_name")}.dll";

    /// <summary>
    /// Returns the default path to this project's <see cref="Netick.GodotEngine.NetickConfig"/>.
    /// </summary>
    /// <returns></returns>
    public static string GetDefaultConfigPath()
    {
        if (ProjectSettings.HasSetting(DefaultConfigPathSetting))
        {
            return ProjectSettings.GetSetting(DefaultConfigPathSetting).As<string>();
        }

        var path = "res://netick_config.tres";

        ProjectSettings.SetSetting(DefaultConfigPathSetting, path);

        return path;
    }
}
