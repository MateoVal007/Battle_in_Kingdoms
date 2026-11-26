param([ValidateSet('host','client')][string]$Role,[string]$Room='pueblo-codex-test')
$folder='C:\Users\MAQ\Documents\ChatGPT\Juego final\outputs\Pueblo_online'
$exe=Join-Path $folder 'Pueblo.exe'
$arguments=@('-batchmode','-nographics','-village-role',$Role,'-village-room',$Room,'-village-test','1','-village-report',('"'+$folder+'\'+$Role+'-result.txt"'),'-logFile',('"'+$folder+'\'+$Role+'.log"'))
$proc=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
$proc.Id | Set-Content (Join-Path $folder ($Role+'-pid.txt'))
Write-Output ('Started '+$Role+' PID '+$proc.Id)
