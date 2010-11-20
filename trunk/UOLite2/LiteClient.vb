'This is the base class, with most of the events being in this file, along with commonly used attributes and methods.

Imports System.Net, System.Net.Sockets, System.Text, System.IO, System.Net.NetworkInformation, Microsoft.Win32

Public Class LiteClient

#Region "Base Declarations"
    Private _EmulatedVersion() As Byte = {0, 0, 0, 7, 0, 0, 0, 0, 0, 0, 0, 8, 0, 0, 0, 2} 'Version 7.0.8.2

    Friend Shared StrLst As SupportClasses.CliLocList
    Private _LoginClient As TcpClient
    Private _LoginStream As NetworkStream
    Private _GameClient As New TcpClient
    Private _GameStream As NetworkStream
    Protected Friend Shared ContentPath As String
    Protected Friend _Mobiles As New MobileList(Me)
    Protected Friend _WaitingForTarget As Boolean
    Protected Friend _Encrypted As Boolean = False
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
    Public Event onCliLocSpeech(ByRef Client As LiteClient, ByVal Serial As Serial, ByVal BodyType As UShort, ByVal SpeechType As UOLite2.Enums.SpeechTypes, ByVal Hue As UShort, ByVal Font As UOLite2.Enums.Fonts, ByVal CliLocNumber As UInteger, ByVal Name As String, ByVal ArgsString As String)

    ''' <summary>Called when the client recieves a "text" or "Unicode Text" packet from the server.</summary>
    ''' <param name="Client">The client to which this applies.</param>
    ''' <param name="Serial">The serial of the mobile/item speaking. 0xFFFFFFFF for System</param>
    ''' <param name="BodyType">The bodytype/artwork of the mobile/item speaking. 0xFFFF for System</param>
    ''' <param name="SpeechType">The type of speech.</param>
    ''' <param name="Hue">The hue of the message.</param>
    ''' <param name="Font">The font of the message.</param>
    ''' <param name="Text">The text to be displayed.</param>
    ''' <param name="Name">The name of the speaker. "SYSTEM" for System.</param>
    Public Event onSpeech(ByRef Client As LiteClient, ByVal Serial As Serial, ByVal BodyType As UShort, ByVal SpeechType As UOLite2.Enums.SpeechTypes, ByVal Hue As UShort, ByVal Font As UOLite2.Enums.Fonts, ByVal Text As String, ByVal Name As String)

    ''' <summary>Called when the server sends the list of characters.</summary>
    ''' <param name="Client">The client making the call</param>
    ''' <param name="CharacterList">The list of characters as <see cref="UOLite2.Structures.CharListEntry">CharacterListEntry</see>'s.</param>
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

    Private _GameServerList() As SupportClasses.GameServerInfo

    ''' <summary>
    ''' Called when the client receives the server list durring the login process.
    ''' </summary>
    Public Event onRecievedServerList(ByRef ServerList() As SupportClasses.GameServerInfo)

    ''' <summary>Called during the login process when the server rejects the username and password.</summary>
    ''' <param name="Reason">The reason for the failure.</param>
    Public Event onLoginDenied(ByRef Reason As String)

#End Region

#Region "Low Level stuff"

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

    Public ReadOnly Property ServerList As SupportClasses.GameServerInfo()
        Get
            If _GameServerList Is Nothing Then
                Throw New ApplicationException("The ServerList was accessed, but it hasn't been populated yet! This is a fatal exception!")
            End If

            Return _GameServerList
        End Get
    End Property

    Public ReadOnly Property Latency As Integer
        Get
            Dim pinger As New Ping
            Dim reply As PingReply
            reply = pinger.Send(_GameServerAddress)
            Return reply.RoundtripTime
        End Get
    End Property

    Private Sub RemoveObject(ByRef Serial As Serial)
        If Serial.Value >= 1073741824 Then
            Items.RemoveItem(Serial)
        Else
            _Mobiles.RemoveMobile(Serial)
        End If
    End Sub

    ''' <summary>Creates a new client.</summary>
    ''' <param name="EnableOSIEncryption">Whether or not to use OSI encryption, for OSI servers.</param>
    ''' <param name="ContentFolderPath" >The path to the directory containing cliloc.enu without the "\" at the end.</param>
    Public Sub New(ByVal ContentFolderPath As String, Optional ByVal EnableOSIEncryption As Boolean = False)
        'TODO: implement localization.

        ContentPath = ContentFolderPath

        SetupErrorHandling()

        _Encrypted = EnableOSIEncryption

        _EncryptionLoginSeed = 3232235520 + CUInt(New Random(TimeOfDay.Millisecond).Next Mod 510) + 2

        If _Encrypted Then
            GenerateLoginKeys()
        End If

        StrLst = New SupportClasses.CliLocList(ContentFolderPath & "\cliloc.enu")
        _TileData = New SupportClasses.TileData(ContentFolderPath)
        _CurrentMap = New SupportClasses.Map(ContentFolderPath & "\map0.mul", "", "", 6144, 4096)

    End Sub

    Private Sub SetupErrorHandling()
#If Not Debug Then
        ' Get the your application's application domain.
        Dim currentDomain As AppDomain = AppDomain.CurrentDomain

        ' Define a handler for unhandled exceptions.
        AddHandler currentDomain.UnhandledException, AddressOf MYExnHandler
#End If
    End Sub

#End Region

#Region "Actions: walk/talk/etc..."

    ''' <summary>Causes the player to speak the specified text.</summary>
    ''' <param name="Text">The text to speak.</param>
    ''' <param name="Hue">The Hue of the text.</param>
    ''' <param name="Type">The type, (ie. Yell, Whisper, etc.)</param>
    ''' <param name="Font">The font.</param>
    Public Sub Speak(ByRef Text As String, Optional ByRef Hue As UOLite2.Enums.Common.Hues = UOLite2.Enums.Common.Hues.Yellow, Optional ByRef Type As UOLite2.Enums.SpeechTypes = UOLite2.Enums.SpeechTypes.Regular, Optional ByRef Font As UOLite2.Enums.Fonts = UOLite2.Enums.Fonts.Default)
        Dim packets As New Packets.UnicodeSpeechPacket(Type, Hue, UOLite2.Enums.Fonts.Default, "ENU", Text)
        Send(packets)
    End Sub

#End Region


End Class


Namespace SupportClasses
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
End Namespace