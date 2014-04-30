rem ---------------------------------------------------------------------------
rem Flytrace, online viewer for GPS trackers.
rem Copyright (C) 2011-2014 Mikhail Karmazin
rem 
rem This file is part of Flytrace.
rem 
rem Flytrace is free software: you can redistribute it and/or modify
rem it under the terms of the GNU Affero General Public License as
rem published by the Free Software Foundation, either version 3 of the
rem License, or (at your option) any later version.
rem 
rem Flytrace is distributed in the hope that it will be useful,
rem but WITHOUT ANY WARRANTY; without even the implied warranty of
rem MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
rem GNU Affero General Public License for more details.
rem 
rem You should have received a copy of the GNU Affero General Public License
rem along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
rem ---------------------------------------------------------------------------

@ECHO OFF

if [%1]==[] goto usage

if [%2]==[] goto usage

if [%3]==[] goto usage


sqlps %~sdp0script_db.ps1 -server %1 -db %2 -filename %3

if ERRORLEVEL 1 (
	echo -
	echo ------------- Problems with generating script, see above.
	goto :eof
)

goto :eof

:usage
echo Usage: %0 server_name db_name output_file_name
echo     --
echo     Examples of server names:
echo        "localhost\default" for default instance on this machine
echo        "localhost\SQLEXPRESS" for SQLEXPRESS instance on this machine
echo        "OtherMachine\InstanceName" for InstanceName instance on this OtherMachine
echo     --
echo     db_name is just a database name
exit /B 1