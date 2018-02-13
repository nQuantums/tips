# WSUS API のメモ

- 更新一覧の取得
	- IUpdateServer.GetUpdates()
	- [How to Determine Status for all Updates](https://msdn.microsoft.com/en-us/library/windows/desktop/ee855020(v=vs.85).aspx)

- 更新のIDの取得
	- [IUpdate.Id Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.id(v=vs.85).aspx)

- 更新タイトルの取得
	- [IUpdate.Title Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.title(v=vs.85).aspx)

- 更新のインストール中にネットワーク接続が必要かどうか
	- [IUpdate.InstallationBehavior Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.installationbehavior(v=vs.85).aspx)

- 更新がドライバかソフトウェアか判定
	- [IUpdate.UpdateType Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.updatetype(v=vs.85).aspx)

- 更新が置き換えられたかどうか判定
	- [IUpdate.IsSuperseded Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.issuperseded(v=vs.85).aspx)

- 更新が最新のリビジョンかどうか判定
	- [IUpdate.IsLatestRevision Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.islatestrevision(v=vs.85).aspx)

- 更新の対象製品ファミリの取得
	- [IUpdate.ProductFamilyTitles Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.productfamilytitles(v=vs.85).aspx)

- 更新の対象プロダクトの取得
	- [IUpdate.ProductTitles Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.producttitles(v=vs.85).aspx)

- 更新の公開日時の取得
	- [IUpdate.CreationDate Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.creationdate(v=vs.85).aspx)

- 更新の公開状態の取得
	- [IUpdate.PublicationState Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.publicationstate(v=vs.85).aspx)

- 更新の区分取得
	- [IUpdate.UpdateClassificationTitle Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.updateclassificationtitle(v=vs.85).aspx)

- KB番号の取得
	- [IUpdate.KnowledgebaseArticles Property](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.knowledgebasearticles(v=vs.85).aspx)

- ダウンロードURLの取得
	- [IUpdate.GetInstallableItems Method ()](https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.updateservices.administration.iupdate.getinstallableitems(v=vs.85).aspx)
