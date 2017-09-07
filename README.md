# UnityStudioLib

Packing [Unity Studio](https://github.com/Perfare/UnityStudio) into a class library.
Only essential parts are implemented.

Requires .NET Framework 4.5.

## Building

```bash
git clone https://github.com/hozuki/UnityStudioLib.git --recursive
cd UnityStudioLib
nuget restore UnityStudioLib.sln
msbuild UnityStudioLib.sln /p:Configuration=Release
```

## License

This repository: MIT

For the license of tracking repository, see [here](tracking/UnityStudio/License.md).
