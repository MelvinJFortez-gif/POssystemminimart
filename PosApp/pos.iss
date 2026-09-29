; Inno Setup script (https://jrsoftware.org/isinfo.php). Run "build.bat" first.
[Setup]
AppName=POS System
AppVersion=1.0
DefaultDirName={autopf}\PosApp
DefaultGroupName=POS System
OutputBaseFilename=PosSetup
Compression=lzma
[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: recursesubdirs
Source: "database.sql"; DestDir: "{app}"
[Icons]
Name: "{group}\POS System"; Filename: "{app}\PosApp.exe"
Name: "{commondesktop}\POS System"; Filename: "{app}\PosApp.exe"
