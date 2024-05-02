# Uninstaller - a Windows CMD util to list and/or execute uninstallation cmd lines

Requires .Net.

Some browsers may find the .exe suspicious. It really isn't, but can harm your computer if used wrong.

This utility is powerful, do not use the -u switch unless you know what you're doing, `uninstaller.exe -u` will uninstall all software (listed in registry) on your computer until it encounters an uninstallation dialogue that halts the process.

```Uninstaller 2.2 by Niklas Sjöberg 2012

Usage: uninstaller [OPTION]
Uninstall software based on uninstall string in registry

  -l            List mode, no uninstall
  -file=        Write GUID/Title to file (only with -l)
  -u            Execute uninstall string for matches (actually uninstall)
  -r=REGEXP     Search expression, use regexp
  -g=GUID       Search expression, use GUID
  -p=parameter  Replace original uninstall string parameter with this expression
-pre=parameter  Append extra parameter before current parameters
  -onlyx86      Only search x86 applications
  -onlyx64      Only search x64 applications
  -failonerror  Terminate Uninstaller if return code != 0 or 3010
  -nokb         Do not list entries that contains *KBnnnnnn

Usage:
  uninstaller.exe -l -r=Java -f="c:\tmp\list.txt"
  List all titels containing Java, save output to file

  uninstaller.exe -l -r="Java.*Update.[12]"
  List all titels such as Java(TM) 6 Update 2

  uninstaller.exe -l -g={3248F0A8-6813-11D6-A77B-00B0D0160030}
  List uninstall information for a GUID

 uninstaller.exe -l -r="Arduino.*" -u -p="/S"
 Execute uninstall string for all entries starting with Arduino, replace parameters with /S
