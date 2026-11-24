Push-Location $PSScriptRoot #knowing which folder we are in is good :)

dotnet tool update -g docfx

$buildFolder = "$PSScriptRoot\_site"

# robocopy /MIR below makes gh-pages a mirror of _site, so whatever an earlier build left in _site would be published too.
if(Test-Path $buildFolder) { Remove-Item -Recurse -Force $buildFolder }

# The same build C-Build-Website makes for checking a change, which fails on any DocFX warning: a broken link or code
# include is never published.
Import-Module "$PSScriptRoot\..\..\..\DevScripts\Compze.psm1" -DisableNameChecking
C-Build-Website
if($LASTEXITCODE -ne 0) { Pop-Location; throw "The website didn't build cleanly, so nothing was published." }

$ghPagesCheckoutFolder = "$PSScriptRoot/../../Compze-gh-pages"

if(!(Test-Path $ghPagesCheckoutFolder))
{
    Write-Host "Missing gh-pages checkout. Cloning"
    Push-Location "$ghPagesCheckoutFolder/.."
    git clone --quiet --single-branch --branch gh-pages 'https://github.com/mlidbom/Compze.git' Compze-gh-pages
    Pop-Location
}


$ghPagesCheckoutFolder = (Get-Item $ghPagesCheckoutFolder).FullName
Push-Location $ghPagesCheckoutFolder
git config core.autocrlf true
git config core.safecrlf false

git checkout -f 
git clean -fd
git pull

Pop-Location

robocopy.exe /MIR /NFL /NDL "$buildFolder" "$ghPagesCheckoutFolder" /XD ".git" "node_modules" /XF ".gitignore" "CNAME"

Push-Location $ghPagesCheckoutFolder

git add .
git commit -a -m "Automatic commit created by automated build and publish script."
git push

Pop-Location
Pop-Location