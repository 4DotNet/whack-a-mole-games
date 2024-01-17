using './main.bicep'

param systemName = 'wam-games-api'
param environmentName = 'tst'
param locationAbbreviation = 'ne'
param developersGroup = '0855534d-094e-40e7-9e19-d189578f69a9'
param integrationEnvironment = {
  resourceGroup: 'wam-int-tst-rg'
  containerAppsEnvironment: 'wam-int-tst-env'
  appConfiguration: 'wam-int-tst-appcfg'
}
param acrLoginServer = ''
param acrPassword = ''
param acrUsername = ''
