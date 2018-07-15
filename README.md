# UnityStudioLib

Packing [Unity Studio](https://github.com/Perfare/UnityStudio) into a class library.
Only essential parts are implemented.

Requires .NET Framework 4.5.

> None of the repo, the tool, nor the repo owner is affiliated with, or sponsored or authorized by
> Unity Technologies or its affiliates.

## Building

```bash
git clone https://github.com/OpenMLTD/UnityStudioLib.git --recursive
cd UnityStudioLib
nuget restore UnityStudioLib.sln
msbuild UnityStudioLib.sln /p:Configuration=Release
```

## License

This repository: MIT

For the license of tracking repository, see [here](tracking/UnityStudio/License.md).

This project also utilizes:

- `SevenZipHelper` by [Peter Bromberg](http://www.nullskull.com/a/768/7zip-lzma-inmemory-compression-with-c.aspx)
- `System.Half` by [landislavlang](https://sourceforge.net/projects/csharp-half/) (in Public Domain)
