$config = 'Debug'
$nuspec_file = 'ReSharper.HeapView.nuspec'
$package_id = 'ReSharper.HeapView'

# 8.1; 8.2
# nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R90\**' -Exclude 'ReSharper.HeapView\bin.R91\**' -Exclude 'ReSharper.HeapView\bin.R92\**' -Properties "Configuration=$config;ReSharperDep=ReSharper;ReSharperVer=8.1;PackageId=$package_id"

# 9.1
# nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R8*\**' -Exclude 'ReSharper.HeapView\bin.R90\**' -Exclude 'ReSharper.HeapView\bin.R92\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[2.0,3.0);PackageId=$package_id.R90"

# 9.2
# nuget pack $nuspec_file -Exclude 'ReSharper.HeapView\bin.R8*\**' -Exclude 'ReSharper.HeapView\bin.R90\**' -Exclude 'ReSharper.HeapView\bin.R91\**' -Properties "Configuration=$config;ReSharperDep=Wave;ReSharperVer=[3.0,4.0);PackageId=$package_id.R90"