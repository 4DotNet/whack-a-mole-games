param containerAppPrincipalId string
param developersGroup string
param integrationResourceGroupName string

resource storageAccountDataContributorRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup()
  name: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
}
module storageAccountDataReaderRoleAssignment 'roleAssignment.bicep' = {
  name: 'storageAccountDataReaderRoleAssignmentModule'
  scope: resourceGroup()
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: storageAccountDataContributorRole.id
  }
}
module storageAccountDataReaderRoleAssignmentForDevelopers 'roleAssignment.bicep' = {
  name: 'storageAccountDataReaderRoleAssignmentForDevelopersModule'
  scope: resourceGroup()
  params: {
    principalId: developersGroup
    roleDefinitionId: storageAccountDataContributorRole.id
    principalType: 'Group'
  }
}

resource configurationDataReaderRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup(integrationResourceGroupName)
  name: '516239f1-63e1-4d78-a4de-a74fb236a071'
}
module configurationReaderRoleAssignment 'roleAssignment.bicep' = {
  name: 'configurationReaderRoleAssignmentModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: configurationDataReaderRole.id
  }
}
