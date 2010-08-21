Imports System, System.Net, System.Net.Sockets

'Causes received bytes to be printed to the console.
#Const DebugReceive = True

Partial Class TelnetSrv

    Public Class Client
        Private _Stream As NetworkStream
        Private _TelnetServer As TelnetSrv
        Private _UID As UInt64
        Private _RemoteIP As IPAddress
        Private _RemotePort As UShort
        Private _Client As TcpClient
        Private _ClientList As ClientList
        Public Property Prompt As String
        Public CmdBuffer As New System.Text.StringBuilder
        Private _Closed As Boolean = False
        Private ReceivingCommands As Boolean = False

        Friend Sub New(ByRef Client As TcpClient, ByRef ClientList As ClientList)
            _TelnetServer = ClientList.Server
            _Prompt = ClientList.Server.Prompt
            _Stream = Client.GetStream
            _Client = Client
            _ClientList = ClientList
            _RemoteIP = DirectCast(Client.Client.RemoteEndPoint, System.Net.IPEndPoint).Address
            _RemotePort = DirectCast(Client.Client.RemoteEndPoint, System.Net.IPEndPoint).Port
            _UID = (_RemoteIP.GetAddressBytes(0) * 1099511627776) + (_RemoteIP.GetAddressBytes(1) * 4294967296) + (_RemoteIP.GetAddressBytes(2) * 16777216) + (_RemoteIP.GetAddressBytes(3) * 65536) + _RemotePort

            'Begin Asynchronous Data Reception.
            Dim RecBuffer(4096) As Byte
            _Stream.BeginRead(RecBuffer, 0, RecBuffer.Length, AddressOf Recieve, RecBuffer)

            'http://support.microsoft.com/kb/231866

            'Setup the configuration options.
            Dim packet1() As Byte = {&HFF, &HFD, &H27, &HFF, &HFA, &H27, &H1, &HFF, &HF0, &HFF, &HFD, &H25, &HFF, &HFA, &H25, &H1, &H7, &H0, &HFF, &HF0,
                                     &HFF, &HFD, &H18, &HFF, &HFD, &H20, &HFF, &HFD, &H23, &HFF, &HFD, &H1F, &HFF, &HFA, &H27, &H1, &HFF, &HF0, &HFF,
                                     &HFA, &H18, &H1, &HFF, &HF0, &HFF, &HFB, &H3, &HFF, &HFD, &H1, &HFF, &HFB, &H5, &HFF, &HFD, &H21, &HFF, &HFE, &H1,
                                     &HFF, &HFB, &H1}

            Send(packet1)

            Send(_TelnetServer.MOTD & Prompt)

        End Sub

        Public Sub Beep()
            Send(CByte(7))
        End Sub

        Private Sub Recieve(ByVal ar As IAsyncResult)

            '--Retreive array of bytes
            Dim bytes() As Byte = ar.AsyncState

            '--Get number of bytes received and also clean up resources that was used from beginReceive
            Dim numBytes As Int32 = _Stream.EndRead(ar)

            '--Did we receive anything?
            If numBytes > 0 Then

                '--Resize the array to match the number of bytes received. Also keep the current data
                ReDim Preserve bytes(numBytes - 1)

                Dim cmdstr As String = System.Text.ASCIIEncoding.ASCII.GetString(bytes)

                Select Case cmdstr
                    Case Chr(30) To Chr(126)
                        CmdBuffer.Append(Chr(bytes(0)))
                        _TelnetServer.onKeyReceiveSub(Me, bytes(0))
                        Send(bytes(0))

                    Case Chr(8)
                        If CmdBuffer.ToString.Length > 2 Then
                            CmdBuffer.Remove(CmdBuffer.Length - 1, 1)
                            Send({CByte(&H1B), CByte(&H5B), CByte(&H44), CByte(&H1B), CByte(&H5B), CByte(&H4B)})
                            _TelnetServer.onKeyReceiveSub(Me, bytes(0))
                        End If

                    Case Chr(13) & Chr(10)
                        _TelnetServer.onCommandReceiveSub(Me, CmdBuffer.ToString)
                        CmdBuffer.Clear()
                        Send(Chr(13) & Chr(10) & Prompt)

                    Case Else
#If DebugReceive Then
                        Console.WriteLine(BitConverter.ToString(bytes))
#End If
                End Select

#If DebugReceive Then
                Console.WriteLine(BitConverter.ToString(bytes))
#End If

            End If

            If _Closed Then
                _Stream.Flush()
                _Stream.Close()
                Exit Sub
            End If

            '--Are we stil conncted?
            If _Client.Connected = False Then
                'Do something about being disconnected?
            Else
                '--Yes, then resize bytes to packet size
                Dim recbytes(4096) As Byte

                '--Call BeginReceive again, catching any error
                Try
                    _Stream.BeginRead(recbytes, 0, recbytes.Length, AddressOf Recieve, recbytes)
                Catch ex As Exception
                    'Deal with the disconnect?
                End Try
            End If
        End Sub


        ''' <summary>Sends the bytes to the client.</summary>
        ''' <param name="Bytes">The array of bytes to send.</param>
        Public Overloads Sub Send(ByRef Bytes() As Byte)
            _Stream.Write(Bytes, 0, Bytes.Length)
        End Sub


        ''' <summary>
        ''' Sends a single byte to the client.
        ''' </summary>
        ''' <param name="Character">The ASCII character or byte to send.</param>
        Public Overloads Sub Send(ByRef Character As Byte)
            _Stream.WriteByte(Character)
        End Sub

        ''' <summary>
        ''' Sends the string of text to the client.
        ''' </summary>
        ''' <param name="Text">An ASCII string to send.</param>
        Public Overloads Sub Send(ByRef Text As String)
            Send(System.Text.ASCIIEncoding.ASCII.GetBytes(Text))
        End Sub

        Public Sub Kick()
            _ClientList.Remove(UID)
            _Closed = True
        End Sub

        ''' <summary>Returns the underlying network stream.</summary>
        Public ReadOnly Property Stream As NetworkStream
            Get
                Return _Stream
            End Get
        End Property

        ''' <summary>The UID used to keep track of the client. This is actualy a string of bytes made of the remote ip and port number.</summary>
        Public ReadOnly Property UID As String
            Get
                Return _UID
            End Get
        End Property

    End Class

    Public Class ClientList
        Private _Clients As New Hashtable
        Private _server As TelnetSrv

        Friend ReadOnly Property Server As TelnetSrv
            Get
                Return _server
            End Get
        End Property

        Friend Sub New(ByRef Server As TelnetSrv)
            _server = Server
        End Sub

        Public ReadOnly Property Client(ByVal UID As Long) As Client
            Get
                Return DirectCast(_Clients(UID), Client)
            End Get
        End Property

        Friend Sub Add(ByRef Client As Client)
            _Clients.Add(Client.UID, Client)
        End Sub

        Friend Overloads Sub Remove(ByRef Client As Client)
            Remove(Client.UID)
        End Sub

        Friend Overloads Sub Remove(ByRef UID As Long)
            _Clients.Remove(UID)
        End Sub

        Public ReadOnly Property Count As UInteger
            Get
                Return _Clients.Count
            End Get
        End Property

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Client)
            Return _Clients.OfType(Of Client).GetEnumerator
        End Function

        Public Function GetEnumerator1() As System.Collections.IEnumerator
            Return _Clients.GetEnumerator
        End Function

    End Class

End Class