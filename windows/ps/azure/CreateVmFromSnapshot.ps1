# 参考元： https://docs.microsoft.com/ja-jp/azure/virtual-machines/scripts/virtual-machines-windows-powershell-sample-create-vm-from-snapshot

#Provide the subscription Id
$subscriptionId = '54e69b76-27f2-4210-bded-e355384847a2'

#Provide the name of your resource group
$resourceGroupName ='test1'

#Provide the name of your resource group
$resourceGroupNameTemp ='temp'

#Provide the name of the snapshot that will be used to create OS disk
$snapshotName = 'ws2016-snapshot'

#Provide the name of the OS disk that will be created using the snapshot
$osDiskName = 'ws2016-disk'

#Provide the name of an existing virtual network where virtual machine will be created
$virtualNetworkName = 'vnet'

#Provide the name of the virtual machine
$virtualMachineName = 'ws2016'

#Provide the size of the virtual machine
#e.g. Standard_DS3
#Get all the vm sizes in a region using below script:
#e.g. Get-AzureRmVMSize -Location westus
$virtualMachineSize = 'Standard_D4s_v3'

#Set the context to the subscription Id where Managed Disk will be created
Select-AzureRmSubscription -SubscriptionId $SubscriptionId

$snapshot = Get-AzureRmSnapshot -ResourceGroupName $resourceGroupName -SnapshotName $snapshotName

$diskConfig = New-AzureRmDiskConfig -Location $snapshot.Location -SourceResourceId $snapshot.Id -CreateOption Copy

$disk = New-AzureRmDisk -Disk $diskConfig -ResourceGroupName $resourceGroupNameTemp -DiskName $osDiskName

#Initialize virtual machine configuration
$VirtualMachine = New-AzureRmVMConfig -VMName $virtualMachineName -VMSize $virtualMachineSize

#Use the Managed Disk Resource Id to attach it to the virtual machine. Please change the OS type to linux if OS disk has linux OS
$VirtualMachine = Set-AzureRmVMOSDisk -VM $VirtualMachine -ManagedDiskId $disk.Id -CreateOption Attach -Windows

#Create a public IP for the VM
$publicIp = New-AzureRmPublicIpAddress -Name ($VirtualMachineName.ToLower()+'_ip') -ResourceGroupName $resourceGroupNameTemp -Location $snapshot.Location -AllocationMethod Dynamic

#Get the virtual network where virtual machine will be hosted
$vnet = Get-AzureRmVirtualNetwork -Name $virtualNetworkName -ResourceGroupName $resourceGroupName

# Create NIC in the first subnet of the virtual network
$nic = New-AzureRmNetworkInterface -Name ($VirtualMachineName.ToLower()+'_nic') -ResourceGroupName $resourceGroupNameTemp -Location $snapshot.Location -SubnetId $vnet.Subnets[0].Id -PublicIpAddressId $publicIp.Id

$VirtualMachine = Add-AzureRmVMNetworkInterface -VM $VirtualMachine -Id $nic.Id

#Create the virtual machine with Managed Disk
New-AzureRmVM -VM $VirtualMachine -ResourceGroupName $resourceGroupNameTemp -Location $snapshot.Location
