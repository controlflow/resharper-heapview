$config = 'Debug'
$nuspec_file = 'ReSharper.HeapView.nuspec'
$package_id = 'ReSharper.HeapView'

function ZipFiles( $zipfilename, $sourcedir )
{
   Add-Type -Assembly System.IO.Compression.FileSystem
   $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
   [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcedir, $zipfilename, $compressionLevel, $false)
}

# 2016.2
#./tools/nuget pack $nuspec_file -Properties "Wave=Wave06;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[6.0,7.0);PackageId=$package_id.R2016.2"

# 2016.3
#./tools/nuget pack $nuspec_file -Properties "Wave=Wave07;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[7.0,8.0);PackageId=$package_id.R2016.3"

# 2017.1
#./tools/nuget pack $nuspec_file -Properties "Wave=Wave08;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[8.0,9.0);PackageId=$package_id.R2017.1"

# 2017.2
#./tools/nuget pack $nuspec_file -Properties "Wave=Wave09;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[9.0,10.0);PackageId=$package_id.R2017.2"

# Rider 2017.2
#./tools/nuget pack $nuspec_file -Properties "Wave=Wave10;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[10.0];PackageId=$package_id.Wave10"

# 2017.3
./tools/nuget pack $nuspec_file -Properties "Wave=Wave11;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[11.0,12.0);PackageId=$package_id.R2017.3"

# Rider 2017.3
./tools/nuget pack $nuspec_file -Properties "Wave=Wave11;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[11.0];PackageId=$package_id.Wave11"

# 2018.1
./tools/nuget pack $nuspec_file -Properties "Wave=Wave12;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[12.0];PackageId=$package_id.R2018.1"

# Rider 2018.1
./tools/nuget pack $nuspec_file -Properties "Wave=Wave12;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[12.0];PackageId=$package_id.Wave12"

# 2018.2
./tools/nuget pack $nuspec_file -Properties "Wave=Wave182;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[182.0];PackageId=$package_id.R2018.2"

# Rider 2018.2
./tools/nuget pack $nuspec_file -Properties "Wave=Wave182;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[182.0];PackageId=$package_id.Wave182"

# 2018.3
./tools/nuget pack $nuspec_file -Properties "Wave=Wave183;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[183.0];PackageId=$package_id.R2018.3"

# Rider 2018.3
./tools/nuget pack $nuspec_file -Properties "Wave=Wave183;Configuration=$config;ReSharperDep=Wave;ReSharperVer=[183.0];PackageId=$package_id.Wave183"

# Note that we only support one version of Rider
if (Test-Path ".\\rider-heapview.zip") { Remove-Item ".\\rider-heapview.zip" }
Get-ChildItem . -Include rider-heapview.zip | Remove-Item
Get-ChildItem .\Rider -Include *.nupkg -Recurse | Remove-Item
Copy-Item "$package_id.Wave182.*.nupkg" "Rider"

# TODO: Can't use Compress-Archive or ZipFiles, as it creates a zip file that Rider doesn't like
# Something to do with the way directories are created. If we open the file in 7-zip, the
# directories do not get a "D" attribute, and depending on the layout, Rider will either complain
# with "failed to load the plugin descriptor" from the META-INF folder, or it will look like it's
# installed, but on reload, the plugin isn't listed.
#
# Make sure to create a file with the following layout:
# rider-heapview.zip
#   -> rider-heapview
#     -> ReSharper.HeapView.Wave10.1.2.4.nupkg
#     -> META-INF
#       -> plugin.xml
#
# See IDEA-180829
#
#Compress-Archive -Path (Resolve-Path .\Rider\*) -DestinationPath rider-heapview.zip

#ZipFiles (Join-Path (Resolve-Path ".\\") "rider-heapview.zip") (Resolve-Path ".\\Rider")
