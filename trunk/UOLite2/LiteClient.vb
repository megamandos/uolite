'This is the base class, with most of the events being in this file, along with commonly used attributes and methods.

Imports System.Net, System.Net.Sockets, System.Text, System.IO, System.Net.NetworkInformation, Microsoft.Win32

Public Class LiteClient

#Region "Base Declarations"
    Private _EmulatedVersion() As Byte = {0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0, 13, 0, 0, 0, 0} 'Version 6.0.13.0

    Friend Shared StrLst As StringList
    Private _LoginClient As TcpClient
    Private _LoginStream As NetworkStream
    Private _GameClient As TcpClient
    Private _GameStream As NetworkStream
    Protected Friend Shared ClientPath As String
    'Protected Friend _AllItems As New Hashtable
    Protected Friend _Mobiles As New MobileList(Me)
    Protected Friend _WaitingForTarget As Boolean
    Protected Friend _Targeting As Boolean = False
    Protected Friend _TargetUID As UInteger
    Protected Friend _TargetType As Byte
    Protected Friend _TargetFlag As Byte

    Private Shared ProcessingPacket As Boolean = False

    Protected Friend _CharacterList As New ArrayList
    Public ReadOnly Property CharacterList As ArrayList
        Get
            Return _CharacterList
        End Get
    End Property

    Protected Friend WithEvents _Player As Mobile

#Region "Events"

    ''' <summary>
    ''' Called when the client recieves a login confirm packets from the game server, and the player character is created.
    ''' </summary>
    ''' <remarks></remarks>
    Public Event onLoginConfirm(ByRef Player As Mobile)

    ''' <summary> Called when an error occures within UOLite. It is important that you have something here to handle this.</summary>
    ''' <param name="Description">The description of the error and possibly extra details.</param>
    Public Event onError(ByRef Description As String)

    ''' <summary>Called when the clientis completely logged in, after all the items and everything loads completely.</summary>
    Public Event onLoginComplete()

    ''' <summary>
    ''' Called when a Packet is sent formt he client to the server.
    ''' </summary>
    ''' <param name="Client">Client from which the packet was sent.</param>
    ''' <param name="bytes">The sent packet.</param>
    Public Event onPacketSend(ByRef Client As LiteClient, ByRef bytes() As Byte)

    ''' <summary>
    ''' Called when the client loses its network connection to the server.
    ''' </summary>
    ''' <param name="Client">The client that lost its connection.</param>
    Public Event onConnectionLoss(ByRef Client As LiteClient)

    ''' <summary>
    ''' Called when a mobile is created and added to the mobile list.
    ''' </summary>
    ''' <param name="Client">The client to which this applies.</param>
    ''' <param name="Mobile">The new mobile.</param>
    Public Event onNewMobile(ByRef Client As LiteClient, ByVal Mobile As Mobile)

    ''' <summary>
    ''' Called after a new item is created and added to the item list.
    ''' </summary>
    ''' <param name="Client">The client to which this applies.</param>
    ''' <param name="Item">The new item.</param>
    Public Event onNewItem(ByRef Client As LiteClient, ByVal Item As Item)

    ''' <summary>
    ''' Called when the server sends the client a CliLoc speech packet. This is after the client processes the packet.
    ''' </summary>
    ''' <param name="Client">The client to which this applies.</param>
    ''' <param name="Serial">The serial of the mobile/item speaking. 0xFFFFFFFF for System</param>
    ''' <param name="BodyType">The bodytype/artwork of the mobile/item speaking. 0xFFFF for System</param>
    ''' <param name="SpeechType">The type of speech.</param>
    ''' <param name="Hue">The hue of the message.</param>
    ''' <param name="Font">The font of the message.</param>
    ''' <param name="CliLocNumber">The cliloc number.</param>
    ''' <param name="Name">The name of the speaker. "SYSTEM" for System.</param>
    ''' <param name="ArgsString">The arguements string, for formatting the speech. Each arguement is seperated by a "\t".</param>
    Public Event onCliLocSpeech(ByRef Client As LiteClient, ByVal Serial As Serial, ByVal BodyType As UShort, ByVal SpeechType As Enums.SpeechTypes, ByVal Hue As UShort, ByVal Font As Enums.Fonts, ByVal CliLocNumber As UInteger, ByVal Name As String, ByVal ArgsString As String)

    ''' <summary>Called when the client recieves a "text" or "Unicode Text" packet from the server.</summary>
    ''' <param name="Client">The client to which this applies.</param>
    ''' <param name="Serial">The serial of the mobile/item speaking. 0xFFFFFFFF for System</param>
    ''' <param name="BodyType">The bodytype/artwork of the mobile/item speaking. 0xFFFF for System</param>
    ''' <param name="SpeechType">The type of speech.</param>
    ''' <param name="Hue">The hue of the message.</param>
    ''' <param name="Font">The font of the message.</param>
    ''' <param name="Text">The text to be displayed.</param>
    ''' <param name="Name">The name of the speaker. "SYSTEM" for System.</param>
    Public Event onSpeech(ByRef Client As LiteClient, ByVal Serial As Serial, ByVal BodyType As UShort, ByVal SpeechType As Enums.SpeechTypes, ByVal Hue As UShort, ByVal Font As Enums.Fonts, ByVal Text As String, ByVal Name As String)

    ''' <summary>Called when the server sends the list of characters.</summary>
    ''' <param name="Client">The client making the call</param>
    ''' <param name="CharacterList">The list of characters as <see cref="CharListEntry">CharacterListEntry</see>'s.</param>
    Public Event onCharacterListReceive(ByRef Client As LiteClient, ByVal CharacterList As ArrayList)

