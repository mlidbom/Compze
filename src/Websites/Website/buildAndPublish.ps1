Push-Location $PSScriptRoot #knowing which folder we are in is good :)

dotnet tool update -g docfx

$buildFolder = "$PSScriptRoot\_site"
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
git config --global core.safecrlf false

git checkout -f 
git clean -fd
git pull

Pop-Location
Get-Location

docfx.exe

robocopy.exe /MIR /NFL /NDL "$buildFolder" "$ghPagesCheckoutFolder" /XD ".git" "node_modules" /XF ".gitignore" "CNAME"

Push-Location $ghPagesCheckoutFolder

git add .
git commit -a -m "Automatic commit created by automated build and publish script."
git push

Pop-Location
Pop-Location