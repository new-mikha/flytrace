param($dest_folder)

$ErrorActionPreference = "Stop"

cd ..

$src_dir = $(get-location).Path + "\Tracker\"

Get-ChildItem -Recurse |
where {
    $_.FullName -like $src_dir + "*"
} |
where {
    $_.FullName -notlike "*log4net.xml" -and
    $_.FullName -notlike "*dev.config" -and
    $_.FullName -notlike "*\obj\*" 
} |
where {
    $_.FullName -like "*.dll" -or
    $_.FullName -like "*.pdb" -or
    $_.FullName -like "*.asax" -or
    $_.FullName -like "*.asmx" -or
    $_.FullName -like "*.aspx" -or
    $_.FullName -like "*.ascx" -or
    $_.FullName -like "*.resx" -or
    $_.FullName -like "*.jpg" -or
    $_.FullName -like "*.png" -or
    $_.FullName -like "*.config" -or
    $_.FullName -like "*.settings" -or
    $_.FullName -like "*.default" -or
    $_.FullName -like "*.htm" -or
    $_.FullName -like "*.js" -or
    $_.FullName -like "*.Master" -or
    $_.FullName -like "*.sitemap" -or
    $_.FullName -like "*.xml" -or
    $_.FullName -like "*\EmailTemplates\*.txt" -or
    $_.FullName -like "*\App_Themes\Default\*"
} | 
Foreach-Object { 
    #Write-Host $_.FullName
    
    $rel_path = $_.FullName.Substring( $src_dir.Length  )

    $dest_file_path = [System.IO.Path]::Combine($dest_folder,$rel_path)
    $dest_dir_path = [System.IO.Path]::GetDirectoryName($dest_file_path)
    
    
    if( -not [System.IO.Directory]::Exists($dest_dir_path)) {
        [System.IO.Directory]::CreateDirectory($dest_dir_path)
    }
    
    [System.IO.File]::Copy($_.FullName, $dest_file_path,$True)
}
