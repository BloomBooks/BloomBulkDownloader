call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsall.bat" x64

pushd .
MSbuild /p:BuildConfigurationID=xyz123 /p:WarningLevel=0 /target:Test /property:teamcity_build_checkoutDir=..\ /verbosity:detailed /property:teamcity_dotnet_nunitlauncher_msbuild_task="notthere" /property:BUILD_NUMBER="*.*.999.999" /property:Minor="1" /property:Platform="Any CPU"
popd
PAUSE