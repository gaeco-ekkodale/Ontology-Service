@echo off
REM Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
REM
REM This file is part of the gaeco platform system.
REM
REM Use of this file is governed by the terms of the license
REM in LICENSE.md at the root of this repository.
REM Unauthorized copying, modification, distribution, or use of this file,
REM via any medium, is strictly prohibited except as expressly permitted
REM under that license.

setlocal

set "D=%~dp0"
set "D=%D:~0,-1%"
set "MANIFEST=%D%\manifest.yaml"

:: Parse manifest values (initialize optionals to empty first)
set "README_FILE="
for /f "usebackq tokens=1,* delims=: " %%A in ("%MANIFEST%") do (
    if "%%A"=="name"               set "PKG_NAME=%%B"
    if "%%A"=="dockerComposeFile"  set "COMPOSE_FILE=%%B"
    if "%%A"=="envSchemaFile"     set "ENV_SCHEMA=%%B"
    if "%%A"=="iconFile"           set "ICON_FILE=%%B"
    if "%%A"=="readmeFile"         set "README_FILE=%%B"
)

:: Strip surrounding quotes
set "PKG_NAME=%PKG_NAME:"=%"
set "COMPOSE_FILE=%COMPOSE_FILE:"=%"
set "ENV_SCHEMA=%ENV_SCHEMA:"=%"
set "ICON_FILE=%ICON_FILE:"=%"
if defined README_FILE set "README_FILE=%README_FILE:"=%"

:: Build zip name (spaces to dashes)
set "ZIP_NAME=%PKG_NAME: =-%"

:: Validate required fields and files
set "ERR=0"
if not defined COMPOSE_FILE  (echo ERROR: dockerComposeFile is not defined  & set ERR=1)
if not defined ENV_SCHEMA    (echo ERROR: envScheamaFile is not defined     & set ERR=1)
if not defined ICON_FILE     (echo ERROR: iconFile is not defined           & set ERR=1)
if defined COMPOSE_FILE  if not exist "%D%\%COMPOSE_FILE%"  (echo ERROR: dockerComposeFile: %COMPOSE_FILE% not found  & set ERR=1)
if defined ENV_SCHEMA    if not exist "%D%\%ENV_SCHEMA%"    (echo ERROR: envScheamaFile: %ENV_SCHEMA% not found       & set ERR=1)
if defined ICON_FILE     if not exist "%D%\%ICON_FILE%"     (echo ERROR: iconFile: %ICON_FILE% not found             & set ERR=1)
if %ERR%==1 goto :error

:: Optional readme (only optional if not defined; if defined, file must exist)
if defined README_FILE (
    if not exist "%D%\%README_FILE%" (echo ERROR: readmeFile: %README_FILE% not found & goto :error)
)

:: Create zip (PowerShell required for zip creation)
set "ZIP=%D%\%ZIP_NAME%.zip"
if exist "%ZIP%" del "%ZIP%"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Push-Location '%D%';" ^
  "$files = @('manifest.yaml','%COMPOSE_FILE%','%ENV_SCHEMA%','%ICON_FILE%');" ^
  "$r='%README_FILE%'; if($r){$files+=$r};" ^
  "Compress-Archive $files '%ZIP%';" ^
  "Pop-Location"
if %ERRORLEVEL%==0 (echo Created: %ZIP%) else (echo ERROR: Failed to create zip! & goto :error)
goto :end

:error
pause

:end

endlocal
