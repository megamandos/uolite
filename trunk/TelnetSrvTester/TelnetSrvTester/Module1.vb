Module Module1
    Public WithEvents Svr As TelnetSrv.TelnetSrv

    Sub Main()
        Svr = New TelnetSrv.TelnetSrv
        Svr.Start()
        While True
            'Sleep forever.
            Threading.Thread.Sleep(2000000000)
        End While
    End Sub

    Private Sub Svr_onClientConnect(ByRef Client As TelnetSrv.TelnetSrv.Client) Handles Svr.onClientConnect
        Console.WriteLine("Client: " & Client.UID & " Connected.")
    End Sub

    Private Sub Svr_onClientDisconnection(ByRef Client As TelnetSrv.TelnetSrv.Client) Handles Svr.onClientDisconnection
        Console.WriteLine("Client: " & Client.UID & " Disconnected.")
    End Sub

    Private Sub Svr_onCommandReceive(ByRef Client As TelnetSrv.TelnetSrv.Client, ByRef CommandLine As String) Handles Svr.onCommandReceive
        Console.WriteLine("Client: " & Client.UID & " Sent: " & CommandLine)
        'Client.Beep()
        If CommandLine.Length >= 1 Then
            Dim CommandChunks() As String = CommandLine.Split(" ")

            Select Case LCase(CommandChunks(0))
                Case "prompt"
                    If CommandChunks.Length > 1 Then
                        Client.Prompt = CommandLine.Substring(CommandChunks(0).Length + 1) & ">"
                    End If
                Case "broadcast"
                    Svr.Broadcast(CommandLine.Substring(CommandChunks(0).Length + 1))
                Case "kickall"
                    Svr.KickAll()
                Case "exit"
                    Client.Kick()

            End Select
        End If
    End Sub


End Module
