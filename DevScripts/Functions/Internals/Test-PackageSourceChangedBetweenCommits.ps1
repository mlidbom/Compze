# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Test-PackageSourceChangedBetweenCommits {
    <#
    .SYNOPSIS
    Tells whether a package's project folder (src/<PackageId>) differs between two commits

    .DESCRIPTION
    What decides whether two builds of one package version are the same code. Fetches from origin first when a commit is
    not in this clone, as happens with a release built from a commit pushed elsewhere.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$FromCommit,
        [Parameter(Mandatory = $true)]
        [string]$ToCommit
    )

    if ($FromCommit -eq $ToCommit) { return $false }

    foreach ($commit in @($FromCommit, $ToCommit)) {
        git -C $script:CompzeRoot cat-file -e "$commit^{commit}" 2>$null
        if ($LASTEXITCODE -ne 0) { git -C $script:CompzeRoot fetch --quiet origin 2>$null | Out-Null }
    }

    git -C $script:CompzeRoot diff --quiet $FromCommit $ToCommit -- "src/$PackageId"
    switch ($LASTEXITCODE) {
        0 { return $false }
        1 { return $true }
        default { throw "Could not compare src/$PackageId between commits $FromCommit and $ToCommit (git exit code $LASTEXITCODE)." }
    }
}
