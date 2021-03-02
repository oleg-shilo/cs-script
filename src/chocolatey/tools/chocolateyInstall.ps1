$packageName = 'cs-script.core'
$url = 'https://github.com/oleg-shilo/cs-script.core/releases/download/v2.0.0.0/cs-script.win.v2.0.0.0.7z'

try {
  $installDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

  $cheksum = 'FE7079CC71AEA4CDD84090942DDCD0364C6A560A95620E0CF3494F2EB9A640F1'
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

  # Download and unpack a zip file
  Install-ChocolateyZipPackage "$packageName" "$url" "$installDir" -checksum $checksum -checksumType $checksumType

  Install-ChocolateyEnvironmentVariable 'CSSCRIPT_DIR' $installDir User
  Install-ChocolateyEnvironmentVariable 'CSSCRIPT_ROOT' $installDir User
  
} catch {
  throw $_.Exception
}
