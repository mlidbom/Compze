# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Compare-PublicApi {
    <#
    .SYNOPSIS
    Lists the public API changes between two commits, project by project: every public and protected type and member added, removed or changed

    .DESCRIPTION
    What a branch changes in what code outside the repository can compile against: for a review of a branch's public
    API, and for a release's changelog.

    It builds the changed projects on both sides, has DocFX's metadata step (the one that generates the website's API
    pages) describe each one's public API as a declaration per type and member, and compares the declarations. A side
    that is a commit rather than the working tree is checked out in a temporary worktree, which is removed afterwards.

    A type or member whose declaration changed, such as a constraint, a base type or a modifier, is listed as changed. A
    method whose parameter types changed is a different method, so it's listed as one removed and one added. Doc
    comments are not compared.

    DocFX doesn't see everything, and the report says what it misses: it skips the members of C# 14 extension blocks, so
    the report names the changed files that hold such blocks, to compare with git diff. It doesn't list the constructor
    C# gives a class that declares none, so the command adds it, marked "(implicit)".

    .PARAMETER Base
    What to compare against. The merge-base of it and Head is used, as git diff Base...Head does. Defaults to main.

    .PARAMETER Head
    The commit to compare. Defaults to the working tree, uncommitted changes included.

    .PARAMETER Projects
    The projects to compare, by name, wildcards allowed. Defaults to the Compze projects under src with changed files.

    .PARAMETER OutFile
    Where to write the report, as markdown. Defaults to writing it to the output.

    .EXAMPLE
    C-Compare-PublicApi

    .EXAMPLE
    C-Compare-PublicApi -Base main -Head my-branch -OutFile $env:TEMP\public-api-changes.md
    #>
    [CmdletBinding()]
    param(
        [string]$Base = "main",
        [string]$Head,
        [string[]]$Projects,
        [string]$OutFile
    )

    $ErrorActionPreference = 'Stop'

    # The helpers come first: PowerShell knows a function only once its definition has run.

    function Invoke-Git {
        $output = & git -C $script:CompzeRoot @args
        if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed with exit code $LASTEXITCODE" }
        $output
    }

    function ChangedPaths([string]$mergeBase, [string[]]$pathspecs) {
        if ($Head) { Invoke-Git diff --name-only $mergeBase $Head '--' @pathspecs }
        else { @(Invoke-Git diff --name-only $mergeBase '--' @pathspecs) + @(Invoke-Git ls-files --others --exclude-standard '--' @pathspecs) }
    }

    function ChangedProjects([string]$mergeBase) {
        ChangedPaths $mergeBase 'src' | ForEach-Object { ($_ -split '/')[1] } | Where-Object { $_ -like 'Compze.*' } | Sort-Object -Unique
    }

    # DocFX skips the members of C# 14 extension blocks: its pull request 10943 made it skip them rather than fail. So
    # the report names the changed files that hold extension blocks, on either side, for their members to be compared
    # from the source.
    function ChangedFilesWithExtensionBlocks([string]$mergeBase, [string[]]$projectNames) {
        ChangedPaths $mergeBase @($projectNames | ForEach-Object { "src/$_" }) | Where-Object { $_ -like '*.cs' } | Sort-Object -Unique |
            Where-Object { (HasExtensionBlock (SourceAt $mergeBase $_)) -or (HasExtensionBlock (SourceAt $Head $_)) }
    }

    function SourceAt([string]$commit, [string]$path) {
        if (-not $commit) {
            $file = Join-Path $script:CompzeRoot $path
            if (Test-Path -LiteralPath $file) { return Get-Content -Raw -LiteralPath $file }
            return ''
        }
        & git -C $script:CompzeRoot cat-file -e "$($commit):$path" 2>$null
        if ($LASTEXITCODE -ne 0) { return '' }
        (Invoke-Git show "$($commit):$path") -join "`n"
    }

    function HasExtensionBlock([string]$source) { $source -match '(?m)^\s*extension\s*[<(]' }

    function ProjectsToCompare([string]$mergeBase) {
        if (-not $Projects) { return @(ChangedProjects $mergeBase) }
        $headProjects = if ($Head) { @(Invoke-Git ls-tree --name-only $Head src/) }
                        else { @(Get-ChildItem (Join-Path $script:CompzeSrcRoot '*') -Directory | ForEach-Object { "src/$($_.Name)" }) }
        $all = @(Invoke-Git ls-tree --name-only $mergeBase src/) + $headProjects
        $all | ForEach-Object { ($_ -split '/')[1] } | Sort-Object -Unique |
            Where-Object { $name = $_; $Projects | Where-Object { $name -like $_ } }
    }

    function CheckOut([string]$commit, [string]$folder) {
        Invoke-Git worktree add --detach --quiet $folder $commit | Out-Null
        $folder
    }

    function Remove-CheckOut([string]$folder) {
        if (Test-Path $folder) { & git -C $script:CompzeRoot worktree remove --force $folder 2>$null | Out-Null }
    }

    # A declaration per public and protected type and member of the side's projects, keyed by DocFX's uid. A project that doesn't
    # exist on the side, such as one the branch adds, has none.
    function PublicApiOf([string]$root, [string[]]$projectNames, [string]$workFolder) {
        $existing = @($projectNames | Where-Object { Test-Path (Join-Path $root "src/$_/$_.csproj") })
        if ($existing.Count -eq 0) { return @{} }

        foreach ($project in $existing) {
            $output = & dotnet build -c Debug -nologo -v q (Join-Path $root "src/$project/$project.csproj") 2>&1
            if ($LASTEXITCODE -ne 0) { throw "Building $project in $root failed:`n$($output -join "`n")" }
        }

        $api = ItemsInFolder (DocfxMetadata $root $existing (Join-Path $workFolder 'public') $false)
        $everything = ItemsInFolder (DocfxMetadata $root $existing (Join-Path $workFolder 'everything') $true)
        AddImplicitConstructors $api $everything
        $api
    }

    # The folder of DocFX's metadata files for the projects. With the private members, it has the internal and private
    # ones too, constructors included.
    function DocfxMetadata([string]$root, [string[]]$projects, [string]$folder, [bool]$withPrivateMembers) {
        New-Item -ItemType Directory -Force $folder | Out-Null
        $docfxJson = @{
            metadata = @(@{
                src                   = @(@{ src = (Join-Path $root 'src'); files = @($projects | ForEach-Object { "$_/$_.csproj" }) })
                dest                  = 'api'
                outputFormat          = 'mref'
                includePrivateMembers = $withPrivateMembers
            })
        }
        $docfxJsonPath = Join-Path $folder 'docfx.json'
        $docfxJson | ConvertTo-Json -Depth 6 | Set-Content -Encoding utf8 $docfxJsonPath
        $output = & docfx metadata $docfxJsonPath 2>&1
        if ($LASTEXITCODE -ne 0) { throw "docfx metadata failed for $root`:`n$($output -join "`n")" }
        Join-Path $folder 'api'
    }

    function ItemsInFolder([string]$folder) {
        $items = @{}
        Get-ChildItem $folder -Filter '*.yml' | Where-Object Name -ne 'toc.yml' |
            ForEach-Object { foreach ($item in (ItemsOf $_.FullName)) { $items[$item.Uid] = $item } }
        $items
    }

    # DocFX lists only the constructors a class declares, and only the public and protected ones unless asked for the
    # private members too. A public class that declares none, of any accessibility, has the one C# gives it: public, or
    # protected in an abstract class.
    function AddImplicitConstructors([hashtable]$api, [hashtable]$everything) {
        $classesWithConstructors = @{}
        $everything.Values | Where-Object Kind -eq 'Constructor' | ForEach-Object { $classesWithConstructors[$_.Parent] = $true }
        $classesWithout = @($api.Values | Where-Object { $_.Kind -eq 'Class' -and (WithoutAttributes $_.Declaration) -notmatch '\bstatic\b[^:]*\bclass\b' -and -not $classesWithConstructors.ContainsKey($_.Uid) })
        foreach ($class in $classesWithout) {
            $accessibility = if ((WithoutAttributes $class.Declaration) -match '\babstract\b[^:]*\bclass\b') { 'protected' } else { 'public' }
            $simpleName = (($class.FullName -replace '<[^<>]*>$', '') -split '\.')[-1]
            $uid = "$($class.Uid).#ctor"
            $api[$uid] = [pscustomobject]@{ Uid = $uid; Kind = 'Constructor'; Parent = $class.Uid; FullName = ''; Assembly = $class.Assembly; Declaration = "$accessibility $simpleName() (implicit)" }
        }
    }

    # A declaration without the attributes in front of it, such as [Obsolete("...")].
    function WithoutAttributes([string]$declaration) { $declaration -replace '^(\[(?:[^\]"]|"(?:[^"\\]|\\.)*")*\]\s*)+', '' }

    # The types and members a DocFX metadata file describes. It reads the few fields it needs line by line, which keeps
    # the command free of a YAML module.
    function ItemsOf([string]$ymlFile) {
        $lines = Get-Content -LiteralPath $ymlFile

        # A line of a declaration that goes on over several lines: indented under it, or blank between such lines, as
        # between an attribute and what it's on.
        function IsContinuation([int]$lineIndex) {
            if ($lineIndex -ge $lines.Count) { return $false }
            if ($lines[$lineIndex] -match '^      ') { return $true }
            $lines[$lineIndex] -eq '' -and $lineIndex + 1 -lt $lines.Count -and $lines[$lineIndex + 1] -match '^      '
        }

        $items = [System.Collections.Generic.List[object]]::new()
        $item = $null
        $inAssemblies = $false
        for ($index = 0; $index -lt $lines.Count; $index++) {
            $line = $lines[$index]
            if ($line -eq 'references:') { break }
            if ($line -match '^- uid: (.+)$') {
                $item = [pscustomobject]@{ Uid = (Unquote $Matches[1]); Kind = ''; Parent = ''; FullName = ''; Assembly = ''; Declaration = '' }
                $items.Add($item)
                $inAssemblies = $false
                continue
            }
            if (-not $item) { continue }
            if ($inAssemblies -and $line -match '^  - (.+)$') { if (-not $item.Assembly) { $item.Assembly = $Matches[1] }; continue }
            $inAssemblies = $line -eq '  assemblies:'
            if ($line -match '^  type: (.+)$') { $item.Kind = $Matches[1] }
            elseif ($line -match '^  parent: (.+)$') { $item.Parent = Unquote $Matches[1] }
            elseif ($line -match '^  fullName: (.+)$') { $item.FullName = Unquote $Matches[1] }
            elseif ($line -match '^    content: (.*)$' -and $lines[$index - 1] -eq '  syntax:') {
                $value = $Matches[1]
                $continuation = [System.Collections.Generic.List[string]]::new()
                while (IsContinuation ($index + 1)) {
                    $index++
                    if ($lines[$index]) { $continuation.Add($lines[$index].Trim()) }
                }
                if ($value -match '^[|>][-+]?$') { $value = $continuation -join ' ' }
                elseif ($continuation.Count -gt 0) { $value = (@($value) + $continuation) -join ' ' }
                $item.Declaration = (Unquote $value) -replace '\s+', ' '
            }
        }
        $items | Where-Object { $_.Kind -ne 'Namespace' }
    }

    function Unquote([string]$value) {
        if ($value -match "^'(.*)'$") { return $Matches[1] -replace "''", "'" }
        if ($value -match '^"(.*)"$') { return $Matches[1] -replace '\\"', '"' -replace '\\n', ' ' -replace '\\\\', '\' }
        $value
    }

    function IsType($item) { $item.Kind -in 'Class', 'Struct', 'Interface', 'Enum', 'Delegate' }

    function Report([hashtable]$baseApi, [hashtable]$headApi, [string[]]$projectNames, [string]$description, [string[]]$filesWithExtensionBlocks) {
        $report = [System.Collections.Generic.List[string]]::new()
        $report.Add("# Public API changes: $description")
        $report.Add('')
        $report.Add('Made by C-Compare-PublicApi, from the declarations DocFX''s metadata step gives each public and protected type and member. A method whose parameter types changed is listed as one removed and one added.')
        if ($filesWithExtensionBlocks) {
            $report.Add('')
            $report.Add('**Not in this report: the members of C# 14 extension blocks.** DocFX skips them (its pull request 10943 made it skip them rather than fail), so compare them with git diff. These changed files hold extension blocks:')
            $report.Add('')
            foreach ($file in $filesWithExtensionBlocks) { $report.Add("- ``$file``") }
        }

        $unchanged = [System.Collections.Generic.List[string]]::new()
        foreach ($project in $projectNames) {
            $baseItems = @($baseApi.Values | Where-Object Assembly -eq $project)
            $headItems = @($headApi.Values | Where-Object Assembly -eq $project)
            $baseTypes = @{}; $baseItems | Where-Object { IsType $_ } | ForEach-Object { $baseTypes[$_.Uid] = $_ }
            $headTypes = @{}; $headItems | Where-Object { IsType $_ } | ForEach-Object { $headTypes[$_.Uid] = $_ }

            $addedTypes = @($headTypes.Values | Where-Object { -not $baseTypes.ContainsKey($_.Uid) } | Sort-Object FullName)
            $removedTypes = @($baseTypes.Values | Where-Object { -not $headTypes.ContainsKey($_.Uid) } | Sort-Object FullName)
            $keptTypeUids = @($headTypes.Keys | Where-Object { $baseTypes.ContainsKey($_) } | Sort-Object)

            $changes = [System.Collections.Generic.List[string]]::new()
            $counts = [ordered]@{ AddedMembers = 0; RemovedMembers = 0; ChangedMembers = 0; ChangedTypes = 0 }
            foreach ($typeUid in $keptTypeUids) {
                $typeChanges = [System.Collections.Generic.List[string]]::new()
                $baseType = $baseTypes[$typeUid]; $headType = $headTypes[$typeUid]
                if ($baseType.Declaration -ne $headType.Declaration) {
                    $typeChanges.Add("- changed: ``$($baseType.Declaration)``")
                    $typeChanges.Add("  to: ``$($headType.Declaration)``")
                    $counts.ChangedTypes++
                }
                $baseMembers = @{}; $baseItems | Where-Object Parent -eq $typeUid | ForEach-Object { $baseMembers[$_.Uid] = $_ }
                $headMembers = @{}; $headItems | Where-Object Parent -eq $typeUid | ForEach-Object { $headMembers[$_.Uid] = $_ }
                foreach ($member in ($baseMembers.Values | Sort-Object Declaration)) {
                    if (-not $headMembers.ContainsKey($member.Uid)) { $typeChanges.Add("- removed: ``$($member.Declaration)``"); $counts.RemovedMembers++ }
                    elseif ($headMembers[$member.Uid].Declaration -ne $member.Declaration) {
                        $typeChanges.Add("- changed: ``$($member.Declaration)``")
                        $typeChanges.Add("  to: ``$($headMembers[$member.Uid].Declaration)``")
                        $counts.ChangedMembers++
                    }
                }
                foreach ($member in ($headMembers.Values | Sort-Object Declaration)) {
                    if (-not $baseMembers.ContainsKey($member.Uid)) { $typeChanges.Add("- added: ``$($member.Declaration)``"); $counts.AddedMembers++ }
                }
                if ($typeChanges.Count -gt 0) {
                    $changes.Add('')
                    $changes.Add("#### ``$($headType.FullName)``")
                    $changes.Add('')
                    $changes.AddRange($typeChanges)
                }
            }

            if ($addedTypes.Count + $removedTypes.Count + $changes.Count -eq 0) { $unchanged.Add($project); continue }
            $report.Add('')
            $report.Add("## $project")
            $report.Add('')
            $report.Add("$($addedTypes.Count) types added, $($removedTypes.Count) removed, $($counts.ChangedTypes) changed; in the types on both sides, $($counts.AddedMembers) members added, $($counts.RemovedMembers) removed, $($counts.ChangedMembers) changed.")
            if ($addedTypes.Count -gt 0) {
                $report.Add(''); $report.Add('### Added types')
                foreach ($type in $addedTypes) {
                    $report.Add(''); $report.Add("- ``$($type.FullName)``: ``$($type.Declaration)``")
                    $headItems | Where-Object Parent -eq $type.Uid | Sort-Object Declaration | ForEach-Object { $report.Add("  - ``$($_.Declaration)``") }
                }
            }
            if ($removedTypes.Count -gt 0) {
                $report.Add(''); $report.Add('### Removed types'); $report.Add('')
                foreach ($type in $removedTypes) { $report.Add("- ``$($type.FullName)``: ``$($type.Declaration)``") }
            }
            if ($changes.Count -gt 0) {
                $report.Add(''); $report.Add('### Changed types')
                $report.AddRange($changes)
            }
        }
        if ($unchanged.Count -gt 0) {
            $report.Add('')
            $report.Add("## No public API change")
            $report.Add('')
            $report.Add("Changed files, but not their public API: $($unchanged -join ', ').")
        }
        $report
    }

    if (-not (Get-Command docfx -ErrorAction SilentlyContinue)) {
        throw "docfx isn't installed. Install it with: dotnet tool install -g docfx"
    }

    $headRef = if ($Head) { $Head } else { 'HEAD' }
    $mergeBase = (Invoke-Git merge-base $Base $headRef).Trim()
    $projectNames = @(ProjectsToCompare $mergeBase)
    if ($projectNames.Count -eq 0) { Write-Output "No Compze project under src changed between $Base and $(if ($Head) { $Head } else { 'the working tree' })."; return }

    $workRoot = Join-Path ([System.IO.Path]::GetTempPath()) "C-Compare-PublicApi-$PID"
    $baseFolder = Join-Path $workRoot 'base'
    $headFolder = Join-Path $workRoot 'head'
    try {
        $baseApi = PublicApiOf (CheckOut $mergeBase $baseFolder) $projectNames (Join-Path $workRoot 'base-docfx')
        $headRoot = if ($Head) { CheckOut (Invoke-Git rev-parse $Head).Trim() $headFolder } else { $script:CompzeRoot }
        $headApi = PublicApiOf $headRoot $projectNames (Join-Path $workRoot 'head-docfx')

        $headDescription = if ($Head) { "$Head ($((Invoke-Git rev-parse --short $Head).Trim()))" } else { "the working tree of $((Invoke-Git rev-parse --abbrev-ref HEAD).Trim())" }
        $description = "$headDescription, against its merge-base with $Base ($($mergeBase.Substring(0, 9)))"
        $report = Report $baseApi $headApi $projectNames $description @(ChangedFilesWithExtensionBlocks $mergeBase $projectNames)
        if ($OutFile) { $report | Set-Content -Encoding utf8 $OutFile } else { $report }
    }
    finally {
        Remove-CheckOut $baseFolder
        Remove-CheckOut $headFolder
        if (Test-Path $workRoot) { Remove-Item -Recurse -Force $workRoot }
        & git -C $script:CompzeRoot worktree prune
    }
}
