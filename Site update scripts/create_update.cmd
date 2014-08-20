@ECHO OFF

echo Create a folder with files updated between two revisions.

if [%1]==[-help] goto :usage
if [%1]==[/help] goto :usage
if [%1]==[\help] goto :usage
if [%1]==[-?] goto :usage
if [%1]==[/?] goto :usage
if [%1]==[\?] goto :usage

SET from_rev=
SET to_rev=
SET dest_folder=


if [%1]==[] (
	echo Inclusive 'to' revision is HEAD, dest folder is ./Update
    SET /P from_rev=Enter NON-inclusive 'from' revision, could be a tag name:
	
	SET to_rev=HEAD
	SET dest_folder=Update
) else (
    SET from_rev=%1

    SET to_rev=%2
    if [%2]==[] SET to_rev=HEAD

    SET dest_folder=%3
    if [%3]==[] SET dest_folder=Update
)

if [%from_rev%]==[] goto :usage

echo From: %from_rev%
echo To: %to_rev%
echo Folder: %dest_folder%

echo git diff --name-only %from_rev% %to_rev% --diff-filter=ACMR 
git diff --name-only %from_rev% %to_rev% --diff-filter=ACMR >changed_files.txt
if ERRORLEVEL 1 (
	echo.
    goto :cleanup
)

powershell Powershell.exe -executionpolicy remotesigned -File  %~sdp0get-update.ps1 -dest_folder "'%dest_folder%'"

if ERRORLEVEL 1 (
	echo.
    goto :cleanup
)

goto :cleanup

:usage
echo Usage: 
echo    Either run it without parameters, e.g. right from the Windows Explorer 
echo    by the double click:
echo    %0
echo      If this bach file is running without parameters, it asks for
echo      'from' revision, and sets 'to' revision and destination 
echo      folder to defaults (see below)
echo    - OR run it from the command line: -
echo    %0 from_revision [to_revision] [dest_folder]
echo      If to_revision is omitted, then HEAD is used.
echo      If dest_folder is omitted, then 'Update' is used.
echo      Note that dest_folder is not cleaned up. If the folder already 
echo      exists, it's recommended to clean it up manually.
pause
exit /B 1

:cleanup
if EXIST changed_files.txt del changed_files.txt

if [%1]==[] pause