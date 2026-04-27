[Setup]
AppName=I/O Lockdown
AppVersion=1.1.2
DefaultDirName={pf}\IOLockdown
DefaultGroupName=I/O Lockdown
OutputDir=.
OutputBaseFilename=IOLockdown_Setup_v1.1.2
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Files]
Source: "publish\io-lockdown.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\I/O Lockdown"; Filename: "{app}\io-lockdown.exe"
Name: "{userstartup}\I/O Lockdown UI"; Filename: "{app}\io-lockdown.exe"

[Run]
; Instala o serviço
Filename: "{sys}\sc.exe"; Parameters: "create IOLockdownService binPath= ""{app}\io-lockdown.exe --service"" start= auto displayname= ""I/O Lockdown Security Service"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "description IOLockdownService ""Protege adaptadores de rede e portas USB contra ataques físicos."""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "start IOLockdownService"; Flags: runhidden
; Inicia a interface para o usuário atual
Filename: "{app}\io-lockdown.exe"; Description: "Iniciar I/O Lockdown agora"; Flags: nowait postinstall runasoriginaluser

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop IOLockdownService"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete IOLockdownService"; Flags: runhidden
