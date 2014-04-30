###############################################################################
# Flytrace, online viewer for GPS trackers.
# Copyright (C) 2011-2014 Mikhail Karmazin
# 
# This file is part of Flytrace.
# 
# Flytrace is free software: you can redistribute it and/or modify
# it under the terms of the GNU Affero General Public License as
# published by the Free Software Foundation, either version 3 of the
# License, or (at your option) any later version.
# 
# Flytrace is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU Affero General Public License for more details.
# 
# You should have received a copy of the GNU Affero General Public License
# along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
###############################################################################
# Set the path context to the local, default instance of SQL Server.
param($server,$db,$filename)

$ErrorActionPreference = "Stop"

CD SQLSERVER:
CD \sql\$server

# Create a Scripter object and set the required scripting options.

$scrp = New-Object -TypeName Microsoft.SqlServer.Management.SMO.Scripter -ArgumentList (Get-Item .)
$scrp.Options.ScriptDrops = $false
$scrp.Options.WithDependencies = $false
$scrp.Options.IncludeIfNotExists = $false
$scrp.Options.Indexes = $true
$scrp.Options.DriDefaults = $true
$scrp.Options.DriChecks = $true
$scrp.Options.ExtendedProperties = $true
$scrp.Options.Permissions = $true
$scrp.Options.NoCommandTerminator = $false
$scrp.Options.NoCollation = $true    #so it inherit DB collation everywhere
$scrp.Options.TargetServerVersion = [Microsoft.SqlServer.Management.Smo.SqlServerVersion]::Version90
$scrp.Options.FileName = $filename
$scrp.Options.AppendToFile = $false
$scrp.Options.ToFileOnly = $true

Write-Host Scripting to $filename ...

CD Databases\$db

Write-Host ""
Write-Host Scripting Roles...
foreach ($Item in Get-ChildItem Roles | 	
	where {!$_.IsFixedRole -and $_.Name -ne "public" -and $_.Name -notlike "*aspnet*"})
{
	Write-Host $Item.Name
	$scrp.Script($Item)
	$scrp.Options.AppendToFile = $true  # first call to Script creates file, others append
} 

Write-Host ""
Write-Host Scripting User Defined Functions...
foreach ($Item in Get-ChildItem UserDefinedFunctions | 	
	where {$_.Name -notlike "*aspnet*"})
{
	Write-Host $Item.Name
	$scrp.Script($Item)
	$scrp.Options.AppendToFile = $true  # first call to Script creates file, others append
} 

$scrp.Options.DriForeignKeys = $false;
$scrp.Options.Triggers = $false

Write-Host ""
Write-Host Scripting Tables...
foreach ($Item in Get-ChildItem Tables | 	
	where {$_.Name -notlike "*aspnet*" })
{
	Write-Host $Item.Name
    $scrp.Script($Item)
	$scrp.Options.AppendToFile = $true  # first call to Script creates file, others append
}

$scrp.Options.DriForeignKeys = $true;
$scrp.Options.Triggers = $true

Write-Host ""
Write-Host Scripting Foreign keys "&" triggers...
foreach ($Item in Get-ChildItem Tables | 	
	where {$_.Name -notlike "*aspnet*" })
{
	foreach ($fk in $Item.ForeignKeys )
	{
		Write-Host $fk.Name
		$scrp.Script($fk)
	}

	foreach ($trg in $Item.Triggers )
	{
		Write-Host $trg.Name
		$scrp.Script($trg)
	}
}

Write-Host ""
Write-Host Scripting Views...
foreach ($Item in Get-ChildItem Views | 	
	where {$_.Name -notlike "*aspnet*"})
{
	Write-Host $Item.Name
	$scrp.Script($Item)
	$scrp.Options.AppendToFile = $true  # first call to Script creates file, others append
}

Write-Host ""
Write-Host Scripting Stored Procedures...
foreach ($Item in Get-ChildItem StoredProcedures | 	
	where {$_.Name -notlike "*aspnet*"})
{
	Write-Host $Item.Name
	$scrp.Script($Item)
}

Write-Host ""
Write-Host Done.