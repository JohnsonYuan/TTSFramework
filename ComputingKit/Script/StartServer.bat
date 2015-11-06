@rem
@rem Batch file to start distribute computing server
@rem
@rem 2005-Dec-08 zweng
@rem 

@if not exist %~dp0Settings.bat echo Can not open %~dp0Settings.bat & exit /b 1
call %~dp0Settings.bat & if not errorlevel 0 exit /b %errorlevel%

@set WorkSite=c:\Temp\DC\ProcessServer
@set Source=%~dp0ProcessServer

@mkdir %WorkSite%
call xcopy /S /E /Y %Source% %WorkSite%

call %WorkSite%\ProcessServer.exe -aip %AggregatorIp% -aport %AggregatorPort% -lport %ServerPort%