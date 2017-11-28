$successful='0'
while ($successful -eq '0')
{
    & git clean -dxf .\src\ .\bin\TestDebug\
    & .\bin\Debug\bin\xabuild.exe .\src\Mono.Android\Test\Mono.Android-Tests.csproj /t:SignAndroidPackage /bl /noconsolelogger
    $successful = $lastExitCode
}