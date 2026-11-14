function Get-BuiltOutputFolder {
    <#
    .SYNOPSIS
    The folder the latest Debug build of a project wrote its output to, or nothing when the project is not built

    .DESCRIPTION
    A project's output is in bin\Debug\<target framework>, and the target framework is not named here: the folder is the
    one whose copy of the project's assembly was written last. So the commands that run or copy what C-Build built keep
    working when the projects change target framework, and the folder an earlier target framework left behind is passed
    over.

    .PARAMETER ProjectFile
    The project's file.
    #>
    param([Parameter(Mandatory)][string]$ProjectFile)

    $project = Get-Item -LiteralPath $ProjectFile
    $outputFolders = Get-ChildItem -LiteralPath (Join-Path $project.DirectoryName 'bin\Debug') -Directory -ErrorAction SilentlyContinue
    $assemblyAsEachHasIt = $outputFolders | ForEach-Object { Get-Item -LiteralPath (Join-Path $_.FullName "$($project.BaseName).dll") -ErrorAction SilentlyContinue }
    $assemblyAsEachHasIt | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1 | ForEach-Object DirectoryName
}
