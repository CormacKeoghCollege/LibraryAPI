@echo off
setlocal enabledelayedexpansion

:: Set the project folder path (change this to your actual project folder path)
set "PROJECT_FOLDER=C:\Users\cokeogh\Documents\LibraryAPI2"

:: Set the output file name
set "OUTPUT_FILE=%PROJECT_FOLDER%\consolidated_files.txt"

:: Clear the output file if it exists
if exist "%OUTPUT_FILE%" del "%OUTPUT_FILE%"

:: Create separator lines
set "SEPARATOR80================================================================================"
set "SEPARATOR80=%SEPARATOR80:~0,80%"

echo Starting file consolidation...
echo Output file: %OUTPUT_FILE%
echo Processing only: root, Controllers, Data, DTOs, Migrations, Models, Properties, and Services folders
echo.

:: Define the folders to process (including root)
set "FOLDERS_TO_PROCESS=. Controllers Data DTOs Migrations Models Properties Services"

:: Process files in each specified folder
for %%d in (%FOLDERS_TO_PROCESS%) do (
    echo.
    echo Processing folder: %%d
    
    if "%%d"=="." (
        set "CURRENT_FOLDER=%PROJECT_FOLDER%"
        echo Looking in root directory...
    ) else (
        set "CURRENT_FOLDER=%PROJECT_FOLDER%\%%d"
        echo Looking in: !CURRENT_FOLDER!
    )
    
    :: Check if folder exists (skip root check)
    if "%%d"=="." (
        set "FOLDER_EXISTS=1"
    ) else (
        if exist "!CURRENT_FOLDER!" (
            set "FOLDER_EXISTS=1"
        ) else (
            set "FOLDER_EXISTS=0"
            echo Warning: Folder !CURRENT_FOLDER! does not exist
        )
    )
    
    :: Process files in current folder only (not recursive)
    if "!FOLDER_EXISTS!"=="1" (
        for %%f in ("!CURRENT_FOLDER!\*.txt" "!CURRENT_FOLDER!\*.cs" "!CURRENT_FOLDER!\*.json" "!CURRENT_FOLDER!\*.sql" "!CURRENT_FOLDER!\*.html" "!CURRENT_FOLDER!\*.sh" "!CURRENT_FOLDER!\*.yaml" "!CURRENT_FOLDER!\*.yml" "!CURRENT_FOLDER!\*.xml" "!CURRENT_FOLDER!\*.config" "!CURRENT_FOLDER!\*.md" "!CURRENT_FOLDER!\dockerfile") do (
            :: Skip the output file itself and check if file actually exists
            if exist "%%f" (
                if /i not "%%f"=="%OUTPUT_FILE%" (
                    echo   Processing: %%f
                    
                    :: Write separator line
                    echo %SEPARATOR80% >> "%OUTPUT_FILE%"
                    
                    :: Write file path and name
                    echo %%f >> "%OUTPUT_FILE%"
                    
                    :: Write second separator line
                    echo %SEPARATOR80% >> "%OUTPUT_FILE%"
                    
                    :: Write file contents
                    type "%%f" >> "%OUTPUT_FILE%" 2>nul || (
                        echo [FILE CANNOT BE READ] >> "%OUTPUT_FILE%"
                    )
                    
                    :: Add blank lines after content
                    echo. >> "%OUTPUT_FILE%"
                    echo. >> "%OUTPUT_FILE%"
                )
            )
        )
    )
)

echo.
echo ========================================
echo Consolidation complete!
echo Output saved to: %OUTPUT_FILE%
echo ========================================
pause




