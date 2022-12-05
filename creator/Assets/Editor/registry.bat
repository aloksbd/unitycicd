@echo off

set registry_key= %1
set url_protocol= %2
set path= %~3

set registry_key=%registry_key: =%
set url_protocol=%url_protocol: =%

echo %registry_key%
echo %url_protocol%
echo %path%

%SystemRoot%\System32\reg.exe add HKCR\%registry_key% /t REG_SZ /d %url_protocol% /f
%SystemRoot%\System32\reg.exe add HKCR\%registry_key% /v "URL Protocol" /t REG_SZ /d "" /f
%SystemRoot%\System32\reg.exe add HKCR\%registry_key%\shell /f
%SystemRoot%\System32\reg.exe add HKCR\%registry_key%\shell\open /f
%SystemRoot%\System32\reg.exe add HKCR\%registry_key%\shell\open\command /t REG_SZ /d "\"%path%\" \"%1\"" /f

@REM pause