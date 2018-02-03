using System;
using System.IO;
using System.Reflection;
using UnityStudio.Extensions;
using UnityStudio.Models;
using UnityStudio.Serialization;

namespace UnityStudio.Tests {
    internal static class Program {

        private static void Main(string[] args) {
            if (args.Length == 0) {
                PrintHelp();
                return;
            }

            var fileName = args[0];
            var extension = Path.GetExtension(fileName);

            if (extension == null) {
                PrintHelp();
                return;
            }

            extension = extension.ToLowerInvariant();
            switch (extension) {
                case ".unity3d":
                    ReadBundle(fileName);
                    break;
                case ".asset":
                    ReadAsset(fileName);
                    break;
                default:
                    PrintHelp();
                    return;
            }
        }

        private static void ReadBundle(string fileName) {
            ScoreObject scoreObj = null;
            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (var bundle = new BundleFile(fileStream, false)) {
                    foreach (var assetFile in bundle.AssetFiles) {
                        foreach (var preloadData in assetFile.PreloadDataList) {
                            if (preloadData.KnownType == KnownClassID.MonoBehaviour) {
                                var behavior = preloadData.LoadAsMonoBehavior(true);
                                if (behavior.Name.Contains("fumen")) {
                                    behavior = preloadData.LoadAsMonoBehavior(false);
                                    var serializer = new MonoBehaviorSerializer();
                                    scoreObj = serializer.Deserialize<ScoreObject>(behavior);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (scoreObj != null) {
                Console.WriteLine("Total notes: {0}", scoreObj.NoteEvents.Length);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void ReadAsset(string fileName) {
            // Do nothing
        }

        private static void PrintHelp() {
            var appAssembly = Assembly.GetAssembly(typeof(Program));
            var titleAttr = appAssembly.GetCustomAttribute<AssemblyTitleAttribute>();
            var title = titleAttr?.Title ?? "UnityStudioTest";

            var help = $@"
Usage:

    {title} <file>

Remarks:

    <file> must end in .unity3d (bundle) or .asset (asset).
";
            Console.WriteLine(help);
        }

    }
}
