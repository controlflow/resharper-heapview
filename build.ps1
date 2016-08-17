$config = 'Debug'
$nuspec_file = 'ReSharper.HeapView.nuspec'
$package_id = 'ReSharper.HeapView'

# 10.0
./tools/nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R2016.1\**;ReSharper.HeapView\bin.R2016.2\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[4.0,5.0);PackageId=$package_id.R100"

# 2016.1
./tools/nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R100\**;ReSharper.HeapView\bin.R2016.2\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[5.0,6.0);PackageId=$package_id.R2016.1"

# 2016.2
./tools/nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R100\**;ReSharper.HeapView\bin.R2016.1\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[6.0,7.0);PackageId=$package_id.R2016.2"