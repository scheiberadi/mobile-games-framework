using System.IO;
using System.Xml.Linq;
using UnityEditor.Android;

// Declares Android Auto Backup rules for the local save (PlayerPrefs live in the app's shared
// preferences). It edits the generated launcher module, so the Main Manifest override slot is not used.
public class EvaAndroidPostProcess : IPostGenerateGradleAndroidProject
{
    private const string Full =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<full-backup-content>\n    <include domain=\"sharedpref\" path=\".\"/>\n</full-backup-content>\n";
    private const string Extraction =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<data-extraction-rules>\n    <cloud-backup>\n        <include domain=\"sharedpref\" path=\".\"/>\n    </cloud-backup>\n    <device-transfer>\n        <include domain=\"sharedpref\" path=\".\"/>\n    </device-transfer>\n</data-extraction-rules>\n";

    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
    {
        var launcher = Path.GetFullPath(Path.Combine(unityLibraryPath, "..", "launcher"));
        var manifest = Path.Combine(launcher, "src", "main", "AndroidManifest.xml");
        var xmlDir = Path.Combine(launcher, "src", "main", "res", "xml");
        Directory.CreateDirectory(xmlDir);
        File.WriteAllText(Path.Combine(xmlDir, "eva_full_backup_content.xml"), Full);
        File.WriteAllText(Path.Combine(xmlDir, "eva_data_extraction_rules.xml"), Extraction);

        XNamespace a = "http://schemas.android.com/apk/res/android";
        var doc = XDocument.Load(manifest);
        var app = doc.Root.Element("application")
            ?? throw new System.InvalidOperationException("No <application> element in " + manifest);
        app.SetAttributeValue(a + "allowBackup", "true");
        app.SetAttributeValue(a + "fullBackupContent", "@xml/eva_full_backup_content");
        app.SetAttributeValue(a + "dataExtractionRules", "@xml/eva_data_extraction_rules");
        doc.Save(manifest);
    }
}
