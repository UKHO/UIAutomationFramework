param
(
    $pfxString,
    $pfxPasswordVariableName = "pfxPassword", # See for details on why not passing string - https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops&tabs=yaml%2Cbatch#secret-variables
    $pfxOutLocation = "$env:Build_SourcesDirectory\CodeSignCert.pfx"
)

Write-Host "`$pfxPasswordVariableName set to $pfxPasswordVariableName"
Write-Host "`$pfxOutLocation` set to $pfxOutLocation"

if(-not(Test-Path env:$pfxPasswordVariableName)){
    $errorMessage = "$pfxPasswordVariableName does not exist as an environment variable. This needs to be set and contain the intended password for the PFX"
    Write-Host $errorMessage
    Write-Host "##vso[task.logissue type=error]$errorMessage"
    exit 1
}

$kvSecretBytes = [System.Convert]::FromBase64String("$pfxString")
$certCollection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection 
$certCollection.Import($kvSecretBytes,$null,[System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
$protectedCertificateBytes = $certCollection.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12, [Environment]::GetEnvironmentVariable($pfxPasswordVariableName))
[System.IO.File]::WriteAllBytes("$pfxOutLocation", $protectedCertificateBytes)

Write-Host
Write-Host "PFX created at $pfxOutLocation"
Write-Host "##vso[task.setvariable variable=pfxLocation;]$pfxOutLocation"
Write-Host
Write-Host "Location of the PFX is now available as both an environmental variable or build variable"
Write-Host "Environment variable - `$env:pfxLocation"
Write-Host "Build variable - `$(pfxLocation)"