#Register enrichment service
$ssa = Get-SPEnterpriseSearchServiceApplication
$config = New-SPEnterpriseSearchContentEnrichmentConfiguration
$config.Endpoint = "http://localhost:90/ContentProcessingEnrichmentService/ContentProcessingEnrichmentService.svc"
$config.InputProperties = "Author", "Path"
$config.OutputProperties = "Department"
Set-SPEnterpriseSearchContentEnrichmentConfiguration –SearchApplication $ssa –ContentEnrichmentConfiguration $config

#Deregister service
#Remove-SPEnterpriseSearchContentEnrichmentConfiguration –SearchApplication $ssa
