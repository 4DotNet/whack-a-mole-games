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

resource webPubSubServiceOwnerRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: resourceGroup(integrationResourceGroupName)
  name: '8cf5e20a-e4b2-4e9d-b3a1-5ceb692c2761'
}
module webPubSubServiceOwnerRoleAssignment 'roleAssignment.bicep' = {
  name: 'webPubSubServiceOwnerRoleAssignmentModule'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: containerAppPrincipalId
    roleDefinitionId: webPubSubServiceOwnerRoleDefinition.id
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
