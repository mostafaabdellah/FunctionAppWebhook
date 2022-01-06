#Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -AllowClobber -Force 
param ($resourceGroup, $region)
Connect-AzAccount
#$resourceGroup = 'SharePointNotifications2'
#$region = 'canadacentral'

New-AzResourceGroup  -Name $resourceGroup -Location $region
$nl = [Environment]::NewLine
$outputs=New-AzResourceGroupDeployment `
-Name $resourceGroup `
-ResourceGroupName $resourceGroup `
-TemplateFile '.\ARMTemplate.json'

foreach ($key in $outputs.Outputs.keys) {
    if ($key -eq "functionAppName") {
        $functionAppName = $outputs.Outputs[$key].value
        #Write-Output 'functionAppName: ' $functionAppName
    }elseif ($key -eq "storageAccountName") {
        $storageAccountName = $outputs.Outputs[$key].value
        #Write-Output 'storageAccountName: ' $storageAccountName
    }
}

$package = (Get-Location).Path + "\Package.zip"

Publish-AzWebapp -ResourceGroupName $resourceGroup `
-Name $functionAppName `
-ArchivePath $package `
-Force

$TriggerName = "Webhook"
$QueueName='notifications'

$FunctionApp = Get-AzWebApp -ResourceGroupName $resourceGroup -Name $functionAppName
$Id = $FunctionApp.Id
$DefaultHostName = $FunctionApp.DefaultHostName
$FunctionKey = (Invoke-AzResourceAction -ResourceId "$Id/functions/$TriggerName" -Action listkeys -Force).default
$FunctionURL = "https://" + $DefaultHostName + "/api/" + $TriggerName + "?code=" + $FunctionKey

Write-Output $nl'Azure Function Url: ' $FunctionURL

Write-Output $nl 'Azure Storage Account: ' $storageAccountName
#Write-Output $nl 'Azure Queue Name: ' $QueueName
Write-Output $nl 'Azure Storage Access Key: '

(Get-AzStorageAccountKey -ResourceGroupName $resourceGroup -AccountName $storageAccountName)| Where-Object {$_.KeyName -eq "key1"}
