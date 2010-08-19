Imports System, System.Net, System.Net.Sockets

Public Class TelnetSrv
    Protected Friend _Server As TcpListener
    Private _Clients As New ClientList(Me)

    Public Property Prompt As String = "TelnetServer>"
    Public Property MOTD As String = "TelnetSvr .Net Componenet" & vbNewLine & vbNewLine

    Public Event onKeyReceive(ByRef Client As Client, ByRef Key As System.ConsoleKey)
    Protected Friend Sub onKeyReceiveSub(ByRef Client As Client, ByRef Key As System.ConsoleKey)
        RaiseEvent onKeyReceive(Client, Key)
    End Sub

    Public Event onCommandReceive(ByRef Client As Client, ByRef CommandLine As String)
    Protected Friend Sub onCommandReceiveSub(ByRef Client As Client, ByRef CommandLine As String)
        RaiseEvent onCommandReceive(Client, CommandLine)
    End Sub

    Public Event onClientConnect(ByRef Client As Client)

    Public Event onClientDisconnection(ByRef Client As Client)
    Protected Friend Sub onClientDisconnectionSub(ByRef Client As Client)
        RaiseEvent onClientDisconnection(Client)
    End Sub

    Public Sub New(Optional ByRef LocalPort As UShort = 23)
        _Server = New TcpListener(IPAddress.Any, LocalPort)

    End Sub

    Public Sub Start()
        _Server.Start()
        _Server.BeginAcceptTcpClient(New AsyncCallback(AddressOf DoAcceptTcpClientCallback), _Server)
    End Sub

    Private Sub DoAcceptTcpClientCallback(ByVal ar As IAsyncResult)
        ' Get the listener that handles the client request.
        Dim listener As TcpListener = CType(ar.AsyncState, TcpListener)

        ' End the operation and display the received data on 
        ' the console.
        Dim client As New Client(listener.EndAcceptTcpClient(ar), _Clients)
        _Clients.Add(client)

        'AddHandler client.onKeyReceive, Me.onKeyReceive

        ' Process the connection here. (Add the client to a server table, read data, etc.)
        'Console.WriteLine("Client connected completed")
        RaiseEvent onClientConnect(client)
    End Sub



    Public Sub Broadcast(ByRef Message As String)
        For Each c As Client In _Clients
            c.Send(Message)
        Next
    End Sub

    Public Sub KickAll()
        For Each c As Client In _Clients
            c.Send("Goodbye")
            c.Kick()
        Next
    End Sub

End Class
