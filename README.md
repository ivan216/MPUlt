# MPUlt
This program by Andrey Astrelin supports large variety of twisty puzzles in higher dimensional spaces.

http://superliminal.com/andrey/mpu/

# Building
You can clone this repo and build using Microsoft Visual Studio.  The project references DirectX Managed Code DLLs bundled in the `v9.02.2904/` directory, so no additional DirectX SDK installation is needed.

### Troubleshooting
- When you run the project in Visual Studio and see BadImageFormatException and if your platform selector shows "AnyCPU", please select "x86" instead.
- When you run the project in Visual Studio and see a "LoaderLock occurred" error, please uncheck "Break when this exception type is thrown", and click "Continue" to ignore it. If your Visual Studio doesn't have this option, follow https://stackoverflow.com/questions/56642/loader-lock-error to ignore this type of exceptions in a menu.

## TODO
restore support for `{3,3}^2_v2` and `MirrorCube`
