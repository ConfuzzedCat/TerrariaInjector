@echo off
echo Compiling mod...

if exist  "%WinDir%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
	set csc="%WinDir%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) else (
	echo Microsoft.NET Framework v4.0 not found ...
	timeout /t 5	
	exit
)

%csc% /out:".\modname.dll"  @compileconfig.rsp 

echo Done!
timeout /t 30