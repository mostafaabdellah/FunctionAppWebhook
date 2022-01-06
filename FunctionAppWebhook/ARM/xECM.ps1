#Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -AllowClobber -Force 
Connect-AzAccount
$resourceGroup='SharePointNotifications'

New-AzResourceGroup  -Name $resourceGroup -Location canadaCentral

$outputs=New-AzResourceGroupDeployment `
-Name $resourceGroup `
-ResourceGroupName $resourceGroup `
-TemplateFile '.\azureFunction.json'

foreach ($key in $outputs.Outputs.keys) {
    if ($key -eq "functionAppName") {
        $functionAppName = $outputs.Outputs[$key].value
        Write-Output 'functionAppName: ' $functionAppName
    }elseif ($key -eq "storageAccountName") {
        $storageAccountName = $outputs.Outputs[$key].value
        Write-Output 'storageAccountName: ' $storageAccountName
    }elseif ($key -eq "StorageAccountKey") {
        $storageAccountKey = $outputs.Outputs[$key].value
        Write-Output 'storageAccountKey: ' $storageAccountKey
    }
}
Publish-AzWebapp -ResourceGroupName $resourceGroup `
-Name $functionAppName `
-ArchivePath 'C:\Users\mmoustafa\Source\Repos\mostafaabdellah\FunctionAppWebhook\FunctionAppWebhook\ARM\Package.zip' `
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
$QueueName='webhooknotifications'

Write-Output 'Azure Storage Account Name: ' $storageAccountName
Write-Output 'Azure Queue Name: ' $QueueName
Write-Output 'Azure Queue Key: ' $queueKey

$QueueURL = "https://" + $storageAccountName + ".queue.core.windows.net/" + $QueueName + "/messages?" + $queueKey
Write-Output 'Azure Queue Url: ' $QueueURL

# $ctx = New-AzStorageContext -StorageAccountName $storageAccountName -UseConnectedAccount
# New-AzStorageContainerSASToken -Context $ctx `
#     -Name container1 `
#     -Permission racwdl `
#     -ExpiryTime $EndTime
