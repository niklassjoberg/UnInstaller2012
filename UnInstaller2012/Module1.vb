Imports Microsoft.Win32
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Text

Module Module1
    Const uGUID = 0
    Const uEXE = 1

    Public Class uninst
        Public Property strDisplayName As String
        Public Property strUnInstall As String
        Public Property strArgs As String
        Public Property intType As Integer
        Public Property strVersion As String
        Public Property strComment As String
    End Class
    Public prglist As New List(Of uninst) 'Working list all entries
    Public actionlist As New List(Of uninst) 'Final list, only matched entries
    Dim test As uninst

    Function Main(ByVal sArgs() As String) As Integer
        Dim i As Integer = 0
        Dim optList As Boolean = False
        Dim optUninstall As Boolean = False
        Dim optOnlyx64 As Boolean = False
        Dim optOnlyx86 As Boolean = False
        Dim optFailOnError As Boolean = False
        Dim optNoKB As Boolean = False

        Dim strRegExp As String = "" 'regexp to match
        Dim strSID As String = "" 'GUID to uninstall
        Dim strParam As String = "" 'param to to replace uninstall string with
        Dim strPreParam As String = "" 'param to prepend to uninstallstring
        Dim strFile As String = "" 'Optional filename to pipe output to

        If sArgs.Length < 1 Then
            Console.WriteLine("Uninstaller 2.2 by Niklas Sjöberg 2012")
            Call showhelp()
            End
        Else
            While i < sArgs.Length
                If InStr(sArgs(i), "-onlyx86") Then optOnlyx86 = True 'List only x86 entries
                If InStr(sArgs(i), "-onlyx64") Then optOnlyx64 = True 'List only x64 entries
                If InStr(sArgs(i), "-failonerror") Then optFailOnError = True 'Terminate if return code is not 0 or 3010
                If InStr(sArgs(i), "-nokb") Then optNoKB = True 'Terminate if return code is not 0 or 3010
                If InStr(sArgs(i), "-p=") Then strParam = Replace(sArgs(i), "-p=", "") ' Replace .exe params with this param instead
                If InStr(sArgs(i), "-pre=") Then strPreParam = Replace(sArgs(i), "-pre=", "") ' prepend this option to original uninstall option
                If InStr(sArgs(i), "-l") Then optList = True ' Print matched entries
                If InStr(sArgs(i), "-u") Then optUninstall = True 'Uninstall matched entries
                'If InStr(sArgs(i), "-d") Then strDebug = True
                If InStr(sArgs(i), "-r=") Then strRegExp = Replace(sArgs(i), "-r=", "") ' Regexp to match against
                If InStr(sArgs(i), "-g=") Then strSID = Replace(sArgs(i), "-g=", "").ToUpper ' GUID to match against
                If InStr(sArgs(i), "-file=") Then strFile = Replace(sArgs(i), "-file=", "").ToUpper ' GUID to match against
                '-pipe till fil
                i = i + 1
            End While
        End If


        'Detect if running on XP
        Dim osInfo As System.OperatingSystem = System.Environment.OSVersion
        Dim vs As Version = osInfo.Version
        'Console.WriteLine(vs.Major)
        If vs.Major < 6 Then
            optOnlyx86 = True
            'Console.WriteLine("Windows XP")
        End If

        If optList = False And optUninstall = False Then
            Console.WriteLine("Uninstaller 2.2 by Niklas Sjöberg 2012")
            Console.WriteLine("Specify at least one command: list, uninstall.")
            End
        End If

        If strRegExp <> "" And strSID <> "" Then
            Console.WriteLine("Uninstaller 2.2 by Niklas Sjöberg 2012")
            Console.WriteLine("Regexp and GUID can not be combined.")
            End
        End If

        If optOnlyx64 = True And optOnlyx86 = True Then
            Console.WriteLine("Uninstaller 2.2 by Niklas Sjöberg 2012")
            Console.WriteLine("-onlyx64 and -onlyx86 can not be combined.")
            End
        End If

        If optOnlyx86 = False Then populate_x64()
        If optOnlyx64 = False Then populate_x86()

        If optNoKB = True Then 'Delete all entries matching KBnnnnnn
            For i = prglist.Count - 1 To 0 Step -1
                If Regex.IsMatch(prglist(i).strDisplayName, ".*KB\d\d\d\d\d\d") Then
                    prglist.RemoveAt(i)
                End If

            Next i

        End If
        determine_uninstall_string(strParam, strPreParam) 'Find out if it is msiexec or custom setup. Mask out GUID if msiexec and params from actual .exe

        'If no regexp and no guid everything goes into actionlist
        If strRegExp = "" And strSID = "" Then actionlist.AddRange(prglist)

        If strRegExp <> "" Then 'Loop all software looking for regexp math
            For Each entry In prglist
                If Regex.IsMatch(entry.strDisplayName, strRegExp) Then
                    actionlist.Add(entry)
                End If
            Next
        End If
        If strSID <> "" Then 'loop through all software looking for specific GUID
            For Each entry In prglist
                If entry.strUnInstall.ToUpper = strSID Then
                    actionlist.Add(entry)
                End If
            Next
        End If

        For Each entry In actionlist
            If optList = True Then
                Console.WriteLine("Title  : " & entry.strDisplayName)
                Console.WriteLine("Version: " & entry.strVersion)
                If entry.intType = uGUID Then
                    Console.WriteLine("   GUID: " & entry.strUnInstall)
                Else
                    Console.WriteLine("   EXE: " & entry.strUnInstall)
                    Console.WriteLine("PARAMS: " & entry.strArgs)
                End If
                Console.WriteLine("======================================================================")
                If strFile <> "" Then 'Same output should go to file
                    Dim sb As New StringBuilder()
                    Using sr As StreamWriter = File.AppendText(strFile)
                        sb.AppendLine("Title  : " & entry.strDisplayName)
                        sb.AppendLine("Version: " & entry.strVersion)
                        If entry.intType = uGUID Then
                            sb.Append("   GUID: " & entry.strUnInstall & vbCrLf)
                        Else
                            sb.AppendLine("   EXE: " & entry.strUnInstall)
                            sb.AppendLine("PARAMS: " & entry.strArgs & vbCrLf)
                        End If
                        sr.Write(sb.ToString())
                    End Using
                End If
            End If

            If optUninstall = True Then
                Dim retcode As Integer = 0
                retcode = UnInstall(entry)
                If (retcode <> 0 Or retcode <> 3010) And optFailOnError = True Then
                    Console.WriteLine("Returncode " & retcode.ToString & " with -failonerror. Exiting.")
                    Return retcode
                End If
            End If
            'Console.WriteLine("")
        Next

        If optList = True Then
            Console.WriteLine(actionlist.Count & " entries.")
        End If

        Return 0

    End Function
    Sub populate_x64()
        Dim localKey As RegistryKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64)
        localKey = localKey.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")

        If localKey IsNot Nothing Then
            For Each subKeyName As String In localKey.GetSubKeyNames()
                Dim subkey As RegistryKey = localKey.OpenSubKey(subKeyName)
                'Console.WriteLine(subkey.Name)
                Dim prg As New uninst
                prg.strDisplayName = subkey.GetValue("DisplayName")
                prg.strUnInstall = subkey.GetValue("UninstallString")
                prg.strVersion = subkey.GetValue("DisplayVersion")
                prg.strComment = subkey.GetValue("Comment")

                If prg.strDisplayName IsNot Nothing And prg.strUnInstall IsNot Nothing Then
                    prglist.Add(prg)
                End If
            Next
        End If

    End Sub
    Sub populate_x86()
        Dim localKey32 As RegistryKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32)
        localKey32 = localKey32.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
        If localKey32 IsNot Nothing Then
            For Each subKeyName As String In localKey32.GetSubKeyNames()
                Dim subkey As RegistryKey = localKey32.OpenSubKey(subKeyName)
                'Console.WriteLine(subkey.Name)
                Dim prg As New uninst
                prg.strDisplayName = subkey.GetValue("DisplayName")
                prg.strUnInstall = subkey.GetValue("UninstallString")
                prg.strVersion = subkey.GetValue("DisplayVersion")
                prg.strComment = subkey.GetValue("Comment")

                If prg.strDisplayName IsNot Nothing And prg.strUnInstall IsNot Nothing Then
                    prglist.Add(prg)
                End If
            Next
        End If

    End Sub
    Sub determine_uninstall_string(newparam As String, preparam As String)
        'If a new parameter <>"" is supplied we replace the original

        'Set type of uninstallcommand, and cleanup the GUID which typically reads "MsiExec.exe /X {01078B88-2981-4F75-96B0-8B22E2D2DE03}" to only contain GUID string
        Dim cmd As String = ""
        For Each entry In prglist
            cmd = entry.strUnInstall

            If Regex.IsMatch(cmd, "(\{.*\})") = True Then
                entry.intType = uGUID
                entry.strUnInstall = Regex.Match(cmd, "(\{.*?\})").Groups(1).ToString()
            Else
                Dim param As String
                Dim fulluninst As String = Regex.Match(entry.strUnInstall.ToLower, "(.*?\.(exe|com|dll))").Groups(1).ToString() 'Get path up to first command, .exe, .com etc
                If newparam <> "" Then
                    param = newparam
                Else
                    param = Replace(entry.strUnInstall.ToLower, fulluninst, "") 'all but path and exe
                End If
                If preparam <> "" Then 'We are asked to prepend one or more parameters
                    param = preparam & param
                End If

                entry.strUnInstall = Replace(fulluninst, """", "")
                entry.strArgs = Trim(Replace(param, """", ""))

                'Debug.WriteLine("exe found")
                entry.intType = uEXE
            End If

        Next
    End Sub
    Function UnInstall(software As uninst) As Integer
        Dim myPro As New Process()
        If software.intType = uGUID Then
            myPro.StartInfo.FileName = "msiexec.exe"
            myPro.StartInfo.Arguments = "/qn /norestart /x " & software.strUnInstall & " REBOOT=ReallySuppress"
        End If
        If software.intType = uEXE Then
            myPro.StartInfo.FileName = software.strUnInstall
            myPro.StartInfo.Arguments = software.strArgs
        End If
        Console.WriteLine("   Uninstalling: " & software.strDisplayName)
        Console.WriteLine(software.strUnInstall & " " & software.strArgs)
        Try
            myPro.StartInfo.UseShellExecute = True
            myPro.StartInfo.CreateNoWindow = True
            myPro.Start()
            myPro.WaitForExit()
            UnInstall = myPro.ExitCode

        Catch ex As Exception
            Console.WriteLine("   Error while uinstalling: " & ex.Message)
            UnInstall = 1
        End Try

        If UnInstall = 0 Then
            Console.WriteLine("  [ OK ]")
        Else
            Console.WriteLine("  [ ERROR : " & UnInstall & " ]")
        End If

        If UnInstall = 3010 Then Console.WriteLine("Restart is required to complete the uninstall. ")

    End Function
    Sub showhelp()
        Console.WriteLine("")
        Console.WriteLine("Usage: uninstaller [OPTION]")
        Console.WriteLine("Uninstall software based on uninstall string in registry")
        Console.WriteLine("")
        Console.WriteLine("  -l            List mode, no uninstall")
        Console.WriteLine("  -file=        Write GUID/Title to file (only with -l)")
        Console.WriteLine("  -u            Execute uninstall string for matches (actually uninstall)")
        Console.WriteLine("  -r=REGEXP     Search expression, use regexp")
        Console.WriteLine("  -g=GUID       Search expression, use GUID")
        Console.WriteLine("  -p=parameter  Replace original uninstall string parameter with this expression")
        Console.WriteLine("-pre=parameter  Append extra parameter before current parameters")
        Console.WriteLine("  -onlyx86      Only search x86 applications")
        Console.WriteLine("  -onlyx64      Only search x64 applications")
        Console.WriteLine("  -failonerror  Terminate Uninstaller if return code != 0 or 3010")
        Console.WriteLine("  -nokb         Do not list entries that contains *KBnnnnnn")

        Console.WriteLine("")
        Console.WriteLine("Usage:")
        Console.WriteLine("  uninstaller.exe -l -r=Java -f=""c:\tmp\list.txt""")
        Console.WriteLine("  List all titels containing Java, save output to file")
        Console.WriteLine("")
        Console.WriteLine("  uninstaller.exe -l -r=""Java.*Update.[12]""")
        Console.WriteLine("  List all titels such as Java(TM) 6 Update 2")
        Console.WriteLine("")
        Console.WriteLine("  uninstaller.exe -l -g={3248F0A8-6813-11D6-A77B-00B0D0160030}")
        Console.WriteLine("  List uninstall information for a GUID")
        Console.WriteLine("")
        Console.WriteLine(" uninstaller.exe -l -r=""Arduino.*"" -u -p=""/S""")
        Console.WriteLine(" Execute uninstall string for all entries starting with Arduino, replace parameters with /S")

    End Sub
    Sub WriteTextToFile(fname As String, text As String)

    End Sub
End Module
