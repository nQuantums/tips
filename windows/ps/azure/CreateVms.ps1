# 参考元： https://docs.microsoft.com/ja-jp/azure/virtual-machines/scripts/virtual-machines-windows-powershell-sample-create-vm-from-snapshot


Connect-AzAccount

$subscriptionId = ''

#Set the context to the subscription Id where Managed Disk will be created
Select-AzSubscription -SubscriptionId $SubscriptionId


function CreateVmFromSnapshot($resourceGroupName, $snapshotName, $resourceGroupNameDest, $vmName, $virtualMachineSize, $vnet, $privateIpAddress) {
	'スナップショット(' + $resourceGroupName + ' の ' + $snapshotName + ')からVM(' + $resourceGroupNameDest + ' の ' + $vmName + ')を作成します'
	$osDiskName = $vmName + '_disk'

	'スナップショット取得: ' + $snapshotName
	$snapshot = Get-AzSnapshot -ResourceGroupName $resourceGroupName -SnapshotName $snapshotName

	'スナップショットから管理ディスク作成: ' + $snapshotName
	$diskConfig = New-AzDiskConfig -SkuName 'Premium_LRS' -Location $snapshot.Location -SourceResourceId $snapshot.Id -CreateOption Copy
	$disk = New-AzDisk -Disk $diskConfig -ResourceGroupName $resourceGroupNameDest -DiskName $osDiskName

	'VMオブジェクト作成: ' + $vmName + ' ' + $virtualMachineSize
	$VirtualMachine = New-AzVMConfig -VMName $vmName -VMSize $virtualMachineSize

	'VMにOSディスク設定: ' + $disk.Id
	$VirtualMachine = Set-AzVMOSDisk -VM $VirtualMachine -ManagedDiskId $disk.Id -CreateOption Attach -Windows

	'パブリックIP作成'
	$publicIp = New-AzPublicIpAddress -Name ($vmName.ToLower() + '_ip') -ResourceGroupName $resourceGroupNameDest -Location $snapshot.Location -AllocationMethod Dynamic

	'IPアドレス固定化したいので自分で指定する: ' + $privateIpAddress
	$subnet = Get-AzVirtualNetworkSubnetConfig -Name 'default' -VirtualNetwork $vnet
	$ipConfName = $vmName + '_ipconf'
	$ipConfig = New-AzNetworkInterfaceIpConfig -Name $ipConfName -PrivateIpAddress $privateIpAddress -Primary -SubnetId $subnet.Id -PublicIpAddressId $publicIp.Id

	'NIC作成'
	$nic = New-AzNetworkInterface -Name ($vmName.ToLower() + '_nic') -ResourceGroupName $resourceGroupNameDest -Location $snapshot.Location -IpConfiguration $ipConfig

	'VMにネットワークカード設定'
	$VirtualMachine = Add-AzVMNetworkInterface -VM $VirtualMachine -Id $nic.Id

	'VMをデプロイ: ' + $vmName
	New-AzVM -VM $VirtualMachine -ResourceGroupName $resourceGroupNameDest -Location $snapshot.Location
}


$masterResourceGroupName = 'Master' # マスターデータが置かれているリソースグループ名
$destResourceGroupName = 'temp' # VM作成先リソースグループ名

#Get the virtual network where virtual machine will be hosted
$vnet = Get-AzVirtualNetwork -Name 'vnet' -ResourceGroupName $masterResourceGroupName

for ($i = 1; $i -lt 2; $i++) {
	$serverName = 'sv' + ($i + 1)
	$clientName = 'cl' + ($i + 1)
	$serverPrivateIpAddress = '10.0.0.' + (10 + $i)
	$clientPrivateIpAddress = '10.0.0.' + (110 + $i)
	CreateVmFromSnapshot $masterResourceGroupName 'sv1act-snapshot' $destResourceGroupName $serverName 'Standard_DS11_v2' $vnet $serverPrivateIpAddress
	# CreateVmFromSnapshot $masterResourceGroupName 'cl1-snapshot' $destResourceGroupName $clientName 'Standard_D4s_v3' $vnet $clientPrivateIpAddress
}
