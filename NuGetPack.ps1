$nuget = "$PSScriptRoot\build\tools\NuGet.exe"
$ciaoProps = "$PSScriptRoot\Ciao.props"
$assembly = "SharpRaven.Log4Net"
$packagesConfig = "$PSScriptRoot\src\app\$assembly\packages.config"
$nuspec = "$PSScriptRoot\src\app\SharpRaven.Log4Net\$assembly.nuspec"
$tempDir = "$PSScriptRoot\NuGet_TEMP"
$releaseBuildDir = "$PSScriptRoot\src\app\$assembly\bin\Release"
$artifactsDir = "$PSScriptRoot\build\artifacts"

function CopyAttr($attributeName, $srcNode, $destNode) {
    $value = $srcNode.Attributes.GetNamedItem($attributeName).'#text'
    $destNode.SetAttribute($attributeName, $value)
}

function PrepareDependenciesNode ($nuspecXml) {
    $nuspecMetadataNode = $nuspecXml.SelectSingleNode("/package/metadata")
    $dependenciesNode = $nuspecXml.SelectSingleNode("/package/metadata/dependencies")
    if ($dependenciesNode) {
        $nuspecMetadataNode.RemoveChild($dependenciesNode) | Out-Null
    }
    $dependenciesNode = $nuspecXml.CreateElement("dependencies")
    $nuspecMetadataNode.AppendChild($dependenciesNode) | Out-Null
    return $dependenciesNode
}

function GetVersion($ciaoProps) {
    $propsXml = [xml](Get-Content $ciaoProps)
    [System.Xml.XmlNamespaceManager] $nsManager = $propsXml.NameTable
    $nsManager.AddNamespace("m", "http://schemas.microsoft.com/developer/msbuild/2003")
    $version = $propsXml.SelectSingleNode("/m:Project/m:PropertyGroup[@Label='Version']", $nsManager)
    $versionPrefix = $version.SelectSingleNode("m:VersionPrefix", $nsManager).InnerText
    $versionSuffix = $version.SelectSingleNode("m:VersionSuffix", $nsManager).InnerText
    return "$versionPrefix.$versionSuffix".TrimEnd(".")
}

function ExecNuget ($command) {
    Write-Output "NuGet.exe $command"
    Invoke-Expression "& `"$nuget`" $command"
}

function DeleteTempDir() {
    if (Get-Item -Path $tempDir -ErrorAction Ignore) {
        Write-Output "Removing temp dir"
        Remove-Item -Path $tempDir -Force -Recurse
    }
}

Write-Output "Copying dependency details from packages.config to nuspec"
$nuspecXml = [xml](Get-Content $nuspec)
$dependenciesNode = PrepareDependenciesNode -nuspecXml $nuspecXml

$packagesXml = [xml](Get-Content $packagesConfig)
foreach ($package in $packagesXml.SelectNodes("/packages/package")) {
    $dependencyNode = $nuspecXml.CreateElement("dependency")
    foreach($attribute in @("id", "version")) {
        CopyAttr -attributeName $attribute -srcNode $package -destNode $dependencyNode
    }
    $dependenciesNode.AppendChild($dependencyNode) | Out-Null
}
$nuspecXml.Save($nuspec)

Write-Output "Preparing package files in temp dir"
DeleteTempDir
$libDir = "$tempDir\lib\net40"
New-Item -Path $libDir -ItemType Directory -Force | Out-Null
Copy-Item -Path "$releaseBuildDir\$assembly.*" -Destination $libDir
Copy-Item -Path $nuspec -Destination $tempDir

# remove log4net-related config entries
$dllConfig = "$libDir\$assembly.dll.config"
$configXml = [xml](Get-Content $dllConfig)
$configurationNode = $configXml.SelectSingleNode("/configuration")
$configurationNode.RemoveChild($configXml.SelectSingleNode("/configuration/configSections")) | Out-Null
$configurationNode.RemoveChild($configXml.SelectSingleNode("/configuration/log4net")) | Out-Null
$configXml.Save($dllConfig)

$versionString = GetVersion -ciaoProps $ciaoProps

ExecNuget -command "Update -Self"
ExecNuget -command "Pack '$tempDir\$assembly.nuspec' -Version $versionString -OutputDirectory $artifactsDir"

DeleteTempDir