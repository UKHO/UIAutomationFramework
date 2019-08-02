param (
    [Parameter(Mandatory = $true)] [string] $buildNumber,
    [Parameter(Mandatory = $true)] [string] $solutionDirectory
)

$buildNumberRegex = "(.+)_([1-9][0-9]*).([0-9]*).([0-9]{3,5}).([0-9]{1,2})"
$validBuildNumber = $buildNumber -match $buildNumberRegex

if ($validBuildNumber -eq $false) {
    Write-Error "Build number passed in must be in the following format: (BuildDefinitionName)_(ProjectVersion).(date:yy)(DayOfYear)(rev:.r)"
    return
}

$buildNumberSplit = $buildNumber.Split('_')
$buildRevisionNumber = $buildNumberSplit[1] -replace ".DRAFT", ""
$versionToApply = "$buildRevisionNumber"

$DefaultAssemblyCompany = "UK Hydrographic Office";
$DefaultAssemblyCopyright = "Copyright © UK Hydrographic Office " + (Get-Date).Year;
$DefaultAssemblyDescription = "The automation framework is intended to help automate the browser for automated tests. It's intended to work with SpecFlow, and by default uses Selenium as the web driver.";
$DefaultAssemblyProduct = "UKHO.UIAutomationFramework";

$assemblyValues = @{
    "Company"         = $DefaultAssemblyCompany;
    "Copyright"       = $DefaultAssemblyCopyright;
    "Description"     = $DefaultAssemblyDescription;
    "Product"         = $DefaultAssemblyProduct;
    "AssemblyVersion" = $versionToApply;
    "FileVersion"     = $versionToApply;
    "Version"         = $versionToApply;
}

function UpdateOrAddAttribute($xmlContent, $assemblyKey, $newValue, $namespace) {
    $propertyGroup = $xmlContent.Project.PropertyGroup
    if ($propertyGroup -is [array]) {
        $propertyGroup = $propertyGroup[0]
    }

    $propertyGroupNode = $propertyGroup.$assemblyKey

    if ($propertyGroupNode -ne $null) {
        Write-Host "Assembly key $assemblyKey has been located in source file - updating"
        $propertyGroup.$assemblyKey = $newValue
        return $xmlContent
    }

    Write-Host "Assembly key $assemblyKey could not be located in source file - appending"

    $newChild = $xmlContent.CreateElement($assemblyKey, $namespace)
    $newChild.InnerText = $newValue
    $propertyGroup.AppendChild($newChild)

    return $propertyGroupNode
}

function FindOrReplaceAttribute($filecontent, $assemblyValue, $newValue) {
    $regex = "\[assembly: Assembly" + $assemblyValue + "\((.+)\)\]"
    $replacement = '[assembly: Assembly' + $assemblyValue + '("' + $newValue + '")]'

    $propertyExists = [regex]::matches($filecontent, $regex)

    if ($propertyExists.Count -eq 1) {
        return $filecontent -replace $regex, $replacement
    }

    return $filecontent + $replacement
}

(Get-ChildItem -Path $solutionDirectory -File -Filter "*.csproj" -Recurse) | ForEach-Object {
    $file = $_

    Write-Host "Updating assembly file at path: $file"
    [xml]$xmlContent = (Get-Content $file.FullName)

    $assemblyValues.Keys | ForEach-Object {
        $key = $_

        UpdateOrAddAttribute $xmlContent $key $assemblyValues[$key] $xmlContent.DocumentElement.NamespaceURI
    }

    $xmlContent.Save($file.FullName)
}


# Apply the version to the assembly property files
$files = Get-ChildItem $solutionDirectory -recurse -include "*Properties*" |
Where-Object { $_.PSIsContainer } |
ForEach-Object { Get-ChildItem -Path $_.FullName -Recurse -include *AssemblyInfo* }
if ($files) {
    Write-Verbose "Will apply $versionToApply to $($files.count) files."

    foreach ($file in $files) {
        $filecontent = Get-Content($file)
        attrib.exe $file -r
        $filecontent = FindOrReplaceAttribute $filecontent "Company" $DefaultAssemblyCompany
        $filecontent = FindOrReplaceAttribute $filecontent "Copyright" $DefaultAssemblyCopyright
        $filecontent = FindOrReplaceAttribute $filecontent "Description" $DefaultAssemblyDescription
        $filecontent = FindOrReplaceAttribute $filecontent "Product" $DefaultAssemblyProduct
        $filecontent = FindOrReplaceAttribute $filecontent "Version" $versionToApply
        $filecontent = FindOrReplaceAttribute $filecontent "FileVersion" $versionToApply

        $filecontent | Out-File $file
        Write-Verbose "$file - Assembly Changes Applied"
    }
}
else {
    Write-Warning "Found no files."
}