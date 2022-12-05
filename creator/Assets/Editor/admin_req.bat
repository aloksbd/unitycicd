@Echo off

echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"

Set _batchFile=%~dp0registry.bat
Set _Args=%*

Set _batchFile=""%_batchFile:"=%""
Set _Args=%_Args:"=""%

echo UAC.ShellExecute "cmd.exe", "/c ""%_batchFile% %_Args%""", "", "runas", 1 >> "%temp%\getadmin.vbs"

"%temp%\getadmin.vbs"
del "%temp%\getadmin.vbs"

@REM pause