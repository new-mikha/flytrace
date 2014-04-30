@ECHO OFF

if [%1]==[] goto usage

SET to_rev=%2
if [%2]==[] SET to_rev=HEAD

SET dest_folder=%3
if [%3]==[] SET dest_folder=Update

SET from_rev=%1

rem echo 1: %from_rev%
rem echo 2: %to_rev%
rem echo 3: %dest_folder%

git diff --name-only %from_rev% %to_rev% --diff-filter=ACMR >changed_files.txt
if ERRORLEVEL 1 (
	goto :cleanup
)

powershell Powershell.exe -executionpolicy remotesigned -File  %~sdp0get-update.ps1 -dest_folder "'%dest_folder%'"

if ERRORLEVEL 1 (
	goto :cleanup
)

goto :cleanup

:usage
echo Create a folder with files updated between two revisions.
echo Usage: %0 from_revision [to_revision] [dest_folder]
echo     If to_revision is omitted, then HEAD is used.
echo     If dest_folder is omitted, then "Update" is used.
echo     Note that dest_folder is not cleaned up. If the folder already 
echo     exists, it's recommended to clean it up manually.
exit /B 1

:cleanup
if EXIST changed_files.txt del changed_files.txt