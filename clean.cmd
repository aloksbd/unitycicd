@echo off
for /f %%i in (.\clean.folders) do if exist creator\%%i\* rd /s /q creator\%%i
for /f %%i in (.\clean.folders) do if exist player\%%i\* rd /s /q player\%%i

