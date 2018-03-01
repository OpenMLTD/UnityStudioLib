using System;
using System.IO;
using System.Reflection;
using UnityStudio.Extensions;
using UnityStudio.Models;
using UnityStudio.Serialization;

namespace UnityStudio.Tests {
    internal static class Program {

        private static void Main(string[] args) {
            if (args.Length < 2) {
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
            var mode = args[1].ToLowerInvariant();

            if (extension != ".unity3d") {
                PrintHelp();
                return;
            }

            switch (mode) {
                case "fumen":
                    ReadFumen(fileName);
                    break;
                case "costumedb":
                    ReadCostumeDatabase(fileName);
                    break;
                default:
                    PrintHelp();
                    return;
            }
        }

        private static void ReadFumen(string fileName) {
            ScoreObject scoreObj = null;
            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (var bundle = new BundleFile(fileStream, false)) {
                    foreach (var assetFile in bundle.AssetFiles) {
                        foreach (var preloadData in assetFile.PreloadDataList) {
                            if (preloadData.KnownType == KnownClassID.MonoBehaviour) {
                                var behaviour = preloadData.LoadAsMonoBehaviour(true);
                                if (behaviour.Name.Contains("fumen")) {
                                    behaviour = preloadData.LoadAsMonoBehaviour(false);
                                    var serializer = new MonobeBaviourSerializer();
                                    scoreObj = serializer.Deserialize<ScoreObject>(behaviour);
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

            Wait();
        }

        private static void ReadCostumeDatabase(string fileName) {
            string str = null;
            string pathName = null, name = null;

            using (var fileStream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (var bundle = new BundleFile(fileStream, false)) {
                    foreach (var assetFile in bundle.AssetFiles) {
                        foreach (var preloadData in assetFile.PreloadDataList) {
                            if (preloadData.KnownType == KnownClassID.TextAsset) {
                                var textAsset = preloadData.LoadAsTextAsset(false);

                                name = textAsset.Name;
                                pathName = textAsset.PathName;
                                str = textAsset.GetString();

                                break;
                            }
                        }
                    }
                }
            }

            if (str != null && pathName != null && name != null) {
                Console.WriteLine("Asset name: {0}", name);
                Console.WriteLine("Asset path: {0}", pathName);
                Console.WriteLine(str);
            } else {
                Console.WriteLine("(The asset is null, maybe the asset bundle does not contain a text asset.");
            }

            Wait();
        }

        private static void Wait() {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void PrintHelp() {
            var appAssembly = Assembly.GetAssembly(typeof(Program));
            var titleAttr = appAssembly.GetCustomAttribute<AssemblyTitleAttribute>();
            var title = titleAttr?.Title ?? "UnityStudioTest";

            var help = $@"
Usage:

    {title} <file> <mode>

Remarks:

    <file> must end in .unity3d (bundle).
    <mode> : fumen, costumedb
";
            Console.WriteLine(help);
        }

    }
}
