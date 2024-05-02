# UnInstaller - a Windows CMD util to list and/or execute uninstallation cmd lines

Some browsers may find the .exe suspicious. It really isn't, but can harm your computer if used wrong.
Feel free to inspect source and compile on your own.

This utility is powerful, do not use the -u switch unless you know what you're doing: 
uninstaller.exe -r=".*" -u 
will uninstall all software (listed in registry) on your computer until it encounters an uninstallation dialogue that halts the process.
