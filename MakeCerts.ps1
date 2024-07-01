Write-Host "Creating Certificates for Self-Signed Testing"

$hostName = [System.Net.Dns]::GetHostByName($env:computerName).HostName
$subject = "CN={0},O=Infoscitex,OU=ARES,L=Dayton,S=Ohio,C=US"
$rootSubject = $subject -f "ARESRoot"
$clientSubject = $subject -f "ARESClient"
$serviceSubject = $subject -f "ARESService"


Write-Host "Creating Root Certificate"
$cert = New-SelfSignedCertificate -Type Custom -KeySpec Signature `
-Subject $rootSubject `
-FriendlyName "ARESRootCert" `
-KeyExportPolicy Exportable `
-HashAlgorithm sha256 -KeyLength 4096 `
-CertStoreLocation "cert://currentuser/My" `
-KeyUsageProperty Sign `
-KeyUsage CertSign `
-NotAfter (Get-Date).AddYears(5)


# Client Auth
Write-Host "Creating UI Client Auth Certificate"
$clientCert = New-SelfSignedCertificate -Type Custom -KeySpec Signature `
-Subject $clientSubject -KeyExportPolicy Exportable `
-FriendlyName "ARESClientCert" `
-HashAlgorithm sha256 -KeyLength 2048 `
-NotAfter (Get-Date).AddMonths(24) `
-CertStoreLocation "cert://currentuser/My" `
-Signer $cert -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2")

# TLS Cert
Write-Host "Creating ARES Service Certificate"
$serviceCert = New-SelfSignedCertificate -Type Custom `
-Subject $serviceSubject -KeyExportPolicy Exportable `
-DnsName $hostName, "localhost" `
-FriendlyName "ARESServiceCert" `
-HashAlgorithm sha256 -KeyLength 2048 `
-KeyUsage "KeyEncipherment", "DigitalSignature" `
-NotAfter (Get-Date).AddMonths(24) `
-CertStoreLocation "cert://currentuser/My" `
-Signer $cert -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")


$PFXPass = ConvertTo-SecureString -String "SecurePassword" -Force -AsPlainText

Write-Host "Exporting Certificates to File"

Export-PfxCertificate -Cert $clientCert `
-Password $PFXPass `
-FilePath ARESClientCert.pfx

Export-PfxCertificate -Cert $serviceCert `
-Password $PFXPass `
-FilePath ARESServiceCert.pfx

