$packageName = 'cs-script.core'

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

Write-Warning "REmoving 'css' shim $($env:ChocolateyInstall)\lib\cs-script.core\tools"

Uninstall-BinFile "css1" "$($env:ChocolateyInstall)\lib\cs-script.core\tools\cscs.exe"
