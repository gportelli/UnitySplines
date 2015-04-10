rem echo off

IF [%1] == [] (
	set PROJECT_DIR=..
) ELSE (
	set PROJECT_DIR=%1
)

call %PROJECT_DIR%\batch\initPath.bat

rem Copying the DLLs
Copy %PROJECT_DIR%\bin\Debug\SplinesLibrary.dll "%DEST_PATH%"


rem Copying Debug informations
Copy %PROJECT_DIR%\bin\Debug\SplinesLibrary.dll.mdb "%DEST_PATH%"

rem Copying code documentation
Copy %PROJECT_DIR%\bin\Debug\SplinesLibrary.XML "%DEST_PATH%"
