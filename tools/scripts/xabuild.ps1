param (
    [Parameter(Mandatory=$true)][string]$project,
    [Parameter(Mandatory=$true)][string]$target,
    [string]$configuration = "Debug"
)

$prefix = "$PSScriptRoot\..\..\bin\$configuration"
$xa_prefix = "$prefix\lib\xamarin.android"

$Paths_targets = "$PSScriptRoot\..\..\build-tools\scripts\Paths.targets"
$ANDROID_NDK_PATH = (msbuild /nologo /v:minimal /t:GetAndroidNdkFullPath $Paths_targets).Trim()
$ANDROID_SDK_PATH = (msbuild /nologo /v:minimal /t:GetAndroidSdkFullPath $Paths_targets).Trim()

$XA_BUILD_FLAGS = @(
    "/p:MonoAndroidToolsDirectory=""$xa_prefix\xbuild\Xamarin\Android""",
    "/p:MonoDroidInstallDirectory=""$prefix""",
    "/p:AndroidNdkDirectory=""$ANDROID_NDK_PATH""",
    "/p:AndroidSdkDirectory=""$ANDROID_SDK_PATH""",
    "/p:TargetFrameworkRootPath=""$xa_prefix\xbuild-frameworks""",
    "/p:FrameworkPathOverride=""$xa_prefix\xbuild-frameworks\MonoAndroid\v1.0"""
)

msbuild $project $XA_BUILD_FLAGS /t:$target