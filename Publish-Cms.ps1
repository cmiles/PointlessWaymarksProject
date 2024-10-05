$ErrorActionPreference = "Stop"
.\Publish-ProgramToInnoSetupInstaller.ps1 CmsGui
.\Publish-ProgramToInnoSetupInstaller.ps1 SiteViewerGui
.\Publish-ProgramToZip.ps1 CmsTask.GarminConnectGpxImport
.\Publish-ProgramToZip.ps1 CmsTask.MemoriesEmail
.\Publish-ProgramToZip.ps1 CmsTask.PhotoPickup
.\Publish-ProgramToZip.ps1 CmsTask.PublishSiteToS3