#End Region

    Friend Sub NewItem(ByVal Item As Item)
        RaiseEvent onNewItem(Me, Item)
    End Sub

#Region "Properties"
    ''' <summary>The password used to connect to the server.</summary>
    Public Property Password As String

    ''' <summary>The username used to connect to the server.</summary>
    Public Property Username As String

    Private _LoginServerAddress As String
    ''' <summary>Returns the ip address that is used when connecting to the login server to get the game server list.</summary>
    Public ReadOnly Property LoginServerAddress As String
        Get
            Return _LoginServerAddress
        End Get
    End Property

    Private _LoginPort As UShort = 2593
    ''' <summary>Returns the port that is used when connecting to the login server to get the game server list.</summary>
    Public ReadOnly Property LoginPort As UShort
        Get
            Return _LoginPort
        End Get
    End Property

    Private _GameServerAddress As String
    ''' <summary>Returns the ip address of the game server that you are connected to.</summary>
    Public ReadOnly Property GameServerAddress As String
        Get
            Return _GameServerAddress
        End Get
    End Property

    Public ReadOnly Property Connected As Boolean
        Get
            If _LoginClient.Connected Then
                Return True
            ElseIf _GameClient.Connected Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Private _GameServerPort As UShort = 2595
    Public ReadOnly Property GameServerPort As UShort
        Get
            Return _GameServerPort
        End Get
    End Property

    Private _AccountUID As UInt32 = 0

    ''' <summary>
    ''' The account ID given to the client by the server at login.
    ''' </summary>
    Public ReadOnly Property AccountUID As UInt32
        Get
            Return _AccountUID
        End Get
    End Property

    ''' <summary>
    ''' The player.
    ''' </summary>
    Public ReadOnly Property Player() As Mobile
        Get
            Return _Player
        End Get
    End Property

    ''' <summary>
    ''' A list of the mobiles currently on the screen.
    ''' </summary>
    Public ReadOnly Property Mobiles() As MobileList
        Get
            Return _Mobiles
        End Get
    End Property

