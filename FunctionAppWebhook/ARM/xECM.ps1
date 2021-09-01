#Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force
# Connect-AzAccount
$resourceGroup='xECM2-WaySync08'

New-AzResourceGroup  -Name $resourceGroup -Location canadaCentral

$outputs=New-AzResourceGroupDeployment `
-Name $resourceGroup `
-ResourceGroupName $resourceGroup `
-TemplateFile '.\azureFunction.json'

foreach ($key in $outputs.Outputs.keys) {
    if ($key -eq "functionAppName") {
        $functionAppName = $outputs.Outputs[$key].value
    }elseif ($key -eq "storageAccountName") {
        $storageAccountName = $outputs.Outputs[$key].value
    }elseif ($key -eq "StorageAccountKey") {
        $storageAccountKey = $outputs.Outputs[$key].value
    }
}
Publish-AzWebapp -ResourceGroupName $resourceGroup `
-Name $functionAppName `
-ArchivePath 'C:\Users\Moustafa\source\repos\FunctionAppWebhook\FunctionAppWebhook\bin\Release\netcoreapp3.1\publish\publish.zip' `
-Force

$TriggerName = "Notifications"
$FunctionApp = Get-AzWebApp -ResourceGroupName $resourceGroup -Name $functionAppName
$Id = $FunctionApp.Id
$DefaultHostName = $FunctionApp.DefaultHostName
$FunctionKey = (Invoke-AzResourceAction -ResourceId "$Id/functions/$TriggerName" -Action listkeys -Force).default
$FunctionURL = "https://" + $DefaultHostName + "/api/" + $TriggerName + "?code=" + $FunctionKey

Write-Output 'Azure Function Url: ' $FunctionURL

$StartTime = Get-Date
$EndTime = $startTime.AddDays(700)
$ctxKey = New-AzStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $storageAccountKey
$queueKey=New-AzStorageQueueSASToken -Name "xECM" -Permission raup -Context $ctxKey `
-ExpiryTime $EndTime

Write-Output 'Azure Queue Key: ' $queueKey

# $ctx = New-AzStorageContext -StorageAccountName $storageAccountName -UseConnectedAccount
# New-AzStorageContainerSASToken -Context $ctx `
#     -Name container1 `
#     -Permission racwdl `
#     -ExpiryTime $EndTime
