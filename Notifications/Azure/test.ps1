param ($servername, $envname)
write-host "If this script were really going to do something, it would do it on $servername in the $envname environment" 
.\Deploy.ps1 -resourceGroup SharePointNotifications -region canadaCentral

Get-Location