#End Region

    Private _GameServerList() As GameServerInfo

    ''' <summary>
    ''' Called when the client receives the server list durring the login process.
    ''' </summary>
    Public Event onRecievedServerList(ByRef ServerList() As GameServerInfo)

    ''' <summary>Called during the login process when the server rejects the username and password.</summary>
    ''' <param name="Reason">The reason for the failure.</param>
    Public Event onLoginDenied(ByRef Reason As String)

#End Region

#Region "Low Level Connection stuff"

#Region "Stupid BF-xx-xx-00-24 timer and emulation stuff"
    Private WithEvents BF24Ticker As New System.Timers.Timer(3800)
    Private BF24Packet() As Byte = {191, 0, 6, 0, 36, 38}

    Private Sub BF24Ticker_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles BF24Ticker.Elapsed
        If _GameClient.Connected = True Then
            Dim ran As New Random

            BF24Packet(5) = Math.Round(ran.Next(38, 131), 0)
            BF24Ticker.Interval = BF24Packet(5) * 100

            _GameStream.Write(BF24Packet, 0, 6)
        End If
    End Sub

    'dont know what this does... but the real client sends it.
    Private specialpacket() As Byte = {&H3, &H0, &H32, &H20, &H2, &HB2, &H0, &H3, &HDB, &H13, &H14, &H3F, &H45, &H2C, &H68, &H38, &H3, &H4D, &H39, &H47, &H54, &H9C, &H7B, &H9, &HB5, &H76, &H51, &HF7, &H3C, &H35, &HFE, &H7A, &H9B, &H94, &H73, &H64, &HF0, &HEC, &H61, &HB0, &HE5, &H59, &H7F, &H4F, &H3F, &H3C, &H13, &H46, &H1F, &H28}

#End Region

    '''<summary>Gets the working directory and location of client.exe from the registry.</summary>
    Private Sub InitializeClientPaths()
        Dim hklm As RegistryKey = Registry.LocalMachine
        Dim originkey As RegistryKey = hklm.OpenSubKey("SOFTWARE\Origin Worlds Online\Ultima Online\1.0")

        If originkey Is Nothing Then
            originkey = hklm.OpenSubKey("SOFTWARE\Origin Worlds Online\Ultima Online Third Dawn\1.0")
        End If

        If originkey IsNot Nothing Then
            Dim instcdpath As String = DirectCast(originkey.GetValue("InstCDPath"), String)
            If instcdpath IsNot Nothing Then
                ClientPath = instcdpath & "\"
                originkey.Close()
                Exit Sub
            End If
            originkey.Close()
        End If

        'use default values
        ClientPath = "C:\Program Files\EA Games\Ultima Online Mondain's Legacy\"

        Exit Sub
    End Sub

    Public ReadOnly Property ServerList As GameServerInfo()
        Get
            If _GameServerList Is Nothing Then
                Throw New ApplicationException("The ServerList was accessed, but it hasn't been populated yet! This is a fatal exception!")
            End If

            Return _GameServerList
        End Get
    End Property

    ''' Hide this class from the user, there is no reason from him/her to see it.
    ''' <summary>Simply a class to hold information about game servers when recieved from the login server.</summary>
    Public Class GameServerInfo

        Public Sub New(ByRef Name As String, ByRef Address As String, ByRef Load As Byte)
            _Name = Name
            _Address = Address
            _Load = Load
        End Sub

        ''' <summary>The IP address of the server.</summary>
        Private _Address As String
        Public ReadOnly Property Address As String
            Get
                Return _Address
            End Get
        End Property

        ''' <summary>The name of the server, as provided by the login server.</summary>
        Private _Name As String
        Public ReadOnly Property Name As String
            Get
                Return _Name
            End Get
        End Property

        Private _Load As Integer = 0
        Public ReadOnly Property Load As Integer
            Get
                Return _Load
            End Get
        End Property

        ''' <summary>The latency from the client to the server and back in milliseconds (ms). This is retrieved by sending a ping to the server when this property is called.</summary>
        Public ReadOnly Property Latency As Integer
            Get
                Dim pinger As New Ping
                Dim reply As PingReply
                reply = pinger.Send(_Address)
                Return reply.RoundtripTime
            End Get
        End Property

    End Class

    Public ReadOnly Property Latency As Integer
        Get
            Dim pinger As New Ping
            Dim reply As PingReply
            reply = pinger.Send(_GameServerAddress)
            Return reply.RoundtripTime
        End Get
    End Property

    ''' <summary>Connects to the specified login server and populates the ServerList property.</summary>
    ''' <param name="Address">The address of the login server to connect to.</param>
    ''' <param name="Port">The port to connect to (default is 2593).</param>
    ''' <param name="Username">The username to connect with.</param>
    ''' <param name="Password">The cooresponding password for the supplied username.</param>
    Public Overloads Function GetServerList(ByVal Username As String, ByVal Password As String, Optional ByVal Address As String = "login.uogamers.com", Optional ByVal Port As UShort = 2593) As String
        Try
            _LoginClient = New TcpClient()
            _LoginClient.ReceiveBufferSize = PacketSize
            _LoginClient.Connect(Address, Port)

        Catch ex As Exception
            Return "ERROR: Unable to connect to login server: " & ex.Message
        End Try

        _LoginStream = _LoginClient.GetStream

        'idk wtf this is for, but it seems to need it to make the server happy.
        Dim TZPacket() As Byte = {239}
        _LoginStream.Write(TZPacket, 0, TZPacket.Length)

        Dim LoginPacket(81) As Byte

        'Generate the encryption seed.
        Dim seed() As Byte = {192, 168, 1, 145}

        'add the seed to the login packet.
        InsertBytes(seed, LoginPacket, 0, 0, 4)

        'Add the version number.
        LoginPacket(4) = _EmulatedVersion(0)
        LoginPacket(5) = _EmulatedVersion(1)
        LoginPacket(6) = _EmulatedVersion(2)
        LoginPacket(7) = _EmulatedVersion(3)

        LoginPacket(8) = _EmulatedVersion(4)
        LoginPacket(9) = _EmulatedVersion(5)
        LoginPacket(10) = _EmulatedVersion(6)
        LoginPacket(11) = _EmulatedVersion(7)

        LoginPacket(12) = _EmulatedVersion(8)
        LoginPacket(13) = _EmulatedVersion(9)
        LoginPacket(14) = _EmulatedVersion(10)
        LoginPacket(15) = _EmulatedVersion(11)

        LoginPacket(16) = _EmulatedVersion(12)
        LoginPacket(17) = _EmulatedVersion(13)
        LoginPacket(18) = _EmulatedVersion(14)
        LoginPacket(19) = _EmulatedVersion(15)

        'Necessary...
        LoginPacket(20) = 128

        'Add the username.
        InsertBytes(GetBytesFromString(30, Username), LoginPacket, 0, 21, 30)
        _Username = Username

        'Add the password.
        InsertBytes(GetBytesFromString(30, Password), LoginPacket, 0, 51, 30)
        _Password = Password

        'Add umm 93?
        LoginPacket(81) = 93

        'Synchronouslysend the packet to the server.
        _LoginStream.Write(LoginPacket, 0, LoginPacket.Length)
        _LoginServerAddress = DirectCast(_LoginClient.Client.RemoteEndPoint, System.Net.IPEndPoint).Address.ToString

        'Set up asynchronous reading.
        Dim RecBuffer(PacketSize) As Byte
        _LoginStream.BeginRead(RecBuffer, 0, RecBuffer.Length, AddressOf LoginRecieve, RecBuffer)

        Return "SUCCESS"

    End Function

    Public Overloads Function ChooseServer(ByRef Index As Byte) As Boolean
        'If not connected then simply give up and return false.
        If Not Connected Then Return False

        Dim SelectBytes(2) As Byte
        SelectBytes(0) = 160
        SelectBytes(1) = 0
        SelectBytes(2) = Index

        _LoginStream.Write(SelectBytes, 0, SelectBytes.Length)

        Return True
    End Function

    Private Sub LoginRecieve(ByVal ar As IAsyncResult)
        '--Retreive array of bytes
        Dim bytes() As Byte = ar.AsyncState

        '--Get number of bytes received and also clean up resources that was used from beginReceive
        Dim numBytes As Int32 = _LoginStream.EndRead(ar)

        '--Did we receive anything?
        If numBytes > 0 Then
            '--Resize the array to match the number of bytes received. Also keep the current data
            ReDim Preserve bytes(numBytes - 1)

            Select Case bytes(0)
                Case 168 'Server List

                    'Populate and display the server list.
                    Dim i As Short = 0
                    Dim s As Integer = 0
                    Dim ShardCountBytes(1) As Byte
                    ShardCountBytes(0) = bytes(5)
                    ShardCountBytes(1) = bytes(4)
                    Dim ShardCount As Short = BitConverter.ToInt16(ShardCountBytes, 0)

                    Dim NameBytes(31) As Byte
                    Dim svrlist(ShardCount - 1) As GameServerInfo

                    For i = 0 To ShardCount - 1
                        'Get The Name
                        For s = i + 8 To i + 39
                            NameBytes(s - (i + 8)) = bytes((i * 40) + s)
                        Next

                        'Create the gameserverinfo object.
                        svrlist(i) = New GameServerInfo(System.Text.Encoding.ASCII.GetString(NameBytes).Replace(Chr(0), ""), bytes(i + 45) & "." & bytes(i + 44) & "." & bytes(i + 43) & "." & bytes(i + 42), bytes(i + 40))

                    Next

                    'copy the game server list into the GameServerList...
                    _GameServerList = svrlist

                    RaiseEvent onRecievedServerList(ServerList)

                Case 140

                    _GameServerAddress = bytes(1) & "." & bytes(2) & "." & bytes(3) & "." & bytes(4)

                    Dim GameServerPortBytes(1) As Byte
                    GameServerPortBytes(0) = bytes(6)
                    GameServerPortBytes(1) = bytes(5)

                    _GameServerPort = BitConverter.ToInt16(GameServerPortBytes, 0)

                    Dim AccountUIDBytes() As Byte = {bytes(10), bytes(9), bytes(8), bytes(7)}
                    _AccountUID = BitConverter.ToUInt32(AccountUIDBytes, 0)

                    Try
                        _GameClient = New TcpClient
                        _GameClient.ReceiveBufferSize = PacketSize
                        _GameClient.Connect(GameServerAddress, GameServerPort)
                        _GameStream = _GameClient.GetStream
                    Catch ex As Exception
                        RaiseEvent onError("Failed to connect to game server: " & ex.Message)
                    End Try

                    Dim EncBytes() As Byte = BitConverter.GetBytes(AccountUID)
                    Dim EncACK(3) As Byte
                    EncACK(0) = EncBytes(3)
                    EncACK(1) = EncBytes(2)
                    EncACK(2) = EncBytes(1)
                    EncACK(3) = EncBytes(0)
                    _GameStream.Write(EncACK, 0, EncACK.Length)

                    'As soon as it connects, send the username,password, and encryption key to request the character list.
                    Dim CharListReq(64) As Byte
                    Dim NameBytes() As Byte = GetBytesFromString(30, _Username)
                    Dim PassBytes() As Byte = GetBytesFromString(30, _Password)

                    CharListReq(0) = 145

                    'Add the Encryption Key (accountuid)
                    CharListReq(1) = EncBytes(3)
                    CharListReq(2) = EncBytes(2)
                    CharListReq(3) = EncBytes(1)
                    CharListReq(4) = EncBytes(0)

                    Dim i As Integer = 0

                    'Add the account name
                    For i = 5 To 34
                        CharListReq(i) = NameBytes(i - 5)
                    Next

                    'Add the password
                    For i = 35 To 64
                        CharListReq(i) = PassBytes(i - 35)
                    Next

                    _GameStream.Write(CharListReq, 0, CharListReq.Length)

                    'Set up asynchronous reading.
                    Dim RecBuffer(PacketSize) As Byte
                    _GameStream.BeginRead(RecBuffer, 0, RecBuffer.Length, AddressOf GameRecieve, RecBuffer)

                    'Close the connection to the server
                    _LoginClient.Close()
                    _LoginStream.Close()
                    Exit Sub

                Case 130
                    Select Case bytes(1)
                        Case 0
                            RaiseEvent onLoginDenied("Invalid Username/Password")
                        Case 1
                            RaiseEvent onLoginDenied("Someone is already using this account!")
                        Case 2
                            RaiseEvent onLoginDenied("Your account has been locked!")
                        Case 3
                            RaiseEvent onLoginDenied("Your account credentials are invalid!")
                        Case 4
                            RaiseEvent onLoginDenied("Commmunication Problem.")
                        Case 5
                            RaiseEvent onLoginDenied("The IGR concurrency limit has been met")
                        Case 6
                            RaiseEvent onLoginDenied("The IGR time limit has been met")
                        Case 7
                            RaiseEvent onLoginDenied("General IGR authentication failure")
                        Case Else
                            RaiseEvent onLoginDenied("Login Denied: Unknown Reason")
                    End Select
                Case Else
                    'Handle unknown packet?!?
                    '...
                    'na, just ignore it....

            End Select


        End If

        '--Are we stil conncted?
        If _LoginClient.Connected = False Then
            'Do something about being disconnected?

        Else
            '--Yes, then resize bytes to packet size
            ReDim bytes(PacketSize - 1)

            '--Call BeginReceive again, catching any error
            Try
                _LoginStream.BeginRead(bytes, 0, bytes.Length, AddressOf LoginRecieve, bytes)
            Catch ex As Exception
                'Deal with the disconnect?
            End Try
        End If
    End Sub

    Private Sub RemoveObject(ByRef Serial As Serial)
        If Serial.Value >= 1073741824 Then
            Items.RemoveItem(Serial)
        Else
            _Mobiles.RemoveMobile(Serial)
        End If
    End Sub

    Public Sub ChooseCharacter(ByRef CharacterName As String, ByRef Password As String, ByVal Slot As Byte)
        If Not _GameClient.Connected Then Exit Sub

        Dim packet(72) As Byte
        '0x5D Char Login Packet
        packet(0) = 93

        'Unknown 0xEDEDEDED
        packet(1) = 237
        packet(2) = 237
        packet(3) = 237
        packet(4) = 237

        Dim CharBytes() As Byte = GetBytesFromString(30, CharacterName)
        Dim PasswordBytes() As Byte = GetBytesFromString(30, Password)

        'Character Name
        For i As Integer = 5 To 34
            packet(i) = CharBytes(i - 5)
        Next

        'Character Password
        For i As Integer = 35 To 64
            packet(i) = PasswordBytes(i - 35)
        Next

        'Character Slot
        packet(65) = 0
        packet(66) = 0
        packet(67) = 0
        packet(68) = Slot

        'Dim EncBytes() As Byte = BitConverter.GetBytes(AccountUID)
        'User's encryption key
        packet(69) = 192
        packet(70) = 168
        packet(71) = 1
        packet(72) = 120

        'MsgBox(BitConverter.ToString(packet))
        Send(packet)

    End Sub

    Public Sub New()
        'TODO: implement localization.
        'InitializeClientPaths()
        'Localize()

