#if TOOLS
using Godot;
using System.IO;

namespace NetickEditor;

[Tool]
public partial class NetickExportPlugin : EditorExportPlugin
{
    internal NetickGodotEditor _editor;
    string _assemblyPath;

    public override void _ExportBegin(string[] features, bool isDebug, string path, uint flags)
    {
        _assemblyPath = null;
        var fileName = Path.GetFileName(path);
        var folder = path.Replace(fileName, "");     //GD.Print($"MAIN FOLDER {folder}");
        var folders = System.IO.Directory.GetDirectories(folder, "*", System.IO.SearchOption.AllDirectories);
        var asmName = Path.GetFileName(_editor._editorConfig.MainEditorGameAssemblyPath);

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