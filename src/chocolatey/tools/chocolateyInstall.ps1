$packageName = 'cs-script'
$url = 'https://github.com/oleg-shilo/cs-script/releases/download/v4.6.3.0/cs-script.win.v4.6.3.0.7z'

try {
  $installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

  $cheksum = '805649AA7C55E594022B16BD4ABAB809E571603600647F48AAC792B616469FFB'
  $checksumType = "sha256"

  function stop-server
  {
     param(
       $server,
       $port,
       $command
     )

    try {

        $client  = New-Object Net.Sockets.TcpClient($server, $port)
        $socketStream  = $client.GetStream()

        [Byte[]]$Buffer = [Text.Encoding]::ASCII.GetBytes($data)


        $socketStream.Write($Buffer, 0, $Buffer.Length)
        $socketStream.Flush()
    }
    catch{
    }
  }


  stop-server "localhost" "17001" "-exit" # prev release Roslyn compiling server requires "-exit"
  stop-server "localhost" "17001" "-stop" # starting from .NET 5 release CodeDom build server requires "-stop"
  stop-server "localhost" "17002" "-stop" # starting from .NET 5 release Roslyn build server requires "-stop"


  # Download and unpack a zip file
  Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -checksum $checksum -checksumType $checksumType

  Install-ChocolateyEnvironmentVariable 'CSSCRIPT_DIR' $installDir User
  Install-ChocolateyEnvironmentVariable 'CSSCRIPT_ROOT' $installDir User
  
} catch {
  throw $_.Exception
}
