param($dest_folder)

$ErrorActionPreference = "Stop"

Function CopyByRelPath ($rel_path)
{
    Write-Host $rel_path
    
    $src_path = [System.IO.Path]::Combine('..',$rel_path)
    $src_path = $src_path.Replace('/', '\')

    $dest_file_path = [System.IO.Path]::Combine($dest_folder,$rel_path)
    $dest_file_path = $dest_file_path.Replace('/', '\')
    
    $dest_dir_path = [System.IO.Path]::GetDirectoryName($dest_file_path)
    
    if( -not [System.IO.Directory]::Exists($dest_dir_path)) {
        [System.IO.Directory]::CreateDirectory($dest_dir_path)
    }
    
    [System.IO.File]::Copy($src_path,$dest_file_path,$True)
}

Get-Content changed_files.txt | 
where {
    $_ -like "DB Scripts/*" -or 
    $_ -like "Tracker/*"
} |
where {
    $_ -like "*.asax" -or
    $_ -like "*.asmx" -or
    $_ -like "*.aspx" -or
    $_ -like "*.ascx" -or
    $_ -like "*.resx" -or
    $_ -like "*.jpg" -or
    $_ -like "*.png" -or
    $_ -like "*.config" -or
    $_ -like "*.settings" -or
    $_ -like "*.default" -or
    $_ -like "*.htm" -or
    $_ -like "*.js" -or
    $_ -like "*.Master" -or
    $_ -like "*.sitemap" -or
    $_ -like "*.xml" -or
    $_ -like "*/EmailTemplates/*.txt" -or
    $_ -like "*/App_Themes/Default/*" -or
    $_ -like "*.sql"
} | 
where {
    $_ -notlike "screenshots/*" 
} |
Foreach-Object { 
    CopyByRelPath $_
} 

CopyByRelPath 'Tracker\bin\LocationLib.dll'
CopyByRelPath 'Tracker\bin\LocationLib.pdb'
CopyByRelPath 'Tracker\bin\Tracker.dll'
CopyByRelPath 'Tracker\bin\Tracker.pdb'
CopyByRelPath 'Tracker\bin\Service.dll'
CopyByRelPath 'Tracker\bin\Service.pdb'
CopyByRelPath 'Tracker\Web.LocationLib.dll.config'
CopyByRelPath 'Tracker\Web.Service.dll.config'
