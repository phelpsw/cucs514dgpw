set namespace=D1A014C27CB040799A951AD38C641F31
set version=1
set root=C:\liveobjects
if not exist %root%\libraries\%namespace% mkdir %root%\libraries\%namespace%
if not exist %root%\libraries\%namespace%\%version% mkdir %root%\libraries\%namespace%\%version%
del /f /s /q %root%\libraries\%namespace%\%version%\*.*
mkdir %root%\libraries\%namespace%\%version%\data
xcopy /y %2CS514_HW2.dll %root%\libraries\%namespace%\%version%\data\
xcopy /y %2CS514_HW2.pdb %root%\libraries\%namespace%\%version%\data\
xcopy /y %1metadata.xml %root%\libraries\%namespace%\%version%\
xcopy /e /r /y %1channels\*.* %root%\channels\