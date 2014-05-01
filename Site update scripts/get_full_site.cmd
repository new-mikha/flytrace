@ECHO OFF

echo 
echo Create a folder with all files related to web site. 
echo The project should be compiled first, i.e. all DLLs
echo already created (this script does NOT compile it)
echo 
echo Usage: %0 [dest_folder]
echo     If dest_folder is omitted, then "Update" is used.
echo     Note that dest_folder is not cleaned up. If the folder already 
echo     exists, it's recommended to clean it up manually.

SET dest_folder=%1
if [%1]==[] SET dest_folder=Update

echo Copying to %dest_folder%...

powershell Powershell.exe -executionpolicy remotesigned -File  %~sdp0get_full_site.ps1 -dest_folder "'%dest_folder%'"

pause