@echo off
echo Compilando Game Launcher...
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /target:winexe /out:Launcher.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll Launcher.cs
if %ERRORLEVEL% equ 0 (
    echo Compilacion exitosa: Launcher.exe
) else (
    echo Error al compilar
)
pause
