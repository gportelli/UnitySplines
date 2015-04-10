rem echo off

IF [%1] == [] (
	set PROJECT_DIR=..
) ELSE (
	set PROJECT_DIR=%1
)

call %PROJECT_DIR%\batch\initPath.bat

if not exist "%DEST_PATH%\Editor" mkdir "%DEST_PATH%\Editor"

rem Copying the DLLs
Copy %PROJECT_DIR%\bin\Debug\SplinesLibraryEditor.dll "%DEST_PATH%\Editor"


rem Copying Debug informations
Copy %PROJECT_DIR%\bin\Debug\SplinesLibraryEditor.dll.mdb "%DEST_PATH%\Editor"

rem Copying code documentation
Copy %PROJECT_DIR%\bin\Debug\SplinesLibraryEditor.XML "%DEST_PATH%\Editor"
