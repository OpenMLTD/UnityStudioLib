# UnityStudioLib

**Now obsolete.** Use the [core library](https://github.com/Perfare/AssetStudio/tree/master/AssetStudio) instead.
If you feel inconvenient using that, please send a PR to that repo to urge a better design.
Alternatively, you can write a bunch of glue code to make your life easier, like in MLTDTools. :D

------

Packing [Unity Studio](https://github.com/Perfare/UnityStudio) into a class library.
Only essential parts are implemented.

Requires .NET Framework 4.5.

> None of the repo, the tool, nor the repo owner is affiliated with, or sponsored or authorized by
> Unity Technologies or its affiliates.

Current features:

- `MonoBehaviour` deserialization (please use fields instead of properties for best performance)
- Reading:
  - Mesh
  - Avatar
  - Text asset

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