#If Not Debug Then
        ' Get the your application's application domain.
        Dim currentDomain As AppDomain = AppDomain.CurrentDomain

        ' Define a handler for unhandled exceptions.
        AddHandler currentDomain.UnhandledException, AddressOf MYExnHandler
#End If

        ' Define a handler for unhandled exceptions for threads behind forms.
        'AddHandler currentDomain.ThreadException, AddressOf MYThreadHandler

    End Sub

    Private Sub Localize()
        'Sets the default language of the string list to the language of the OS
        'used for clilocs, item types, etc. That way people with non-english can get the
        'right info.
        'Languages: enu,chs,cht,deu,esp,fra,jpn,kor
        Select Case My.Application.Culture.ThreeLetterISOLanguageName
            Case "chi" 'Chinese
                StrLst = New StringList("chs")
            Case "zho" 'Chinese Traditional
                StrLst = New StringList("cht")

            Case "eng" 'English
                StrLst = New StringList("enu")
            Case "enm" 'English
                StrLst = New StringList("enu")

            Case "fre" 'French
                StrLst = New StringList("fra")
            Case "fra" 'French
                StrLst = New StringList("fra")
            Case "frm" 'French
                StrLst = New StringList("fra")
            Case "fro" 'French
                StrLst = New StringList("fra")

            Case "ger" 'German
                StrLst = New StringList("deu")
            Case "deu" 'German
                StrLst = New StringList("deu")
            Case "gmh" 'German
                StrLst = New StringList("deu")
            Case "goh" 'German
                StrLst = New StringList("deu")

            Case "spa" 'Spanish
                StrLst = New StringList("esp")

            Case "jpn" 'Japanese
                StrLst = New StringList("jpn")

            Case "kor" 'Korean
                StrLst = New StringList("kor")

            Case Else 'Don't know what to set it to? Then English.
                StrLst = New StringList("enu")

        End Select
    End Sub

#End Region

#Region "Actions: walk/talk/etc..."

    ''' <summary>Causes the player to speak the specified text.</summary>
    ''' <param name="Text">The text to speak.</param>
    ''' <param name="Hue">The Hue of the text.</param>
    ''' <param name="Type">The type, (ie. Yell, Whisper, etc.)</param>
    ''' <param name="Font">The font.</param>
    Public Sub Speak(ByRef Text As String, Optional ByRef Hue As Enums.Common.Hues = Enums.Common.Hues.Yellow, Optional ByRef Type As Enums.SpeechTypes = Enums.SpeechTypes.Regular, Optional ByRef Font As Enums.Fonts = Enums.Fonts.Default)
        Dim packet As New Packets.UnicodeSpeechPacket(Type, Hue, Enums.Fonts.Default, "ENU", Text)
        Send(packet)
    End Sub



#End Region


End Class
