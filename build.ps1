$config = 'Debug'
$nuspec_file = 'ReSharper.HeapView.nuspec'
$package_id = 'ReSharper.HeapView'

nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R90\**' -Exclude 'ReSharper.HeapView\bin.R91\**' -Properties "Configuration=$config;ReSharperDep=ReSharper;ReSharperVer=8.1;PackageId=$package_id"

# nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R8*\**' -Exclude 'ReSharper.HeapView\bin.R91\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[1.0,2.0);PackageId=$package_id.R90"

nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R8*\**' -Exclude 'ReSharper.HeapView\bin.R90\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[2.0,3.0);PackageId=$package_id.R90"