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
    method whose parameter types changed is a different method, so it's listed as one removed and one added, unless the
    only change is a paired type's new name (below). Doc comments are not compared.

    A type that moved to another namespace or project, or was renamed, is listed once, as moved or renamed, with the
    changes to its declaration and members, when evidence pairs it with what it was. The report says, for each pairing,
    what it rests on. The evidence, tried again for as long as it pairs more:
    - The same full name, in another project.
    - Its file. A pair of files is a file git sees renamed (git diff -M, at git's default of 50% similar), a file with the
      same path on both sides, or the files that declare two types already paired. In a pair of files, the types of the
      same name pair, and then the one type left unpaired on each side, if there is one.
    - Its namespace's move. A project's rename (its project file renamed, as git sees it) moves the namespaces under the
      project's name, and a type already paired moves its own namespace. A type then pairs with the type of the same name
      in the namespace it moved to, or of its name renamed as the namespace was: the move from
      Compze.Teventive.StateStore to Compze.Teventive.MemoryImageStateStore pairs IStateStore with IMemoryImageStateStore.
    A type that the evidence would pair with two others is paired with neither. Pairing happens only among the projects
    compared, so a renamed project's old and new names must both be among them, as they are by default. The paired types'
    new names are then used on the base side too, so that a member is compared with its counterpart, and a declaration
    that only names a paired type is not listed as changed. A type that nothing pairs is listed as removed, and what
    replaced it as added.

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

    # Without rename detection, a renamed file is listed under both its paths, so that a renamed project is compared under
    # its old name as well as its new one.
    function ChangedPaths([string]$mergeBase, [string[]]$pathspecs) {
        if ($Head) { Invoke-Git diff --name-only --no-renames $mergeBase $Head '--' @pathspecs }
        else { @(Invoke-Git diff --name-only --no-renames $mergeBase '--' @pathspecs) + @(Invoke-Git ls-files --others --exclude-standard '--' @pathspecs) }
    }

    function ChangedProjects([string]$mergeBase) {
        ChangedPaths $mergeBase 'src' | ForEach-Object { ($_ -split '/')[1] } | Where-Object { $_ -like 'Compze.*' } | Sort-Object -Unique
    }

    # The files under src that git sees renamed, at its default of 50% similar. In the working tree, it sees only files that
    # git tracks.
    function FileRenames([string]$mergeBase) {
        $statuses = if ($Head) { Invoke-Git diff -M --name-status $mergeBase $Head '--' src } else { Invoke-Git diff -M --name-status $mergeBase '--' src }
        foreach ($status in $statuses) {
            if ($status -match '^R(\d+)\t(.+)\t(.+)$') { [pscustomobject]@{ Old = $Matches[2]; New = $Matches[3]; Similarity = [int]$Matches[1] } }
        }
    }

    # The projects whose project file git sees renamed, src/<Old>/<Old>.csproj to src/<New>/<New>.csproj.
    function ProjectRenames([object[]]$fileRenames) {
        foreach ($rename in $fileRenames) {
            if ($rename.Old -match '^src/([^/]+)/\1\.csproj$') {
                $old = $Matches[1]
                if ($rename.New -match '^src/([^/]+)/\1\.csproj$') { [pscustomobject]@{ Old = $old; New = $Matches[1] } }
            }
        }
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
        foreach ($item in $api.Values) {
            if ($item.SourcePath) { $item.SourcePath = [System.IO.Path]::GetRelativePath($root, $item.SourcePath) -replace '\\', '/' }
        }
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
            $api[$uid] = [pscustomobject]@{ Uid = $uid; Kind = 'Constructor'; Parent = $class.Uid; FullName = ''; Assembly = $class.Assembly; Namespace = $class.Namespace; SourcePath = ''; Declaration = "$accessibility $simpleName() (implicit)" }
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
        $inSource = $false
        for ($index = 0; $index -lt $lines.Count; $index++) {
            $line = $lines[$index]
            if ($line -eq 'references:') { break }
            if ($line -match '^- uid: (.+)$') {
                $item = [pscustomobject]@{ Uid = (Unquote $Matches[1]); Kind = ''; Parent = ''; FullName = ''; Assembly = ''; Namespace = ''; SourcePath = ''; Declaration = '' }
                $items.Add($item)
                $inAssemblies = $false
                $inSource = $false
                continue
            }
            if (-not $item) { continue }
            if ($inAssemblies -and $line -match '^  - (.+)$') { if (-not $item.Assembly) { $item.Assembly = $Matches[1] }; continue }
            $inAssemblies = $line -eq '  assemblies:'
            if ($line -match '^  \S') { $inSource = $line -eq '  source:' }
            elseif ($inSource -and $line -match '^    path: (.+)$') { $item.SourcePath = Unquote $Matches[1]; continue }
            if ($line -match '^  type: (.+)$') { $item.Kind = $Matches[1] }
            elseif ($line -match '^  parent: (.+)$') { $item.Parent = Unquote $Matches[1] }
            elseif ($line -match '^  fullName: (.+)$') { $item.FullName = Unquote $Matches[1] }
            elseif ($line -match '^  namespace: (.+)$') { $item.Namespace = Unquote $Matches[1] }
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

    function TypesOf([hashtable]$api) {
        $types = @{}
        foreach ($item in $api.Values) { if (IsType $item) { $types[$item.Uid] = $item } }
        $types
    }

    # The members of each type, by the type's uid.
    function MembersByType([hashtable]$api) {
        $members = @{}
        foreach ($item in $api.Values) {
            if (IsType $item) { continue }
            if (-not $members.ContainsKey($item.Parent)) { $members[$item.Parent] = [System.Collections.Generic.List[object]]::new() }
            $members[$item.Parent].Add($item)
        }
        $members
    }

    # A type's name in its namespace, as in its uid: with the outer types of a nested type, and the arity of a generic one,
    # such as TeventStoreApi.TommandApi.AddTaggregate`1.
    function NameInNamespace($type) {
        if ($type.Namespace -and $type.Uid.StartsWith("$($type.Namespace).", [System.StringComparison]::Ordinal)) { return $type.Uid.Substring($type.Namespace.Length + 1) }
        $type.Uid
    }

    # A type's own name, without its namespace and outer types, as in its uid: AddTaggregate`1.
    function OwnName($type) { ((NameInNamespace $type) -split '\.')[-1] }

    # A type's own name as C# writes it in a declaration, without the arity: AddTaggregate.
    function SimpleName($type) { (OwnName $type) -replace '`\d+$', '' }

    function TypePair($baseType, $headType, [string]$evidence) { [pscustomobject]@{ Base = $baseType; Head = $headType; Evidence = $evidence } }

    # Each removed type paired with the added type that is the same type, moved to another namespace or project, or
    # renamed, as far as the evidence the .DESCRIPTION lists shows it.
    function PairedTypes([hashtable]$baseTypes, [hashtable]$headTypes, [object[]]$fileRenames) {
        $pairs = [System.Collections.Generic.List[object]]::new()
        foreach ($uid in @($baseTypes.Keys | Where-Object { $headTypes.ContainsKey($_) -and $baseTypes[$_].Assembly -ne $headTypes[$_].Assembly } | Sort-Object)) {
            $pairs.Add((TypePair $baseTypes[$uid] $headTypes[$uid] 'the same full name'))
        }
        $removed = @{}; foreach ($type in $baseTypes.Values) { if (-not $headTypes.ContainsKey($type.Uid)) { $removed[$type.Uid] = $type } }
        $added = @{}; foreach ($type in $headTypes.Values) { if (-not $baseTypes.ContainsKey($type.Uid)) { $added[$type.Uid] = $type } }
        $projectRenames = @(ProjectRenames $fileRenames)

        function AddUnambiguous([object[]]$proposals) {
            foreach ($pair in (Unambiguous $proposals)) {
                $pairs.Add($pair)
                $removed.Remove($pair.Base.Uid)
                $added.Remove($pair.Head.Uid)
            }
        }

        do {
            $pairedBefore = $pairs.Count
            AddUnambiguous @(ProposalsByFile $removed $added $fileRenames $pairs)
            AddUnambiguous @(ProposalsByNamespace $removed $added $projectRenames $pairs)
        } while ($pairs.Count -gt $pairedBefore)
        $pairs
    }

    # The proposed pairs in which neither type is proposed with another: the first evidence given for each.
    function Unambiguous([object[]]$proposals) {
        $distinct = @($proposals | Group-Object { "$($_.Base.Uid)|$($_.Head.Uid)" } | ForEach-Object { $_.Group[0] })
        $proposalsOfBase = @{}; $proposalsOfHead = @{}
        foreach ($proposal in $distinct) { $proposalsOfBase[$proposal.Base.Uid] += 1; $proposalsOfHead[$proposal.Head.Uid] += 1 }
        $distinct | Where-Object { $proposalsOfBase[$_.Base.Uid] -eq 1 -and $proposalsOfHead[$_.Head.Uid] -eq 1 }
    }

    # Pairs proposed by the files that declare the types: a file git sees renamed, a file with the same path on both sides,
    # or the files of two types already paired. In them, the types of the same name pair, and then the one type left
    # unpaired on each side, if there is one.
    function ProposalsByFile([hashtable]$removed, [hashtable]$added, [object[]]$fileRenames, $pairs) {
        $filePairs = [ordered]@{}
        function AddFilePair([string]$old, [string]$new, [string]$description) {
            if ($old -and $new -and -not $filePairs.Contains("$old|$new")) { $filePairs["$old|$new"] = [pscustomobject]@{ Old = $old; New = $new; Description = $description } }
        }
        foreach ($rename in $fileRenames) { AddFilePair $rename.Old $rename.New "``$($rename.Old)``, and in ``$($rename.New)``, which git sees as its rename ($($rename.Similarity)% similar)" }
        foreach ($pair in $pairs) { AddFilePair $pair.Base.SourcePath $pair.Head.SourcePath "``$($pair.Base.SourcePath)``, and in ``$($pair.Head.SourcePath)``, the files of ``$(NameInNamespace $pair.Base)`` and ``$(NameInNamespace $pair.Head)``, which are paired" }
        foreach ($type in $removed.Values) { AddFilePair $type.SourcePath $type.SourcePath "``$($type.SourcePath)``, on both sides" }

        foreach ($filePair in $filePairs.Values) {
            $before = @($removed.Values | Where-Object SourcePath -eq $filePair.Old)
            $after = @($added.Values | Where-Object SourcePath -eq $filePair.New)
            if ($before.Count -eq 0 -or $after.Count -eq 0) { continue }
            $byName = @(foreach ($type in $before) {
                $sameName = @($after | Where-Object { (OwnName $_) -ceq (OwnName $type) })
                if ($sameName.Count -eq 1) { TypePair $type $sameName[0] "its file: the type of the same name in $($filePair.Description)" }
            })
            $byName
            $beforeLeft = @($before | Where-Object { $_.Uid -notin $byName.Base.Uid })
            $afterLeft = @($after | Where-Object { $_.Uid -notin $byName.Head.Uid })
            if ($beforeLeft.Count -eq 1 -and $afterLeft.Count -eq 1) { TypePair $beforeLeft[0] $afterLeft[0] "its file: the one type left unpaired in $($filePair.Description)" }
        }
    }

    # Pairs proposed by the moves of namespaces: a project's rename moves the namespaces under the project's name, and a
    # type already paired moves its own. A removed type pairs with the added type in the namespace it moved to that has its
    # name, or its name renamed as the namespace was.
    function ProposalsByNamespace([hashtable]$removed, [hashtable]$added, [object[]]$projectRenames, $pairs) {
        $moves = [ordered]@{}
        function AddMove([string]$old, [string]$new, [string]$description) {
            if ($old -and $new -and $old -cne $new -and -not $moves.Contains("$old|$new")) { $moves["$old|$new"] = [pscustomobject]@{ Old = $old; New = $new; Description = $description } }
        }
        foreach ($rename in $projectRenames) { AddMove $rename.Old $rename.New "the rename of the project $($rename.Old) to $($rename.New)" }
        foreach ($pair in $pairs) { AddMove $pair.Base.Namespace $pair.Head.Namespace "the move of ``$($pair.Base.FullName)`` to ``$($pair.Head.FullName)``" }

        foreach ($type in $removed.Values) {
            foreach ($move in ($moves.Values | Sort-Object { $_.Old.Length } -Descending)) {
                if ($type.Namespace -cne $move.Old -and -not $type.Namespace.StartsWith("$($move.Old).", [System.StringComparison]::Ordinal)) { continue }
                $namespace = $move.New + $type.Namespace.Substring($move.Old.Length)
                $name = NameInNamespace $type
                $evidence = "its namespace's move from ``$($type.Namespace)`` to ``$namespace``, shown by $($move.Description)"
                if ($added.ContainsKey("$namespace.$name")) { TypePair $type $added["$namespace.$name"] $evidence }
                $segment = RenamedSegment $move
                if (-not $segment) { continue }
                $renamedName = $name -creplace "$([regex]::Escape($segment.Old))(?![a-z])", $segment.New
                if ($renamedName -cne $name -and $added.ContainsKey("$namespace.$renamedName")) {
                    TypePair $type $added["$namespace.$renamedName"] "$evidence, with $($segment.Old) renamed $($segment.New) in its name too"
                }
            }
        }
    }

    # The one segment of a namespace that a move renames, such as StateStore to MemoryImageStateStore. None when the move
    # changes more than one segment, or the number of them.
    function RenamedSegment($move) {
        $old = @($move.Old -split '\.'); $new = @($move.New -split '\.')
        if ($old.Count -ne $new.Count) { return $null }
        $differing = @(0..($old.Count - 1) | Where-Object { $old[$_] -cne $new[$_] })
        if ($differing.Count -ne 1) { return $null }
        [pscustomobject]@{ Old = $old[$differing[0]]; New = $new[$differing[0]] }
    }

    # What the paired types are called on the head side, for reading the base side with: in uids, a paired type's full name
    # before is replaced by its full name now, and in declarations, its simple name before by its simple name now. A generic
    # type's uid is used without its arity, as a member's uid names it in a parameter:
    # Compze.Teventive.ITeventRoutedPart{System.String}.
    function NamesOnTheHeadSide([object[]]$pairs) {
        $uids = @{}; $simpleNames = @{}
        foreach ($pair in $pairs) {
            $uidBefore = $pair.Base.Uid -replace '`\d+$', ''; $uidNow = $pair.Head.Uid -replace '`\d+$', ''
            if ($uidBefore -cne $uidNow) { $uids[$uidBefore] = $uidNow }
            $simpleNameBefore = SimpleName $pair.Base; $simpleNameNow = SimpleName $pair.Head
            if ($simpleNameBefore -cne $simpleNameNow) { $simpleNames[$simpleNameBefore] = $simpleNameNow }
        }
        [pscustomobject]@{
            Uids              = $uids
            UidPattern        = Alternation $uids.Keys '(?<![\w.`])' '(?!\w)'
            SimpleNames       = $simpleNames
            SimpleNamePattern = Alternation $simpleNames.Keys '\b' '\b'
        }
    }

    # A regular expression that matches any of the names, the longest first.
    function Alternation([string[]]$names, [string]$before, [string]$after) {
        if (-not $names) { return '' }
        "$before(?:$(($names | Sort-Object Length -Descending | ForEach-Object { [regex]::Escape($_) }) -join '|'))$after"
    }

    function WithNamesOnTheHeadSide([string]$text, [string]$pattern, [hashtable]$names) {
        if (-not $pattern) { return $text }
        $text -creplace $pattern, { $names[$_.Value] }
    }

    # A member's uid without its type's, so that a member of a paired type is found under its counterpart's.
    function MemberKey($member, $type) {
        if ($member.Uid.StartsWith($type.Uid, [System.StringComparison]::Ordinal)) { return $member.Uid.Substring($type.Uid.Length) }
        $member.Uid
    }

    # What changed in a type that is on both sides: its declaration, and its members added, removed and changed. The base
    # side is read with the paired types' names on the head side.
    function TypeChanges($baseType, $headType, [hashtable]$baseMembers, [hashtable]$headMembers, $headSideNames) {
        $changes = [pscustomobject]@{ Lines = [System.Collections.Generic.List[string]]::new(); DeclarationChanged = $false; AddedMembers = 0; RemovedMembers = 0; ChangedMembers = 0 }
        function Differs([string]$declarationBefore, [string]$declarationNow) {
            $declarationBefore -cne $declarationNow -and (WithNamesOnTheHeadSide $declarationBefore $headSideNames.SimpleNamePattern $headSideNames.SimpleNames) -cne $declarationNow
        }

        if (Differs $baseType.Declaration $headType.Declaration) {
            $changes.Lines.Add("- changed: ``$($baseType.Declaration)``")
            $changes.Lines.Add("  to: ``$($headType.Declaration)``")
            $changes.DeclarationChanged = $true
        }
        $before = @{}; foreach ($member in $baseMembers[$baseType.Uid]) { $before[(WithNamesOnTheHeadSide (MemberKey $member $baseType) $headSideNames.UidPattern $headSideNames.Uids)] = $member }
        $now = @{}; foreach ($member in $headMembers[$headType.Uid]) { $now[(MemberKey $member $headType)] = $member }
        foreach ($entry in ($before.GetEnumerator() | Sort-Object { $_.Value.Declaration })) {
            $member = $entry.Value
            if (-not $now.ContainsKey($entry.Key)) { $changes.Lines.Add("- removed: ``$($member.Declaration)``"); $changes.RemovedMembers++ }
            elseif (Differs $member.Declaration $now[$entry.Key].Declaration) {
                $changes.Lines.Add("- changed: ``$($member.Declaration)``")
                $changes.Lines.Add("  to: ``$($now[$entry.Key].Declaration)``")
                $changes.ChangedMembers++
            }
        }
        foreach ($entry in ($now.GetEnumerator() | Sort-Object { $_.Value.Declaration })) {
            if (-not $before.ContainsKey($entry.Key)) { $changes.Lines.Add("- added: ``$($entry.Value.Declaration)``"); $changes.AddedMembers++ }
        }
        $changes
    }

    function Report([hashtable]$baseApi, [hashtable]$headApi, [string[]]$projectNames, [string]$description, [string[]]$filesWithExtensionBlocks, [object[]]$fileRenames) {
        $baseTypes = TypesOf $baseApi; $headTypes = TypesOf $headApi
        $baseMembers = MembersByType $baseApi; $headMembers = MembersByType $headApi
        $pairs = @(PairedTypes $baseTypes $headTypes $fileRenames)
        $paired = @{}; foreach ($pair in $pairs) { $paired["base|$($pair.Base.Uid)"] = $true; $paired["head|$($pair.Head.Uid)"] = $true }
        $headSideNames = NamesOnTheHeadSide $pairs

        $report = [System.Collections.Generic.List[string]]::new()
        $report.Add("# Public API changes: $description")
        $report.Add('')
        $report.Add('Made by C-Compare-PublicApi, from the declarations DocFX''s metadata step gives each public and protected type and member. A method whose parameter types changed is listed as one removed and one added, unless the only change is a paired type''s new name. A type that moved to another namespace or project, or was renamed, is listed as moved or renamed, under the project it''s in now, with the evidence that pairs it with what it was. A type that nothing pairs is listed as removed, and what replaced it as added.')
        if ($filesWithExtensionBlocks) {
            $report.Add('')
            $report.Add('**Not in this report: the members of C# 14 extension blocks.** DocFX skips them (its pull request 10943 made it skip them rather than fail), so compare them with git diff. These changed files hold extension blocks:')
            $report.Add('')
            foreach ($file in $filesWithExtensionBlocks) { $report.Add("- ``$file``") }
        }

        $unchanged = [System.Collections.Generic.List[string]]::new()
        foreach ($project in $projectNames) {
            $addedTypes = @($headTypes.Values | Where-Object { $_.Assembly -eq $project -and -not $baseTypes.ContainsKey($_.Uid) -and -not $paired["head|$($_.Uid)"] } | Sort-Object FullName)
            $removedTypes = @($baseTypes.Values | Where-Object { $_.Assembly -eq $project -and -not $headTypes.ContainsKey($_.Uid) -and -not $paired["base|$($_.Uid)"] } | Sort-Object FullName)
            $keptTypeUids = @($headTypes.Values | Where-Object { $_.Assembly -eq $project -and $baseTypes.ContainsKey($_.Uid) -and $baseTypes[$_.Uid].Assembly -eq $project } | ForEach-Object Uid | Sort-Object)
            $pairsHere = @($pairs | Where-Object { $_.Head.Assembly -eq $project } | Sort-Object { $_.Head.FullName })
            $projectsMovedTo = @($pairs | Where-Object { $_.Base.Assembly -eq $project -and $_.Head.Assembly -ne $project } | ForEach-Object { $_.Head.Assembly })

            $counts = [ordered]@{ AddedMembers = 0; RemovedMembers = 0; ChangedMembers = 0; ChangedTypes = 0 }
            function Counted($typeChanges) {
                $counts.AddedMembers += $typeChanges.AddedMembers; $counts.RemovedMembers += $typeChanges.RemovedMembers; $counts.ChangedMembers += $typeChanges.ChangedMembers
                $typeChanges
            }

            $changes = [System.Collections.Generic.List[string]]::new()
            foreach ($typeUid in $keptTypeUids) {
                $typeChanges = Counted (TypeChanges $baseTypes[$typeUid] $headTypes[$typeUid] $baseMembers $headMembers $headSideNames)
                if ($typeChanges.DeclarationChanged) { $counts.ChangedTypes++ }
                if ($typeChanges.Lines.Count -gt 0) {
                    $changes.Add('')
                    $changes.Add("#### ``$($headTypes[$typeUid].FullName)``")
                    $changes.Add('')
                    $changes.AddRange($typeChanges.Lines)
                }
            }

            $moves = [System.Collections.Generic.List[string]]::new()
            foreach ($pair in $pairsHere) {
                $typeChanges = Counted (TypeChanges $pair.Base $pair.Head $baseMembers $headMembers $headSideNames)
                $fromProject = if ($pair.Base.Assembly -ne $project) { "From the project $($pair.Base.Assembly). " } else { '' }
                $nothingElse = if ($typeChanges.Lines.Count -eq 0) { ' Nothing else changed.' } else { '' }
                $moves.Add('')
                $moves.Add("#### ``$($pair.Base.FullName)`` to ``$($pair.Head.FullName)``")
                $moves.Add('')
                $moves.Add("$($fromProject)Paired by $($pair.Evidence).$nothingElse")
                if ($typeChanges.Lines.Count -gt 0) { $moves.Add(''); $moves.AddRange($typeChanges.Lines) }
            }

            if ($addedTypes.Count + $removedTypes.Count + $changes.Count + $moves.Count + $projectsMovedTo.Count -eq 0) { $unchanged.Add($project); continue }
            $report.Add('')
            $report.Add("## $project")
            $report.Add('')
            $movedHere = if ($pairsHere.Count -gt 0) { ", $($pairsHere.Count) moved or renamed" } else { '' }
            $report.Add("$($addedTypes.Count) types added, $($removedTypes.Count) removed, $($counts.ChangedTypes) changed$movedHere; in the types on both sides, $($counts.AddedMembers) members added, $($counts.RemovedMembers) removed, $($counts.ChangedMembers) changed.")
            if ($projectsMovedTo.Count -gt 0) {
                $otherProjects = @($projectsMovedTo | Sort-Object -Unique)
                $where = if ($otherProjects.Count -eq 1) { 'another project, and are listed under it' } else { 'other projects, and are listed under them' }
                $report.Add('')
                $report.Add("$($projectsMovedTo.Count) of its types moved to $($where): $($otherProjects -join ', ').")
            }
            if ($addedTypes.Count -gt 0) {
                $report.Add(''); $report.Add('### Added types')
                foreach ($type in $addedTypes) {
                    $report.Add(''); $report.Add("- ``$($type.FullName)``: ``$($type.Declaration)``")
                    $headMembers[$type.Uid] | Sort-Object Declaration | ForEach-Object { $report.Add("  - ``$($_.Declaration)``") }
                }
            }
            if ($removedTypes.Count -gt 0) {
                $report.Add(''); $report.Add('### Removed types'); $report.Add('')
                foreach ($type in $removedTypes) { $report.Add("- ``$($type.FullName)``: ``$($type.Declaration)``") }
            }
            if ($moves.Count -gt 0) {
                $report.Add(''); $report.Add('### Moved and renamed types')
                $report.AddRange($moves)
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
        $report = Report $baseApi $headApi $projectNames $description @(ChangedFilesWithExtensionBlocks $mergeBase $projectNames) @(FileRenames $mergeBase)
        if ($OutFile) { $report | Set-Content -Encoding utf8 $OutFile } else { $report }
    }
    finally {
        Remove-CheckOut $baseFolder
        Remove-CheckOut $headFolder
        if (Test-Path $workRoot) { Remove-Item -Recurse -Force $workRoot }
        & git -C $script:CompzeRoot worktree prune
    }
}
