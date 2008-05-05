set namespace=80BF90BD866049bc87223A029311D797
set version=1
set root=C:\liveobjects
if not exist %root%\libraries\%namespace% mkdir %root%\libraries\%namespace%
if not exist %root%\libraries\%namespace%\%version% mkdir %root%\libraries\%namespace%\%version%
del /f /s /q %root%\libraries\%namespace%\%version%\*.*
mkdir %root%\libraries\%namespace%\%version%\data
xcopy /y %2VideoMonitor_Proj3.dll %root%\libraries\%namespace%\%version%\data\
xcopy /y %2VideoMonitor_Proj3.pdb %root%\libraries\%namespace%\%version%\data\
xcopy /y %1metadata.xml %root%\libraries\%namespace%\%version%\
REM the following line is used when config for TCP comm
xcopy /e /r /y %1channels\*.* %root%\channels\ 
