@rem
@rem Batch file to start distribute computing aggregator
@rem
@rem 2005-Dec-08 zweng
@rem 

@if not exist %~dp0Settings.bat echo Can not open %~dp0Settings.bat & exit /b 1
call %~dp0Settings.bat & if not errorlevel 0 exit /b %errorlevel%

@set WorkSite=c:\Temp\DC\Aggregator
@set Source=%~dp0Aggregator

@mkdir %WorkSite%
call xcopy /S /E /Y %Source% %WorkSite%

call %WorkSite%\Aggregator.exe -lport %AggregatorPort%