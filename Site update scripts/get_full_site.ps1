param($dest_folder)

$ErrorActionPreference = "Stop"

cd ..

$src_dir = $(get-location).Path + "\Tracker\"

Get-ChildItem -Recurse |
where {
    $_.FullName -like $src_dir + "*"
} |
where {
    $_ -notlike "*log4net.xml" -and
    -not $_.FullName.Contains("\obj\" )
} |
where {
    $_ -like "*.dll" -or
    $_ -like "*.pdb" -or
    $_ -like "*.asax" -or
    $_ -like "*.asmx" -or
    $_ -like "*.aspx" -or
    $_ -like "*.ascx" -or
    $_ -like "*.config" -or
    $_ -like "*.css" -or
    $_ -like "*.default" -or
    $_ -like "*.htm" -or
    $_ -like "*.js" -or
    $_ -like "*.Master" -or
    $_ -like "*.sitemap" -or
    $_ -like "*.xml"
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
