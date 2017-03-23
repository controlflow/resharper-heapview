$config = 'Debug'
$nuspec_file = 'ReSharper.HeapView.nuspec'
$package_id = 'ReSharper.HeapView'

# 2016.2
./tools/nuget pack $nuspec_file -Exclude 'bin\R2016.3\**;bin\R2017.1\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[6.0,7.0);PackageId=$package_id.R2016.2"

# 2016.3
./tools/nuget pack $nuspec_file -Exclude 'bin\R2016.2\**;bin\R2017.1\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[7.0,8.0);PackageId=$package_id.R2016.3"

# 2017.1
./tools/nuget pack $nuspec_file -Exclude 'bin\R2016.2\**;bin\R2016.3\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[8.0,9.0);PackageId=$package_id.R2017.1"