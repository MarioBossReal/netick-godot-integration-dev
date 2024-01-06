#if TOOLS
using Godot;
using System.IO;

namespace NetickEditor;

[Tool]
public partial class NetickExportPlugin : EditorExportPlugin
{
    string _assemblyPath;

    public override void _ExportBegin(string[] features, bool isDebug, string path, uint flags)
    {
        _assemblyPath = null;
        var fileName = Path.GetFileName(path);
        var folder = path.Replace(fileName, "");
        var folders = System.IO.Directory.GetDirectories(folder, "*", System.IO.SearchOption.AllDirectories);
        var asmName = Path.GetFileName(NetickProjectSettings.FullGameAssemblyPath);

        for (int i = 0; i < folders.Length; i++)
        {
            var p = $"{folders[i]}/{asmName}";
            bool doesExist = System.IO.File.Exists(p);

            if (doesExist)
            {
                _assemblyPath = p;
                break;
            }
        }
    }

    public override void _ExportEnd()
    {
        if (_assemblyPath != null)
        {
            Netick.CodeGen.Processor.ProcessAssembly(new GodotCodeGen(), _assemblyPath);
            GD.Print("Netick Editor: export done.");
        }
        else
        {
            GD.PrintErr("Netick Editor: export failed.");
        }
    }
}
#endif