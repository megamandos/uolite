' This file contains the invidual packet classes, which contain the code to parse the different types of packets.

Imports System.IO
Imports System.Text

#Const PacketLogging = False
#Const DebugMobiles = False
#Const DebugItems = False
#Const DebugTargeting = False

Namespace Packets

    ''' <summary>The base packet class, inherited by all classes in UOAI.Packets</summary>
    Public Class Packet
        Friend _Data() As Byte
        Friend _Type As UOLite2.Enums.PacketType
        Friend _size As UShort
        Friend buff As UOLite2.SupportClasses.BufferHandler

        Public Sub New(ByVal Type As UOLite2.Enums.PacketType)
            _Type = Type
        End Sub

        ''' <summary>Returns the raw packet data as a byte array.</summary>
        Public Overridable ReadOnly Property Data() As Byte()
            Get
                If buff Is Nothing Then
                    Return _Data
                Else
                    _Data = buff.buffer
                    Return _Data
                End If
            End Get
        End Property

        Public ReadOnly Property Type() As UOLite2.Enums.PacketType
            Get
                Return _Type
            End Get
        End Property

        Public Overridable ReadOnly Property Size() As UShort
            Get
                Return Data.Length
            End Get
        End Property

    End Class

#Region "Text"
    ''' <summary>Clients send this packet when talking.</summary>
    ''' <remarks>Packet 0xAD</remarks>
    Public Class UnicodeSpeechPacket
        Inherits Packet

        Private _mode As UOLite2.Enums.SpeechTypes
        Private _hue As UShort
        Private _font As UOLite2.Enums.Fonts
        Private _lang As String
        Private _text As String
        Private _Skip12BitBytes As UShort = 0

        Friend Sub New(ByVal SpeechType As UOLite2.Enums.SpeechTypes, ByVal Hue As UShort, ByVal Font As UOLite2.Enums.Fonts, ByVal Language As String, ByVal Text As String)
            MyBase.New(UOLite2.Enums.PacketType.SpeechUnicode)
            Dim bytes(13 + (Text.Length * 2)) As Byte
            bytes(0) = 173 '0xAD

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1

                .networkorder = True
                .writeushort(bytes.Length)
                '.networkorder = False

                .writebyte(SpeechType)
                .writeushort(Hue)
                .writeushort(Font)
                .writestrn(UCase(Language), 4)
                .writeustr(Text)
            End With
        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.SpeechUnicode)

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            _Type = UOLite2.Enums.PacketType.SpeechUnicode

            buff.Position = 1

            buff.networkorder = False
            '1-2
            _size = buff.readushort
            buff.networkorder = True

            '3
            _mode = buff.readbyte

            '4-5
            _hue = buff.readushort

            '6-7
            _font = buff.readushort

            '8-11
            _lang = buff.readstr

            buff.Position = 12

            If _mode >= 192 Then
                _mode -= 192

                Dim result As UInteger = 0
                Dim ushrt1 As UShort = 0
                Dim ushrt2 As UShort = 0

                'God damned mother fucking 12-bit shorts!!! why the fuck did they use 12-bit shorts??!?!?

                'Move these 8 bits left by 4 bits (11111111 turns into 111111110000)
                ushrt1 = buff.readbyte * 16

                'move these 8 bits right by 4 bits, cutting off and ignoring the right 4. (11110101 turns into 1111)
                ushrt2 = buff.readbyte \ 16

                'Add the together. (combine 111111110000 + 1111 = 111111111111)
                result = ushrt1 + ushrt2

                result += 1 'Accounts for the first 12 bit short we just read)

                'Checks if the result is even or odd
                If (result Mod 2 = 0) Then
                    'The result is even
                    _Skip12BitBytes = (result / 2) * 3
                Else
                    'The result is odd
                    _Skip12BitBytes = (result \ 2) * 3
                    _Skip12BitBytes += 1
                End If

                buff.Position = 12 + _Skip12BitBytes

                '(12 + _Skip12BitBytes)-(Size - 1)
                _text = buff.readstr
            Else
                '12-(Size - 1)
                _text = buff.readustr
            End If

        End Sub

        ''' <summary>Gets or Sets the speech type as <see cref="UOLite2.Enums.SpeechTypes"/>.</summary>
        Public Property SpeechType() As UOLite2.Enums.SpeechTypes
            Get
                Return _mode
            End Get
            Set(ByVal value As UOLite2.Enums.SpeechTypes)
                _mode = value
                buff.Position = 3
                buff.writebyte(_mode)
            End Set
        End Property

        ''' <summary>Gets or Sets the hue of the text.</summary>
        Public Property Hue() As UShort
            Get
                Return _hue
            End Get
            Set(ByVal value As UShort)
                _hue = value
                buff.Position = 4
                buff.writeushort(value)
            End Set
        End Property

        ''' <summary>Gets or Sets the font of the text.</summary>
        Public Property Font() As UOLite2.Enums.Fonts
            Get
                Return _font
            End Get
            Set(ByVal value As UOLite2.Enums.Fonts)
                _hue = value
                buff.Position = 6
                buff.writeushort(value)
            End Set
        End Property

        ''' <summary>Gets or Sets the language key of the packet. This only effects how it is interpreted, it does NOT change the actual language.</summary>
        Public Property Language() As String
            Get
                Return _lang
            End Get
            Set(ByVal value As String)
                If value.Length <= 4 Then
                    _lang = value
                    buff.Position = 8
                    buff.writestrn(_lang, 4)
                Else
                    Throw New ConstraintException("Language string must be 4 characters or less!")
                End If
            End Set
        End Property

        ''' <summary>Gets or Sets the text that will be displayed. Will not allow a value longer than the current one.</summary>
        Public Property Text() As String
            Get
                Return _text
            End Get
            Set(ByVal value As String)
                If value.Length <= _text.Length Then
                    buff.Position = 12
                    buff.writeustrn(value, _text.Length)
                    _text = value
                Else
                    Throw New ConstraintException("You cannot set the text value to a value longer than its current one.")
                End If
            End Set
        End Property

    End Class

    ''' <summary>This is sent from the server to tell the client that someone is talking.</summary>
    ''' <remarks>Packet 0xAE</remarks>
    Public Class UnicodeText
        Inherits Packet
        Private _text As String = ""
        Private _Mode As UOLite2.Enums.SpeechTypes
        Private _hue As UShort
        Private _font As UOLite2.Enums.Fonts
        Private _body As UShort
        Private _Serial As Serial
        Private _lang As String
        Private _name As String = ""
        Private Created As Boolean = False

        Friend Sub New(ByVal Text As String)
            MyBase.New(UOLite2.Enums.PacketType.TextUnicode)
            'Dim txtbytes() As Byte = System.Text.Encoding.Unicode.GetBytes(Text)
            Dim bytes(52 + (Text.Length * 2)) As Byte
            bytes(0) = 174
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            With buff
                .Position = 1
                .networkorder = False
                .writeushort(bytes.Length)
                .networkorder = True
                .Position = 48
                .writeustr(Text)
            End With

        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.TextUnicode)
            _Data = bytes
            _size = bytes.Length

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            'Parse the data into the fields.
            _Data = bytes

            '0
            _Type = bytes(0)

            buff.Position = 1
            buff.networkorder = False

            '1-2
            _size = buff.readushort()
            buff.networkorder = True

            '3-6
            _Serial = buff.readuint

            '7-8
            _body = buff.readushort

            '9
            _Mode = buff.readbyte

            '10-11
            _hue = buff.readushort

            '12-13
            _font = buff.readushort

            '14-17
            _lang = buff.readstr

            buff.Position = 18

            '18-48
            _name = buff.readstr

            buff.Position = 48

            '49-(Size - 1)
            _text = buff.readustr

#Const DebugUnicodeText = False

#If DebugUnicodeText Then
                Console.WriteLine("-Unicode Text 0xAE: ")
                Console.WriteLine(" Serial: " & Serial.ToString)
                Console.WriteLine(" Body: " & Body)
                Console.WriteLine(" Name: " & Name)
                Console.WriteLine(" SpeechType: " & Mode.ToString)
                Console.WriteLine(" Hue: " & Hue)
                Console.WriteLine(" Font: " & Font.ToString)
                Console.WriteLine(" Text: " & Text)
                Console.WriteLine(" Language: " & Language)
                Console.WriteLine("")
#End If

        End Sub

        ''' <summary>Gets or Sets the text that will be displayed. Will not allow a value longer than the current one.</summary>
        Public Property Text() As String
            Get
                Return _text
            End Get
            Set(ByVal value As String)
                If value.Length <= _text.Length Then
                    buff.Position = 48
                    buff.writeustrn(value, _text.Length)
                    _text = value
                Else
                    Throw New ConstraintException("You cannot set the text value to a value longer than its current one.")
                End If
            End Set
        End Property

        ''' <summary>Gets or Sets the name of the speaker. Maximum of 30 characters.</summary>
        Public Property Name() As String
            Get
                Return _name
            End Get
            Set(ByVal value As String)
                If value.Length <= 30 Then
                    _name = value
                    buff.Position = 18
                    buff.writestrn(value, 30)
                Else
                    Throw New ApplicationException("Name is too long, much be 30 characters or less.")
                End If
            End Set
        End Property

        ''' <summary>Gets or Sets the speech type as <see cref="UOLite2.Enums.SpeechTypes"/>.</summary>
        Public Property Mode() As UOLite2.Enums.SpeechTypes
            Get
                Return _Mode
            End Get
            Set(ByVal value As UOLite2.Enums.SpeechTypes)
                _Mode = value
                buff.Position = 9
                buff.writebyte(_Mode)
            End Set
        End Property

        ''' <summary>Gets or sets the serial of the person or object speaking. 0xFFFFFFFF is used for system.</summary>
        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 3
                buff.writeuint(_Serial)
            End Set
        End Property

        ''' <summary>Gets or Sets the body value of the character that is talking. 0xFFFF is used for system.</summary>
        Public Property Body() As UShort
            Get
                Return _body
            End Get
            Set(ByVal value As UShort)
                _body = value
                buff.Position = 7
                buff.writeushort(value)
            End Set
        End Property

        ''' <summary>Gets or Sets the hue of the text.</summary>
        Public Property Hue() As UShort
            Get
                Return _hue
            End Get
            Set(ByVal value As UShort)
                _hue = value
                buff.Position = 10
                buff.writeushort(value)
            End Set
        End Property

        ''' <summary>Gets or Sets the font of the text.</summary>
        Public Property Font() As UOLite2.Enums.Fonts
            Get
                Return _font
            End Get
            Set(ByVal value As UOLite2.Enums.Fonts)
                _font = value
                buff.Position = 12
                buff.writeushort(value)
            End Set
        End Property

        ''' <summary>Gets or Sets the language key of the packet. This only effects how it is interpreted, it does NOT change the actual language.</summary>
        Public Property Language() As String
            Get
                Return _lang
            End Get
            Set(ByVal value As String)
                If value.Length <= 4 Then
                    _lang = value
                    buff.Position = 14
                    buff.writestrn(_lang, 4)
                Else
                    Throw New ConstraintException("Language string must be 4 characters or less!")
                End If
            End Set
        End Property

    End Class

    Public Class Text
        Inherits Packet
        Friend _Serial As Serial
        Friend _BodyType As UShort
        Friend _SpeechType As UOLite2.Enums.SpeechTypes
        Friend _TextHue As UShort = 0
        Friend _TextFont As UOLite2.Enums.Fonts = UOLite2.Enums.Fonts.Default
        Friend _Name As String = ""
        Friend _Text As String = ""

        Friend Sub New(ByVal Text As String)
            MyBase.New(UOLite2.Enums.PacketType.Text)

            Dim bytes(46 + Text.Length) As Byte
            bytes(0) = 28

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                .networkorder = False
                .writeushort(buff.buffer.Length)
                .networkorder = True

                .Position = 44
                .writestr(Text)

            End With

            Name = "System"
            Serial = New Serial(CUInt(4294967295))
            BodyType = CUShort(&HFFFF)
            TextFont = UOLite2.Enums.Fonts.Default

        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.Text)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes)

            With buff
                .Position = 3
                '3-6
                _Serial = .readuint
                '7-8
                _BodyType = .readushort
                '9-9
                _SpeechType = .readbyte
                '10-11
                _TextHue = .readushort
                '12-13
                _TextFont = .readushort
                '14-43
                _Name = .readstr

                .Position = 44

                '44-46
                _Text = .readstr
            End With

#Const DebugText = False

#If DebugText Then
                Console.WriteLine("-Text 0x1C: ")
                Console.WriteLine(" Serial: " & Serial.ToString)
                Console.WriteLine(" Name: " & Name)
                Console.WriteLine(" Body Type: " & BodyType)
                Console.WriteLine(" SpeechType: " & SpeechType)
                Console.WriteLine(" Hue: " & TextHue)
                Console.WriteLine(" Font: " & TextFont)
                Console.WriteLine(" Text: " & Text)
                Console.WriteLine("")
#End If

        End Sub

        Public Overrides ReadOnly Property Size() As UShort
            Get
                Return Data.Length
            End Get
        End Property

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal Value As Serial)
                _Serial = Value
                buff.Position = 3
                buff.writeuint(Value)
            End Set
        End Property

        Public Property BodyType() As UShort
            Get
                Return _BodyType
            End Get
            Set(ByVal Value As UShort)
                _BodyType = Value
                buff.Position = 7
                buff.writeushort(Value)
            End Set
        End Property

        Public Property SpeechType() As UOLite2.Enums.SpeechTypes
            Get
                Return _SpeechType
            End Get
            Set(ByVal Value As UOLite2.Enums.SpeechTypes)
                _SpeechType = Value
                buff.Position = 9
                buff.writebyte(Value)
            End Set
        End Property

        Public Property TextHue() As UShort
            Get
                Return _TextHue
            End Get
            Set(ByVal Value As UShort)
                _TextHue = Value
                buff.Position = 10
                buff.writeushort(Value)
            End Set
        End Property

        Public Property TextFont() As UOLite2.Enums.Fonts
            Get
                Return _TextFont
            End Get
            Set(ByVal Value As UOLite2.Enums.Fonts)
                _TextFont = Value
                buff.Position = 12
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Name() As String
            Get
                Return _Name
            End Get
            Set(ByVal Value As String)
                _Name = Value
                buff.Position = 13
                buff.writestrn(Value, 30)
            End Set
        End Property

        Public Property Text() As String
            Get
                Return _Text
            End Get
            Set(ByVal Value As String)
                _Text = Value
                buff.Position = 44
                buff.writestr(Value)
            End Set
        End Property

    End Class

    Public Class LocalizedText
        Inherits Packet
        Friend _Serial As Serial
        Friend _BodyType As UShort
        Friend _SpeechType As UOLite2.Enums.SpeechTypes
        Friend _Hue As UShort
        Friend _Font As UOLite2.Enums.Fonts
        Friend _CliLocNumber As UInt32
        Friend _Name As String
        Friend _ArgString As String

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.LocalizedText)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 3
                '3-6
                _Serial = .readuint
                '7-8
                _BodyType = .readushort
                '9-9
                _SpeechType = .readbyte
                '10-11
                _Hue = .readushort
                '12-13
                _Font = .readushort
                '14-17
                _CliLocNumber = .readuint
                '18-47
                _Name = .readstr

                buff.Position = 48
                '48-??
                _ArgString = .readustr
            End With
        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal Value As Serial)
                _Serial = Value
                buff.Position = 3
                buff.writeuint(Value.Value)
            End Set
        End Property

        Public Property BodyType() As UShort
            Get
                Return _BodyType
            End Get
            Set(ByVal Value As UShort)
                _BodyType = Value
                buff.Position = 7
                buff.writeushort(Value)
            End Set
        End Property

        Public Property SpeechType() As UOLite2.Enums.SpeechTypes
            Get
                Return _SpeechType
            End Get
            Set(ByVal Value As UOLite2.Enums.SpeechTypes)
                _SpeechType = Value
                buff.Position = 9
                buff.writebyte(Value)
            End Set
        End Property

        Public Property Hue() As UShort
            Get
                Return _Hue
            End Get
            Set(ByVal Value As UShort)
                _Hue = Value
                buff.Position = 10
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Font() As UOLite2.Enums.Fonts
            Get
                Return _Font
            End Get
            Set(ByVal Value As UOLite2.Enums.Fonts)
                _Font = Value
                buff.Position = 12
                buff.writeushort(Value)
            End Set
        End Property

        Public Property CliLocNumber() As UInt32
            Get
                Return _CliLocNumber
            End Get
            Set(ByVal Value As UInt32)
                _CliLocNumber = Value
                buff.Position = 14
                buff.writeuint(Value)
            End Set
        End Property

        Public Property Name() As String
            Get
                Return _Name
            End Get
            Set(ByVal Value As String)
                _Name = Value
                buff.Position = 18
                buff.writeustrn(Value, 30)
            End Set
        End Property

        Public Property ArgString() As String
            Get
                Return _ArgString
            End Get
            Set(ByVal Value As String)
                _ArgString = Value
                buff.Position = 48
                buff.writeustr(Value)
            End Set
        End Property

    End Class

#End Region

#Region "Items"
    ''' <summary>
    ''' This is sent by the server to open a container or game board (which is also a container). It is only for the art to pop up on the screen, and actualy serves no purpose here.
    ''' </summary>
    ''' <remarks>Packet 0x24</remarks>
    Public Class OpenContainer
        Inherits Packet

        Private _Serial As Serial
        Private _model As UShort

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.OpenContainer)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 1

            _size = 7
            '1-4
            _Serial = buff.readuint

            '5-6
            _model = buff.readushort

        End Sub

        ''' <summary>
        ''' The serial of the container being opened.
        ''' </summary>
        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 1
                buff.writeuint(value)
            End Set
        End Property

        ''' <summary>
        ''' The model of the container being opened.
        ''' </summary>
        Public Property Model() As UShort
            Get
                Return _model
            End Get
            Set(ByVal value As UShort)
                _model = value
                buff.Position = 5
                buff.writeushort(value)
            End Set
        End Property

    End Class

    ''' <summary>
    ''' This is sent by the server to add a single item to a container. (not to display its contents)
    ''' </summary>
    ''' <remarks>Packet 0x25</remarks>
    Public Class ObjectToObject
        Inherits Packet

        Friend _Serial As Serial
        Friend _Itemtype As UShort = 0
        Friend _stackID As Byte
        Friend _amount As UShort
        Friend _X As UShort
        Friend _Y As UShort
        Friend _Container As Serial
        Friend _Hue As UShort

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.ObjecttoObject)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            _size = _Data.Length

            buff.Position = 1
            '1-4
            _Serial = buff.readuint

            '5-6
            _Itemtype = buff.readushort

            '7
            _stackID = buff.readbyte

            '8-9
            _amount = buff.readushort

            '10-11
            _X = buff.readushort

            '12-13
            _Y = buff.readushort

            If Size = 21 Then
                '14
                'Grid Index: Since 6.0.1.7
                buff.Position += 1
            End If

            '15-18
            _Container = buff.readuint

            '19-20
            _Hue = buff.readushort

        End Sub

        ''' <summary>
        ''' The serial of the item to add.
        ''' </summary>
        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 1
                buff.writeuint(value)
            End Set
        End Property

        ''' <summary>
        ''' The artwork number of the item.
        ''' </summary>
        Public Property ItemType() As UShort
            Get
                Return _Itemtype
            End Get
            Set(ByVal value As UShort)
                _Itemtype = value
                buff.Position = 5
                buff.writeushort(value)
            End Set
        End Property

        Public Property StackID() As Byte
            Get
                Return _stackID
            End Get
            Set(ByVal value As Byte)
                _stackID = value
                buff.Position = 7
                buff.writebyte(value)
            End Set
        End Property

        Public Property Amount() As UShort
            Get
                Return _amount
            End Get
            Set(ByVal value As UShort)
                _amount = value
                buff.Position = 8
                buff.writeushort(_amount)
            End Set
        End Property

        Public Property X() As UShort
            Get
                Return _X
            End Get
            Set(ByVal value As UShort)
                _X = value
                buff.Position = 10
                buff.writeushort(_X)
            End Set
        End Property

        Public Property Y() As UShort
            Get
                Return _Y
            End Get
            Set(ByVal value As UShort)
                _Y = value
                buff.Position = 12
                buff.writeushort(_Y)
            End Set
        End Property

        Public Property Container() As Serial
            Get
                Return _Container
            End Get
            Set(ByVal value As Serial)
                _Container = value
                buff.Position = 14
                buff.writeuint(value)
            End Set
        End Property

        Public Property Hue() As UShort
            Get
                Return _Hue
            End Get
            Set(ByVal value As UShort)
                _Hue = value
                buff.Position = 18
                buff.writeushort(value)
            End Set
        End Property

    End Class

    ''' <summary>
    ''' This is sent to deny the player's request to get an item.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class GetItemFailed
        Inherits Packet
        Private _reason As UOLite2.Enums.GetItemFailedReason

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.GetItemFailed)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            _size = 2

            '1
            _reason = buff.readbyte

        End Sub

        Public Property Reason() As UOLite2.Enums.GetItemFailedReason
            Get
                Return _reason
            End Get
            Set(ByVal value As UOLite2.Enums.GetItemFailedReason)
                _reason = value
                buff.Position = 1
                buff.writebyte(_reason)
            End Set
        End Property

    End Class

    ''' <summary>
    ''' This is sent by the server to equip a single item on a character.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class EquipItem
        Inherits Packet

        Private _serial As Serial
        Private _itemtype As UShort = 0
        Private _layer As UOLite2.Enums.Layers
        Private _container As Serial
        Private _hue As UShort

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.EquipItem)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 1
            '1-4
            _serial = buff.readuint

            '5-6
            _itemtype = buff.readushort

            '7
            'Skip Unknown byte 0x00
            buff.Position += 1

            '8
            _layer = buff.readbyte

            '9-12
            _container = buff.readuint

            '13-14
            _hue = buff.readushort

        End Sub

        ''' <summary>
        ''' The serial of the item to equip.
        ''' </summary>
        Public Property Serial() As Serial
            Get
                Return _serial
            End Get
            Set(ByVal value As Serial)
                _serial = value
                buff.Position = 1
                buff.writeuint(value)
            End Set
        End Property

        ''' <summary>
        ''' The item's artwork number.
        ''' </summary>
        Public Property ItemType() As UShort
            Get
                Return _itemtype
            End Get
            Set(ByVal value As UShort)
                _itemtype = value
                buff.Position = 5
                buff.writeushort(value)
            End Set
        End Property

        ''' <summary>
        ''' The item's layer
        ''' </summary>
        Public Property Layer() As UOLite2.Enums.Layers
            Get
                Return _layer
            End Get
            Set(ByVal value As UOLite2.Enums.Layers)
                _layer = value
                buff.Position = 8
                buff.writebyte(value)
            End Set
        End Property

        ''' <summary>
        ''' The serial of the character on which the item will be equipped.
        ''' </summary>
        Public Property Container() As Serial
            Get
                Return _container
            End Get
            Set(ByVal value As Serial)
                _container = value
                buff.Position = 9
                buff.writeuint(value)
            End Set
        End Property

        ''' <summary>
        ''' The item's hue.
        ''' </summary>
        Public Property Hue() As UShort
            Get
                Return _hue
            End Get
            Set(ByVal value As UShort)
                _hue = value
                buff.Position = 13
                buff.writeushort(value)
            End Set
        End Property

    End Class

    ''' <summary>
    ''' This is sent to display the contents of a container.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ContainerContents
        Inherits Packet

        Private _ItemList As New HashSet(Of Item)
        Private _Count As UShort

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.ContainerContents)
            _Data = bytes
            _size = bytes.Length

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

#If DebugItems Then
                Console.WriteLine("Container Contents: " & BitConverter.ToString(bytes))
#End If

            buff.Position = 3

            '3-4
            _Count = buff.readushort

            Dim it As New Item

            If _Count >= 1 Then
                If (Size - 5) Mod 20 = 0 Then
                    For i As UShort = 0 To _Count - 1
                        it = New Item

                        '5-8
                        it._Serial = buff.readuint

                        '9-10
                        it._Type = buff.readushort

                        '11
                        it._StackID = buff.readbyte

                        '12-13
                        it._Amount = buff.readushort

                        '14-15
                        it._X = buff.readushort

                        '16-17
                        it._Y = buff.readushort

                        '18
                        it._GridIndex = buff.readbyte

                        '19-22
                        it._Container = buff.readuint

                        '23-24
                        it._Hue = buff.readushort

                        'Debug.WriteLine("-Adding item to Container Contents Packet ItemList.")
                        'Debug.WriteLine(" Container Serial: " & it.Container.Value)
                        'Debug.WriteLine(" Serial: " & it.Serial.Value)
                        'Debug.WriteLine(" Count: " & it.Amount)

                        _ItemList.Add(it)
                    Next
                Else
                    For i As UShort = 0 To _Count - 1
                        it = New Item

                        '5-8
                        it._Serial = buff.readuint

                        '9-10
                        it._Type = buff.readushort

                        '11
                        it._StackID = buff.readbyte

                        '12-13
                        it._Amount = buff.readushort

                        '14-15
                        it._X = buff.readushort

                        '16-17
                        it._Y = buff.readushort

                        '19-22
                        it._Container = buff.readuint

                        '23-24
                        it._Hue = buff.readushort

#If DebugItems Then
                    Console.WriteLine("Adding item to Container Contents Packet ItemList.")
                    Console.WriteLine("Container Serial: " & it.Container.Value)
                    Console.WriteLine("Serial: " & it.Serial.Value)
#End If
                        _ItemList.Add(it)
                    Next
                End If

            End If

        End Sub

        Public Overloads ReadOnly Property Items() As HashSet(Of Item)
            Get
                Return _ItemList
            End Get
        End Property

    End Class

    Public Class ShowItem
        Inherits Packet

        Private _Serial As Serial
        Private _ItemType As UShort = 0
        Private _Amount As UShort = 1
        Private _StackID As Byte = 0
        Private _X As UShort = 0
        Private _Y As UShort = 0
        Private _Direction As Byte = 0
        Private _Z As Byte = 0
        Private _Hue As UShort = 0
        Private _Status As Byte = 0

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.ShowItem)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 3
                _Serial = .readuint
                _ItemType = .readushort

                If _Serial.Value >= 2147483648 Then
                    _Serial.Value -= 2147483648
                    _Amount = .readushort 'Check Serial for flag 0x80000000
                End If
                If _ItemType >= 32768 Then 'Check Item Type for flag 0x8000
                    _ItemType -= 32768
                    _StackID = .readbyte
                End If

                _X = .readushort
                _Y = .readushort

                If _X >= 32768 Then
                    _X -= 32768
                    _Direction = .readbyte 'Check _X for flag 0x8000
                End If

                _Z = .readbyte

                Select Case _Y
                    Case Is > 49152 'Flag 0x8000 and 0x4000
                        _Y -= 49152
                        _Hue = .readushort
                        _Status = .readbyte
                    Case Is > 32768 'Flag 0x8000
                        _Y -= 32768
                        _Hue = .readushort
                    Case Is > 16384 'Flag 0x4000
                        _Y -= 16384
                        _Status = .readbyte
                End Select

            End With

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property ItemType() As UShort
            Get
                Return _ItemType
            End Get
        End Property

        Public ReadOnly Property Amount() As UShort
            Get
                Return _Amount
            End Get
        End Property

        Public ReadOnly Property StackID() As UShort
            Get
                Return _StackID
            End Get
        End Property

        Public ReadOnly Property X() As UShort
            Get
                Return _X
            End Get
        End Property

        Public ReadOnly Property Y() As UShort
            Get
                Return _Y
            End Get
        End Property

        Public ReadOnly Property Direction() As UOLite2.Enums.Direction
            Get
                Return _Direction
            End Get
        End Property

        Public ReadOnly Property Z() As Byte
            Get
                Return _Z
            End Get
        End Property

        Public ReadOnly Property Hue() As UShort
            Get
                Return _Hue
            End Get
        End Property

        Public ReadOnly Property Status() As Byte
            Get
                Return _Status
            End Get
        End Property

    End Class

    Public Class MegaCliLoc
        Inherits Packet

        Friend _Serial As Serial
        Friend _Serial2 As Serial


        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.MegaCliloc)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 3
                _Serial = .readuint


            End With

        End Sub
    End Class

#End Region

#Region "Mobiles"

    ''' <summary>
    ''' Sent by the client to rename another mobile.
    ''' </summary>
    Public Class RenameMOB
        Inherits Packet

        Private _Serial As Serial
        Private _Name As String


        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.RenameMOB)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 1
            '1-4
            _Serial = buff.readuint

            '5-35
            _Name = buff.readstrn(30)

        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 1
                buff.writeuint(value)
            End Set
        End Property

        Public Property Name() As String
            Get
                Return _Name
            End Get
            Set(ByVal value As String)
                If value.Length <= 30 Then
                    _Name = value
                    buff.Position = 5
                    buff.writestrn(_Name, 30)
                Else
                    Throw New ConstraintException("String specified for name is too long, it must be < 30 characters long.")
                End If
            End Set
        End Property

    End Class

    ''' <summary>
    ''' Yo programmer, I'm really happy for you and I'm gona let you finish 
    ''' but this is one of the biggest packet classes of all time, OF ALL TIME!
    '''  -Kanye West
    ''' 757 lines...
    ''' </summary>
    Public Class MobileStats
        Inherits Packet

        Friend _Serial As Serial
        Friend _Name As String = ""
        Friend _Hits As UShort = 1
        Friend _HitsMax As UShort = 1
        Friend _Renamable As Enums.Renamable = Enums.Renamable.NotRenamable
        Friend _DisplayMode As Enums.DisplayMode = Enums.DisplayMode.Normal
        Friend _Gender As UOLite2.Enums.Gender = UOLite2.Enums.Gender.Neutral
        Friend _Strength As UShort = 1
        Friend _Dexterity As UShort = 1
        Friend _Intelligence As UShort = 1
        Friend _Stamina As UShort = 1
        Friend _StaminaMax As UShort = 1
        Friend _Mana As UShort = 1
        Friend _ManaMax As UShort = 1
        Friend _Gold As UInt32 = 0
        Friend _ResistPhysical As UShort = 0
        Friend _Weight As UShort = 0

        'Included in Mobile Stat Packet if SF 0x03
        Friend _StatCap As UShort = 1
        Friend _Followers As Byte = 0
        Friend _FollowersMax As Byte = 5

        'Included in Mobile Stat Packet if SF 0x04
        Friend _ResistFire As UShort = 0
        Friend _ResistCold As UShort = 0
        Friend _ResistPoison As UShort = 0
        Friend _ResistEnergy As UShort = 0
        Friend _Luck As UShort = 0
        Friend _DamageMin As UShort = 1
        Friend _DamageMax As UShort = 1
        Friend _TithingPoints As UShort = 0

        'Included in Mobile Stat Packet if SF 0x05
        Friend _Race As Byte = 0
        Friend _WeightMax As UShort = 0

        'Included in Mobile Stat Packet if SF 0x06
        Friend _HitChanceIncrease As Short = 1
        Friend _SwingSpeedIncrease As Short = 1
        Friend _DamageChanceIncrease As Short = 1
        Friend _LowerReagentCost As Short = 1
        Friend _HitPointsRegeneration As Short = 1
        Friend _StaminaRegeneration As Short = 1
        Friend _ManaRegeneration As Short = 1
        Friend _ReflectPhysicalDamage As Short = 1
        Friend _EnhancePotions As Short = 1
        Friend _DefenseChanceIncrease As Short = 1
        Friend _SpellDamageIncrease As Short = 1
        Friend _FasterCastRecovery As Short = 1
        Friend _FasterCasting As Short = 1
        Friend _LowerManaCost As Short = 1
        Friend _StrengthIncrease As Short = 1
        Friend _DexterityIncrease As Short = 1
        Friend _IntelligenceIncrease As Short = 1
        Friend _HitPointsIncrease As Short = 1
        Friend _StaminaIncrease As Short = 1
        Friend _ManaIncrease As Short = 1
        Friend _MaximumHitPointsIncrease As Short = 1
        Friend _MaximumStaminaIncrease As Short = 1
        Friend _MaximumManaIncrease As Short = 1

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.MobileStats)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            _size = bytes.Length
            _Data = bytes

            With buff
                .Position = 3
                _Serial = .readuint
                _Name = .readstrn(30)
                _Hits = .readushort
                _HitsMax = .readushort
                _Renamable = .readbyte
                _DisplayMode = .readbyte
                _Gender = .readbyte
                _Strength = .readushort
                _Dexterity = .readushort
                _Intelligence = .readushort
                _Stamina = .readushort
                _StaminaMax = .readushort
                _Mana = .readushort
                _ManaMax = .readushort
                _Gold = .readuint
                _ResistPhysical = .readushort
                _Weight = .readushort

                Select Case _DisplayMode

                    Case 3 ' 0x00 through 0x03
                        _StatCap = .readushort
                        _Followers = .readbyte
                        _FollowersMax = .readbyte
                    Case Enums.DisplayMode.StatCap_Followers_Resistances ' 0x04
                        _StatCap = .readushort
                        _Followers = .readbyte
                        _FollowersMax = .readbyte
                        _ResistFire = .readushort
                        _ResistCold = .readushort
                        _ResistPoison = .readushort
                        _ResistEnergy = .readushort
                        _Luck = .readushort
                        _DamageMin = .readushort
                        _DamageMax = .readushort
                        _TithingPoints = .readushort
                    Case Enums.DisplayMode.SupportedFeatures5 ' 0x05
                        _WeightMax = .readushort
                        _Race = .readbyte
                        _StatCap = .readushort
                        _Followers = .readbyte
                        _FollowersMax = .readbyte
                        _ResistFire = .readushort
                        _ResistCold = .readushort
                        _ResistPoison = .readushort
                        _ResistEnergy = .readushort
                        _Luck = .readushort
                        _DamageMin = .readushort
                        _DamageMax = .readushort
                        _TithingPoints = .readushort

                    Case Enums.DisplayMode.KR ' 0x06
                        _WeightMax = .readushort
                        _Race = .readbyte
                        _StatCap = .readushort
                        _Followers = .readbyte
                        _FollowersMax = .readbyte
                        _ResistFire = .readushort
                        _ResistCold = .readushort
                        _ResistPoison = .readushort
                        _ResistEnergy = .readushort
                        _Luck = .readushort
                        _DamageMin = .readushort
                        _DamageMax = .readushort
                        _TithingPoints = .readushort

                        _HitChanceIncrease = .readushort
                        _SwingSpeedIncrease = .readushort
                        _DamageChanceIncrease = .readushort
                        _LowerReagentCost = .readushort
                        _HitPointsRegeneration = .readushort
                        _StaminaRegeneration = .readushort
                        _ManaRegeneration = .readushort
                        _ReflectPhysicalDamage = .readushort
                        _EnhancePotions = .readushort
                        _DefenseChanceIncrease = .readushort
                        _SpellDamageIncrease = .readushort
                        _FasterCastRecovery = .readushort
                        _FasterCasting = .readushort
                        _LowerManaCost = .readushort
                        _StrengthIncrease = .readushort
                        _DexterityIncrease = .readushort
                        _IntelligenceIncrease = .readushort
                        _HitPointsIncrease = .readushort
                        _StaminaIncrease = .readushort
                        _ManaIncrease = .readushort
                        _MaximumHitPointsIncrease = .readushort
                        _MaximumStaminaIncrease = .readushort
                        _MaximumManaIncrease = .readushort


                End Select

            End With

        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal Value As Serial)
                _Serial = Value
                buff.Position = 3
                buff.writeuint(Value.Value)
            End Set
        End Property

        Public Property Name() As String
            Get
                Return _Name
            End Get
            Set(ByVal Value As String)
                _Name = Value
                buff.Position = 7
                buff.writeustrn(Value, 30)
            End Set
        End Property

        Public Property Hits() As UShort
            Get
                Return _Hits
            End Get
            Set(ByVal Value As UShort)
                _Hits = Value
                buff.Position = 37
                buff.writeushort(Value)
            End Set
        End Property

        Public Property HitsMax() As UShort
            Get
                Return _HitsMax
            End Get
            Set(ByVal Value As UShort)
                _HitsMax = Value
                buff.Position = 39
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Renamable() As Enums.Renamable
            Get
                Return _Renamable
            End Get
            Set(ByVal Value As Enums.Renamable)
                _Renamable = Value
                buff.Position = 41
                buff.writebyte(Value)
            End Set
        End Property

        Public Property DisplayMode() As Enums.DisplayMode
            Get
                Return _DisplayMode
            End Get
            Set(ByVal Value As Enums.DisplayMode)
                _DisplayMode = Value
                buff.Position = 42
                buff.writebyte(Value)
            End Set
        End Property

        Public Property Gender() As UOLite2.Enums.Gender
            Get
                Return _Gender
            End Get
            Set(ByVal Value As UOLite2.Enums.Gender)
                _Gender = Value
                buff.Position = 43
                buff.writebyte(Value)
            End Set
        End Property

        Public Property Strength() As UShort
            Get
                Return _Strength
            End Get
            Set(ByVal Value As UShort)
                _Strength = Value
                buff.Position = 44
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Dexterity() As UShort
            Get
                Return _Dexterity
            End Get
            Set(ByVal Value As UShort)
                _Dexterity = Value
                buff.Position = 46
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Intelligence() As UShort
            Get
                Return _Intelligence
            End Get
            Set(ByVal Value As UShort)
                _Intelligence = Value
                buff.Position = 48
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Stamina() As UShort
            Get
                Return _Stamina
            End Get
            Set(ByVal Value As UShort)
                _Stamina = Value
                buff.Position = 50
                buff.writeushort(Value)
            End Set
        End Property

        Public Property StaminaMax() As UShort
            Get
                Return _StaminaMax
            End Get
            Set(ByVal Value As UShort)
                _StaminaMax = Value
                buff.Position = 52
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Mana() As UShort
            Get
                Return _Mana
            End Get
            Set(ByVal Value As UShort)
                _Mana = Value
                buff.Position = 54
                buff.writeushort(Value)
            End Set
        End Property

        Public Property ManaMax() As UShort
            Get
                Return _ManaMax
            End Get
            Set(ByVal Value As UShort)
                _ManaMax = Value
                buff.Position = 56
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Gold() As UInt32
            Get
                Return _Gold
            End Get
            Set(ByVal Value As UInt32)
                _Gold = Value
                buff.Position = 58
                buff.writeuint(Value)
            End Set
        End Property

        Public Property ResistPhysical() As UShort
            Get
                Return _ResistPhysical
            End Get
            Set(ByVal Value As UShort)
                _ResistPhysical = Value
                buff.Position = 62
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Weight() As UShort
            Get
                Return _Weight
            End Get
            Set(ByVal Value As UShort)
                _Weight = Value
                buff.Position = 64
                buff.writeushort(Value)
            End Set
        End Property

        Public Property StatCap() As UShort
            Get
                Return _StatCap
            End Get
            Set(ByVal Value As UShort)
                _StatCap = Value
                buff.Position = 66
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Followers() As Byte
            Get
                Return _Followers
            End Get
            Set(ByVal Value As Byte)
                _Followers = Value
                buff.Position = 68
                buff.writebyte(Value)
            End Set
        End Property

        Public Property FollowersMax() As Byte
            Get
                Return _FollowersMax
            End Get
            Set(ByVal Value As Byte)
                _FollowersMax = Value
                buff.Position = 69
                buff.writebyte(Value)
            End Set
        End Property

        Public Property ResistFire() As UShort
            Get
                Return _ResistFire
            End Get
            Set(ByVal Value As UShort)
                _ResistFire = Value
                buff.Position = 70
                buff.writeushort(Value)
            End Set
        End Property

        Public Property ResistCold() As UShort
            Get
                Return _ResistCold
            End Get
            Set(ByVal Value As UShort)
                _ResistCold = Value
                buff.Position = 72
                buff.writeushort(Value)
            End Set
        End Property

        Public Property ResistPoison() As UShort
            Get
                Return _ResistPoison
            End Get
            Set(ByVal Value As UShort)
                _ResistPoison = Value
                buff.Position = 74
                buff.writeushort(Value)
            End Set
        End Property

        Public Property ResistEnergy() As UShort
            Get
                Return _ResistEnergy
            End Get
            Set(ByVal Value As UShort)
                _ResistEnergy = Value
                buff.Position = 76
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Luck() As UShort
            Get
                Return _Luck
            End Get
            Set(ByVal Value As UShort)
                _Luck = Value
                buff.Position = 78
                buff.writeushort(Value)
            End Set
        End Property

        Public Property DamageMin() As UShort
            Get
                Return _DamageMin
            End Get
            Set(ByVal Value As UShort)
                _DamageMin = Value
                buff.Position = 80
                buff.writeushort(Value)
            End Set
        End Property

        Public Property DamageMax() As UShort
            Get
                Return _DamageMax
            End Get
            Set(ByVal Value As UShort)
                _DamageMax = Value
                buff.Position = 82
                buff.writeushort(Value)
            End Set
        End Property

        Public Property TithingPoints() As UShort
            Get
                Return _TithingPoints
            End Get
            Set(ByVal Value As UShort)
                _TithingPoints = Value
                buff.Position = 84
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Race() As Byte
            Get
                Return _Race
            End Get
            Set(ByVal Value As Byte)
                _Race = Value
                buff.Position = 86
                buff.writebyte(Value)
            End Set
        End Property

        Public Property WeightMax() As UShort
            Get
                Return _WeightMax
            End Get
            Set(ByVal Value As UShort)
                _WeightMax = Value
                buff.Position = 87
                buff.writeushort(Value)
            End Set
        End Property

        Public Property HitChanceIncrease() As UShort
            Get
                Return _HitChanceIncrease
            End Get
            Set(ByVal Value As UShort)
                _HitChanceIncrease = Value
                buff.Position = 89
                buff.writeushort(Value)
            End Set
        End Property

        Public Property SwingSpeedIncrease() As UShort
            Get
                Return _SwingSpeedIncrease
            End Get
            Set(ByVal Value As UShort)
                _SwingSpeedIncrease = Value
                buff.Position = 91
                buff.writeushort(Value)
            End Set
        End Property

        Public Property DamageChanceIncrease() As UShort
            Get
                Return _DamageChanceIncrease
            End Get
            Set(ByVal Value As UShort)
                _DamageChanceIncrease = Value
                buff.Position = 93
                buff.writeushort(Value)
            End Set
        End Property

        Public Property LowerReagentCost() As UShort
            Get
                Return _LowerReagentCost
            End Get
            Set(ByVal Value As UShort)
                _LowerReagentCost = Value
                buff.Position = 95
                buff.writeushort(Value)
            End Set
        End Property

        Public Property HitPointsRegeneration() As UShort
            Get
                Return _HitPointsRegeneration
            End Get
            Set(ByVal Value As UShort)
                _HitPointsRegeneration = Value
                buff.Position = 97
                buff.writeushort(Value)
            End Set
        End Property

        Public Property StaminaRegeneration() As UShort
            Get
                Return _StaminaRegeneration
            End Get
            Set(ByVal Value As UShort)
                _StaminaRegeneration = Value
                buff.Position = 99
                buff.writeushort(Value)
            End Set
        End Property

        Public Property ManaRegeneration() As UShort
            Get
                Return _ManaRegeneration
            End Get
            Set(ByVal Value As UShort)
                _ManaRegeneration = Value
                buff.Position = 101
                buff.writeushort(Value)
            End Set
        End Property

        Public Property ReflectPhysicalDamage() As UShort
            Get
                Return _ReflectPhysicalDamage
            End Get
            Set(ByVal Value As UShort)
                _ReflectPhysicalDamage = Value
                buff.Position = 103
                buff.writeushort(Value)
            End Set
        End Property

        Public Property EnhancePotions() As UShort
            Get
                Return _EnhancePotions
            End Get
            Set(ByVal Value As UShort)
                _EnhancePotions = Value
                buff.Position = 105
                buff.writeushort(Value)
            End Set
        End Property

        Public Property DefenseChanceIncrease() As UShort
            Get
                Return _DefenseChanceIncrease
            End Get
            Set(ByVal Value As UShort)
                _DefenseChanceIncrease = Value
                buff.Position = 107
                buff.writeushort(Value)
            End Set
        End Property

        Public Property SpellDamageIncrease() As UShort
            Get
                Return _SpellDamageIncrease
            End Get
            Set(ByVal Value As UShort)
                _SpellDamageIncrease = Value
                buff.Position = 109
                buff.writeushort(Value)
            End Set
        End Property

        Public Property FasterCastRecovery() As UShort
            Get
                Return _FasterCastRecovery
            End Get
            Set(ByVal Value As UShort)
                _FasterCastRecovery = Value
                buff.Position = 111
                buff.writeushort(Value)
            End Set
        End Property

        Public Property FasterCasting() As UShort
            Get
                Return _FasterCasting
            End Get
            Set(ByVal Value As UShort)
                _FasterCasting = Value
                buff.Position = 113
                buff.writeushort(Value)
            End Set
        End Property

        Public Property LowerManaCost() As UShort
            Get
                Return _LowerManaCost
            End Get
            Set(ByVal Value As UShort)
                _LowerManaCost = Value
                buff.Position = 115
                buff.writeushort(Value)
            End Set
        End Property

        Public Property StrengthIncrease() As UShort
            Get
                Return _StrengthIncrease
            End Get
            Set(ByVal Value As UShort)
                _StrengthIncrease = Value
                buff.Position = 117
                buff.writeushort(Value)
            End Set
        End Property

        Public Property DexterityIncrease() As UShort
            Get
                Return _DexterityIncrease
            End Get
            Set(ByVal Value As UShort)
                _DexterityIncrease = Value
                buff.Position = 119
                buff.writeushort(Value)
            End Set
        End Property

        Public Property IntelligenceIncrease() As UShort
            Get
                Return _IntelligenceIncrease
            End Get
            Set(ByVal Value As UShort)
                _IntelligenceIncrease = Value
                buff.Position = 121
                buff.writeushort(Value)
            End Set
        End Property

        Public Property HitPointsIncrease() As UShort
            Get
                Return _HitPointsIncrease
            End Get
            Set(ByVal Value As UShort)
                _HitPointsIncrease = Value
                buff.Position = 123
                buff.writeushort(Value)
            End Set
        End Property

        Public Property StaminaIncrease() As UShort
            Get
                Return _StaminaIncrease
            End Get
            Set(ByVal Value As UShort)
                _StaminaIncrease = Value
                buff.Position = 125
                buff.writeushort(Value)
            End Set
        End Property

        Public Property ManaIncrease() As UShort
            Get
                Return _ManaIncrease
            End Get
            Set(ByVal Value As UShort)
                _ManaIncrease = Value
                buff.Position = 127
                buff.writeushort(Value)
            End Set
        End Property

        Public Property MaximumHitPointsIncrease() As UShort
            Get
                Return _MaximumHitPointsIncrease
            End Get
            Set(ByVal Value As UShort)
                _MaximumHitPointsIncrease = Value
                buff.Position = 129
                buff.writeushort(Value)
            End Set
        End Property

        Public Property MaximumStaminaIncrease() As UShort
            Get
                Return _MaximumStaminaIncrease
            End Get
            Set(ByVal Value As UShort)
                _MaximumStaminaIncrease = Value
                buff.Position = 131
                buff.writeushort(Value)
            End Set
        End Property

        Public Property MaximumManaIncrease() As UShort
            Get
                Return _MaximumManaIncrease
            End Get
            Set(ByVal Value As UShort)
                _MaximumManaIncrease = Value
                buff.Position = 133
                buff.writeushort(Value)
            End Set
        End Property


    End Class

    ''' <summary>
    ''' This is sent by the server to tell the client to update a Mobile's health and max health.
    ''' </summary>
    Public Class HPHealth
        Inherits Packet

        Private _Serial As Serial
        Private _HitsMax As UShort
        Private _Hits As UShort

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.HPHealth)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            _size = bytes.Length
            _Data = bytes

            With buff
                .Position = 1
                '1-4
                _Serial = .readuint

                '5-6
                _HitsMax = .readushort

                '7-8
                _Hits = .readushort
            End With
        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 1
                buff.writeuint(value.Value)
            End Set
        End Property

        Public Property HitsMax() As UShort
            Get
                Return _HitsMax
            End Get
            Set(ByVal value As UShort)
                _HitsMax = value
                buff.Position = 5
                buff.writeushort(value)
            End Set
        End Property

        Public Property Hits() As UShort
            Get
                Return _Hits
            End Get
            Set(ByVal value As UShort)
                _Hits = value
                buff.Position = 7
                buff.writeushort(value)
            End Set
        End Property

    End Class

    Public Class FatHealth
        Inherits Packet

        Private _Serial As Serial
        Private _StamMax As UShort
        Private _Stam As UShort

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.FatHealth)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            _size = bytes.Length
            _Data = bytes

            With buff
                .Position = 1
                '1-4
                _Serial = .readuint

                '5-6
                _StamMax = .readushort

                '7-8
                _Stam = .readushort
            End With
        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 1
                buff.writeuint(value.Value)
            End Set
        End Property

        Public Property StamMax() As UShort
            Get
                Return _StamMax
            End Get
            Set(ByVal value As UShort)
                _StamMax = value
                buff.Position = 5
                buff.writeushort(value)
            End Set
        End Property

        Public Property Stam() As UShort
            Get
                Return _Stam
            End Get
            Set(ByVal value As UShort)
                _Stam = value
                buff.Position = 7
                buff.writeushort(value)
            End Set
        End Property

    End Class

    Public Class ManaHealth
        Inherits Packet

        Private _Serial As Serial
        Private _ManaMax As UShort
        Private _Mana As UShort

        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.ManaHealth)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            _size = bytes.Length
            _Data = bytes

            With buff
                .Position = 1
                '1-4
                _Serial = .readuint

                '5-6
                _ManaMax = .readushort

                '7-8
                _Mana = .readushort
            End With
        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 1
                buff.writeuint(value.Value)
            End Set
        End Property

        Public Property ManaMax() As UShort
            Get
                Return _ManaMax
            End Get
            Set(ByVal value As UShort)
                _ManaMax = value
                buff.Position = 5
                buff.writeushort(value)
            End Set
        End Property

        Public Property Mana() As UShort
            Get
                Return _Mana
            End Get
            Set(ByVal value As UShort)
                _Mana = value
                buff.Position = 7
                buff.writeushort(value)
            End Set
        End Property

    End Class

    Public Class EquippedMobile
        Inherits Packet

        Private _Serial As Serial
        Private _BodyType As UShort
        Private _X As UShort
        Private _Y As UShort
        Private _Z As Byte
        Private _Direction As UOLite2.Enums.Direction
        Private _Hue As UShort
        Private _Status As UOLite2.Enums.MobileStatus
        Private _Notoriety As UOLite2.Enums.Reputation
        Private _EquippedItems As New HashSet(Of Item)
        Private _AmountOrCorpse As UShort = 1

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.EquippedMOB)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 3
                _Serial = .readuint
                _BodyType = .readushort

                If _Serial.Value >= 2147483648 Then
                    _Serial = New Serial(_Serial.Value - 2147483648)
                    _AmountOrCorpse = .readushort
                End If

                _X = .readushort
                _Y = .readushort
                _Z = .readbyte
                _Direction = .readbyte
                _Hue = .readushort
                _Status = .readbyte
                _Notoriety = .readbyte

                Dim i As Item
                Do
                    i = New Item

                    i._Container = LiteClient.WorldSerial
                    i._Serial = .readuint
                    i._Type = .readushort
                    i._Layer = .readbyte

                    'Check for the Hue flag in the item Type
                    If i._Type >= 32768 Then
                        i._Type -= 32768
                        _Hue = .readushort
                    End If

#If DebugMobile Then
                        Console.WriteLine("-Adding item to Equipped Mobile Packet EquippedItems ItemList.")
                        Console.WriteLine(" Serial: " & i.Serial.ToString)
                        Console.WriteLine(" Container/Mobile Serial: " & i.Container.ToString)
#End If

                    EquippedItems.Add(i)
                Loop Until _size - buff.Position <= 4

            End With

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property Amount() As UShort
            Get
                Return _AmountOrCorpse
            End Get
        End Property

        Public ReadOnly Property BodyType() As UShort
            Get
                Return _BodyType
            End Get
        End Property

        Public ReadOnly Property X() As UShort
            Get
                Return _X
            End Get
        End Property

        Public ReadOnly Property Y() As UShort
            Get
                Return _Y
            End Get
        End Property

        Public ReadOnly Property Z() As Byte
            Get
                Return _Z
            End Get
        End Property

        Public ReadOnly Property Direction() As Byte
            Get
                Return _Direction
            End Get
        End Property

        Public ReadOnly Property Hue() As UShort
            Get
                Return _Hue
            End Get
        End Property

        Public ReadOnly Property Status() As UShort
            Get
                Return _Status
            End Get
        End Property

        Public ReadOnly Property Notoriety()
            Get
                Return _Notoriety
            End Get
        End Property

        Public ReadOnly Property EquippedItems() As HashSet(Of Item)
            Get
                Return _EquippedItems
            End Get
        End Property

        Public ReadOnly Property Count() As Byte
            Get
                Return _EquippedItems.Count
            End Get
        End Property

    End Class

    Public Class NakedMobile
        Inherits Packet

        Private _Serial As Serial
        Private _BodyType As UShort
        Private _X As UShort
        Private _Y As UShort
        Private _Z As Byte
        Private _Direction As UOLite2.Enums.Direction
        Private _Hue As UShort
        Private _Status As UOLite2.Enums.MobileStatus
        Private _Notoriety As UOLite2.Enums.Reputation

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.NakedMOB)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                _Serial = .readuint
                _BodyType = .readushort
                _X = .readushort
                _Y = .readushort
                _Z = .readbyte
                _Direction = .readbyte
                _Hue = .readushort
                _Status = .readbyte
                _Notoriety = .readbyte
            End With

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property BodyType() As UShort
            Get
                Return _BodyType
            End Get
        End Property

        Public ReadOnly Property X() As UShort
            Get
                Return _X
            End Get
        End Property

        Public ReadOnly Property Y() As UShort
            Get
                Return _Y
            End Get
        End Property

        Public ReadOnly Property Z() As Byte
            Get
                Return _Z
            End Get
        End Property

        Public ReadOnly Property Direction() As Byte
            Get
                Return _Direction
            End Get
        End Property

        Public ReadOnly Property Hue() As UShort
            Get
                Return _Hue
            End Get
        End Property

        Public ReadOnly Property Status() As UShort
            Get
                Return _Status
            End Get
        End Property

        Public ReadOnly Property Notoriety()
            Get
                Return _Notoriety
            End Get
        End Property

    End Class

    Public Class DeathAnimation
        Inherits Packet

        Private _Serial As Serial
        Private _CorpseSerial As Serial

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.DeathAnimation)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                buff.Position = 1

                _Serial = .readuint
                _CorpseSerial = .readuint

            End With

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property CorpseSerial() As Serial
            Get
                Return _CorpseSerial
            End Get
        End Property

    End Class

    Public Class LoginConfirm
        Inherits Packet
        Private _Serial As Serial
        Friend _BodyType As UShort
        Friend _X As UShort
        Friend _Y As UShort
        Friend _Z As Byte
        Friend _Direction As Byte
        Friend _MapWidth As UShort
        Friend _MapHeight As UShort

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.LoginConfirm)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                '1-4
                _Serial = .readuint

                .Position += 4

                '9-10
                _BodyType = .readushort
                '11-12
                _X = .readushort
                '13-14
                _Y = .readushort

                buff.Position += 1

                '16
                _Z = .readbyte
                '17
                _Direction = .readbyte

                buff.Position += 9

                '26-27
                _MapWidth = .readushort
                '28-29
                _MapHeight = .readushort
            End With
        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal Value As Serial)
                _Serial = Value
                buff.Position = 1
                buff.writeuint(Value.Value)
            End Set
        End Property

        Public Property BodyType() As UShort
            Get
                Return _BodyType
            End Get
            Set(ByVal Value As UShort)
                _BodyType = Value
                buff.Position = 9
                buff.writeushort(Value)
            End Set
        End Property

        Public Property X() As UShort
            Get
                Return _X
            End Get
            Set(ByVal Value As UShort)
                _X = Value
                buff.Position = 11
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Y() As UShort
            Get
                Return _Y
            End Get
            Set(ByVal Value As UShort)
                _Y = Value
                buff.Position = 13
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Z() As Byte
            Get
                Return _Z
            End Get
            Set(ByVal Value As Byte)
                _Z = Value
                buff.Position = 16
                buff.writebyte(Value)
            End Set
        End Property

        Public Property Direction() As Byte
            Get
                Return _Direction
            End Get
            Set(ByVal Value As Byte)
                _Direction = Value
                buff.Position = 17
                buff.writebyte(Value)
            End Set
        End Property

        Public Property MapWidth() As UShort
            Get
                Return _MapWidth
            End Get
            Set(ByVal Value As UShort)
                _MapWidth = Value
                buff.Position = 26
                buff.writeushort(Value)
            End Set
        End Property

        Public Property MapHeight() As UShort
            Get
                Return _MapHeight
            End Get
            Set(ByVal Value As UShort)
                _MapHeight = Value
                buff.Position = 28
                buff.writeushort(Value)
            End Set
        End Property

    End Class

    Public Class HealthBarStatusUpdate
        Inherits Packet

        Private _Serial As Serial
        Private _StatusColor As UShort = 0
        Private _StatusFlag As Byte = 0

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.HealthBarStatusUpdate)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 3
                _Serial = .readuint

                .Position += 2

                _StatusColor = .readushort
                _StatusFlag = .readbyte
            End With
        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property Color() As UShort
            Get
                Return _StatusColor
            End Get
        End Property

        Public ReadOnly Property Flag() As Byte
            Get
                Return _StatusFlag
            End Get
        End Property

    End Class

#End Region

#Region "Movement"
    Public Class FastWalk
        Inherits GenericCommand

        Private _keys(5) As UInt32

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.BF_Sub_Commands.FastWalk)
            _Data = bytes

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 5
            For i As Integer = 0 To _keys.Length - 1
                _keys(i) = buff.readuint
            Next
        End Sub

        Public ReadOnly Property Keys() As UInt32()
            Get
                Return _keys
            End Get
        End Property

    End Class

    Public Class AddWalkKey
        Inherits GenericCommand

        Private _key As UInt32

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.BF_Sub_Commands.AddWalkKey)
            _Data = bytes

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 5
            _key = buff.readuint
        End Sub

        Public ReadOnly Property Key As UInt32
            Get
                Return _key
            End Get
        End Property

    End Class

    Public Class BlockMovement
        Inherits Packet

#Region "Variables"
        Private _sequence As Byte
        Private _x As UShort
        Private _y As UShort
        Private _direction As UOLite2.Enums.Direction
        Private _z As Byte
#End Region

#Region "Constructors/Packet Breakdown"
        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.BlockMovement)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            buff.Position = 1

            '1
            _sequence = buff.readbyte

            '2-3
            _x = buff.readushort

            '4-5
            _y = buff.readushort

            '6
            _direction = buff.readbyte

            '7
            _z = buff.readbyte

        End Sub
#End Region

#Region "Publicly Accessable Properties"
        Public ReadOnly Property Sequence As Byte
            Get
                Return _sequence
            End Get
        End Property

        Public ReadOnly Property X As UShort
            Get
                Return _x
            End Get
        End Property

        Public ReadOnly Property Y As UShort
            Get
                Return _y
            End Get
        End Property

        Public ReadOnly Property Z As Byte
            Get
                Return _z
            End Get
        End Property

        Public ReadOnly Property Direction As UOLite2.Enums.Direction
            Get
                Return _direction
            End Get
        End Property
#End Region

    End Class

    Public Class AcceptMovement_ResyncRequest
        Inherits Packet

#Region "Variables"
        Private _sequence As Byte
        Private _Reputation As UOLite2.Enums.Reputation
#End Region

#Region "Constructor/Packet Breakdown"
        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.AcceptMovement_ResyncRequest)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            buff.Position = 1

            '1
            _sequence = buff.readbyte

            '2
            _Reputation = buff.readbyte

        End Sub
#End Region

#Region "Publicly Accessable Properties"
        Public ReadOnly Property Sequence As Byte
            Get
                Return _sequence
            End Get
        End Property

        Public ReadOnly Property Reputation As UOLite2.Enums.Reputation
            Get
                Return _Reputation
            End Get
        End Property
#End Region

    End Class

    Public Class Teleport
        Inherits Packet

#Region "Variables"
        Private _serial As Serial
        Private _artwork As UShort
        Private _hue As UShort
        Private _status As UOLite2.Enums.MobileStatus
        Private _x As UShort
        Private _y As UShort
        Private _direction As UOLite2.Enums.Direction
        Private _z As Byte
#End Region

#Region "Constructors/Packet Breakdown"
        Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.Teleport)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            buff.Position = 1

            '1-4
            _serial = buff.readuint

            '5-6
            _artwork = buff.readushort

            '7-8
            _hue = buff.readushort

            '9
            _status = buff.readbyte

            '10-11
            _x = buff.readushort

            '11-12
            _y = buff.readushort

            '13
            _direction = buff.readbyte

            '14
            _z = buff.readbyte

        End Sub
#End Region

#Region "Publicly Accessable Properties"
        Public ReadOnly Property Serial As Serial
            Get
                Return _serial
            End Get
        End Property

        Public ReadOnly Property Hue As UShort
            Get
                Return _hue
            End Get
        End Property

        Public ReadOnly Property Artwork As UShort
            Get
                Return _artwork
            End Get
        End Property

        Public ReadOnly Property Status As UOLite2.Enums.MobileStatus
            Get
                Return _status
            End Get
        End Property

        Public ReadOnly Property X As UShort
            Get
                Return _x
            End Get
        End Property

        Public ReadOnly Property Y As UShort
            Get
                Return _y
            End Get
        End Property

        Public ReadOnly Property Z As Byte
            Get
                Return _z
            End Get
        End Property

        Public ReadOnly Property Direction As UOLite2.Enums.Direction
            Get
                Return _direction
            End Get
        End Property
#End Region

    End Class

#End Region

#Region "Items/Mobiles"

    Public Class Destroy
        Inherits Packet

        Private _serial As Serial

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.Destroy)
            _size = bytes.Length
            _Data = bytes
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                _serial = .readuint
            End With

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _serial
            End Get
        End Property

    End Class

#End Region

#Region "Skills"
    Public Class Skills
        Inherits Packet

        Private _skills(60) As SupportClasses.Skill
        Private _singleSkill As SupportClasses.Skill
        Private _listType As ListTypes

        Friend Sub New(ByVal bytes() As Byte, ByRef Client As LiteClient)
            MyBase.New(UOLite2.Enums.PacketType.Skills)
            _size = bytes.Length
            _Data = bytes
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 3
                _listType = .readbyte

                Select Case _listType
                    Case &H2
                        Dim skillnum As UShort
                        For i As Integer = 4 To _Data.Length - 1 Step 9
                            skillnum = buff.readushort
                            _skills(skillnum) = New SupportClasses.Skill(Client, skillnum) With {._Value = buff.readushort,
                                                                                  ._BaseValue = buff.readushort,
                                                                                  ._Lock = buff.readbyte,
                                                                                  ._Cap = buff.readushort}
                        Next
                    Case &HDF
                        _singleSkill = New SupportClasses.Skill(Client, buff.readushort) With {._Value = buff.readushort,
                                                                              ._BaseValue = buff.readushort,
                                                                              ._Lock = buff.readbyte,
                                                                              ._Cap = buff.readushort}
                    Case &HFF
                        _singleSkill = New SupportClasses.Skill(Client, buff.readushort) With {._Value = buff.readushort,
                                                                              ._BaseValue = buff.readushort,
                                                                              ._Lock = buff.readbyte}

                End Select

            End With

        End Sub

        Public ReadOnly Property Skills As SupportClasses.Skill()
            Get
                Return _skills
            End Get
        End Property

        Public ReadOnly Property SingleSkill As SupportClasses.Skill
            Get
                Return _singleSkill
            End Get
        End Property

        Public ReadOnly Property ListType As ListTypes
            Get
                Return _listType
            End Get
        End Property

        Public Enum ListTypes As Byte
            Basic = 0
            GodView = 1
            BasicWithSkillCap = 2
            GodViewWithSkillCap = 3
            SkillUpdateWithSkillCap = &HDF
            SkillUpdate = &HFF
        End Enum

    End Class
#End Region

#Region "Interface - Targeting, Single/Double Click, Hue Picker, etc..."

    Public Class CharacterList
        Inherits Packet

        Private _CharList As New ArrayList

        Public ReadOnly Property CharacterList As ArrayList
            Get
                Return _CharList
            End Get
        End Property

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.CharacterList)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1

                Dim CharCount As Byte = bytes(3)

                Dim NameBytes(29) As Byte
                Dim PasswordBytes(29) As Byte

                Dim Character As New Structures.CharListEntry

                For i As Integer = 0 To CharCount - 1

                    'Get The Name
                    For s As Integer = i + 4 To i + 33
                        NameBytes(s - (i + 4)) = bytes((i * 60) + s)
                    Next

                    For s As Integer = i + 34 To i + 63
                        PasswordBytes(s - (i + 34)) = bytes((i * 60) + s)
                    Next

                    If NameBytes(0) = 0 Then
                        Exit For
                    Else
                        Character.Name = System.Text.Encoding.ASCII.GetString(NameBytes).Replace(Chr(0), "")
                        Character.Password = System.Text.Encoding.ASCII.GetString(PasswordBytes).Replace(Chr(0), "")
                        Character.Slot = i
                        _CharList.Add(Character)
                        Character = New Structures.CharListEntry
                    End If

                Next

            End With
        End Sub

    End Class

    Public Class TakeObject
        Inherits Packet

        Private _Serial As Serial
        Private _Amount As UShort

        Friend Sub New(ByVal Serial As Serial, ByVal Amount As UShort)
            MyBase.New(UOLite2.Enums.PacketType.TakeObject)
            Dim bytes(6) As Byte
            bytes(0) = 7 'TakeObject Byte
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 1
            buff.writeuint(Serial.Value)
            buff.writeushort(Amount)
        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.TakeObject)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1

                _Serial = .readuint
                _Amount = .readushort
            End With
        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property Amount() As UShort
            Get
                Return _Amount
            End Get
        End Property

    End Class

    Public Class DropObject
        Inherits Packet

        Private _Serial As Serial
        Private _X As UShort
        Private _Y As UShort
        Private _Z As Byte
        Private _Container As Serial

        Friend Sub New(ByVal Serial As Serial, ByVal X As UShort, ByVal Y As UShort, ByVal Z As Byte, ByVal Container As Serial)
            MyBase.New(UOLite2.Enums.PacketType.DropObject)
            Dim bytes(14) As Byte
            bytes(0) = 8 'Drop Object byte

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                .writeuint(Serial.Value)
                .writeushort(X)
                .writeushort(Y)
                .writebyte(Z)

                'Skip the gridindex
                .Position += 1

                .writeuint(Container.Value)
            End With

        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.DropObject)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                _Serial = .readuint
                _X = .readushort
                _Y = .readushort
                _Z = .readbyte

                'Skip the gridindex
                .Position += 1

                _Container = .readuint
            End With

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property X() As UShort
            Get
                Return _X
            End Get
        End Property

        Public ReadOnly Property Y() As UShort
            Get
                Return _Y
            End Get
        End Property

        Public ReadOnly Property Z() As Byte
            Get
                Return _Z
            End Get
        End Property

        Public ReadOnly Property Container() As Serial
            Get
                Return _Container
            End Get
        End Property
    End Class

    Public Class Doubleclick
        Inherits Packet
        Private _Serial As New Serial(CUInt(0))

        Friend Sub New()
            MyBase.New(UOLite2.Enums.PacketType.DoubleClick)
            Dim bytes(4) As Byte
            bytes(0) = CByte(6)
            _Data = bytes
            _size = 5

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.DoubleClick)
            _Data = bytes
            _size = bytes.Length

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                _Serial = .readuint
            End With

        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = value
                buff.Position = 1
                buff.writeuint(value.Value)
            End Set
        End Property

        Public Overrides ReadOnly Property Data() As Byte()
            Get
                Return _Data
            End Get
        End Property

    End Class

    Public Class Singleclick
        Inherits Packet
        Private _Serial As New Serial(CUInt(0))

        Friend Sub New()
            MyBase.New(UOLite2.Enums.PacketType.SingleClick)
            Dim bytes(4) As Byte
            bytes(0) = CByte(9)
            _Data = bytes
            _size = 5

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.SingleClick)
            _Data = bytes
            _size = bytes.Length

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                _Serial = .readuint
            End With

        End Sub

        Public Property Serial() As Serial
            Get
                Return _Serial
            End Get
            Set(ByVal value As Serial)
                _Serial = Serial
                buff.Position = 1
                buff.writeuint(value.Value)
            End Set
        End Property

        Public Overrides ReadOnly Property Data() As Byte()
            Get
                Return _Data
            End Get
        End Property

    End Class

    Public Class HuePicker
        Inherits Packet

        Private _Serial As Serial
        Private _Artwork As UShort = 0
        Private _Hue As UShort

        Friend Sub New(ByVal Serial As Serial, ByVal Artwork As UShort, ByVal Hue As UShort)
            MyBase.New(UOLite2.Enums.PacketType.HuePicker)
            Dim bytes(8) As Byte

            bytes(0) = 149

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            buff.Position = 1
            buff.writeuint(Serial.Value)
            buff.writeushort(Artwork)
            buff.writeushort(Hue)

        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.HuePicker)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            With buff
                .Position = 1
                _Serial = .readuint
                _Artwork = .readushort
                _Hue = .readushort
            End With
        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property Artwork() As UShort
            Get
                Return _Artwork
            End Get
        End Property

        Public ReadOnly Property Hue() As UShort
            Get
                Return _Hue
            End Get
        End Property

    End Class

    Public Class LoginComplete
        Inherits Packet

        'The simplist packet ever...  1 byte.
        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.LoginComplete)
        End Sub
    End Class

#End Region

#Region "Base Classes"
    Public Class GenericCommand
        Inherits Packet

        Private _SubCommand As UShort = 0

        Friend Sub New(ByVal Subcommand As UOLite2.Enums.BF_Sub_Commands)
            MyBase.New(UOLite2.Enums.PacketType.GenericCommand)

            _SubCommand = Subcommand
        End Sub

        Public ReadOnly Property SubCommand() As UOLite2.Enums.BF_Sub_Commands
            Get
                Return _SubCommand
            End Get
        End Property

    End Class

#End Region

#Region "Context Menu Stuff"
    Public Class ContextMenuRequest
        Inherits GenericCommand

        Private _Serial As Serial

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.BF_Sub_Commands.ContextMenuRequest)
            _Data = bytes

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 5
            _Serial = buff.readuint

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

    End Class

    Public Class ContextMenuResponse
        Inherits GenericCommand

        Private _Serial As Serial
        Private _Index As UShort = 0

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.BF_Sub_Commands.ContextMenuResponse)
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 5
            _Serial = buff.readuint

            If buff.Position <> buff.buffer.Length - 1 Then
                _Index = buff.readushort
            End If

        End Sub

        Public ReadOnly Property Serial() As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property Index() As UShort
            Get
                Return _Index
            End Get
        End Property

    End Class

    '''' <summary>Displays a context menu for the 2D client.</summary>
    'Public Class DisplayContextMenu
    ' Inherits GenericCommand
    '
    'Private _Serial As Serial
    'Private _NumberOfOptions As Byte
    'Private _ContextMenu As New ContextMenu
    'Private _Number As UShort = 0
    'Private _Flags As UShort = 0
    'Private _Hue As UShort = 0
    '
    '    Friend Sub New(ByVal Menu As ContextMenu, ByVal Serial As Serial)
    '        MyBase.New(UOLite2.Enums.BF_Sub_Commands.DisplayContextMenu)
    '        _Serial = Serial
    '
    '    'Calculate the number of bytes needed.
    '    Dim k As UInteger = 12
    '
    '        For Each j As ContextMenu.ContextMenuOption In Menu
    '            If j.Hue <> 0 Then
    '                k += 6
    '            Else
    '                k += 8
    '            End If
    '        Next
    '
    '    'Make the byte array.
    '    Dim bytes(k) As Byte
    '
    '        buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
    '
    '        With buff
    '            .Position = 0
    '            .writebyte(191)
    '
    '            .networkorder = False
    '            .writeushort(k + 1)
    '            .networkorder = True
    '
    '            .writeushort(20)
    '
    '            .writeushort(1)
    '            .writeuint(_Serial.Value)
    '            .writebyte(Menu.Count)
    '
    '            For Each cmo As ContextMenu.ContextMenuOption In Menu
    '                _Flags = 0
    '
    '                .writeushort(cmo.Index)
    '
    '                If cmo.Enabled = False Then _Flags += 1
    '                If cmo.Hue <> 0 Then _Flags += 32
    '
    '                .writeushort(CUShort(cmo.CliLocNumber - 3000000))
    '
    '                .writeushort(_Flags)
    '
    '                If cmo.Hue <> 0 Then .writeushort(cmo.Hue)
    '            Next
    '
    '        End With
    '
    '    End Sub
    '
    '    Friend Sub New(ByVal bytes() As Byte)
    '    MyBase.New(UOLite2.Enums.BF_Sub_Commands.DisplayContextMenu)
    '    buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
    '
    '        With buff
    '            .Position = 7
    '            _Serial = .readuint
    '
    '            buff.networkorder = False
    '            _NumberOfOptions = .readbyte
    '            buff.networkorder = True
    '
    '            For i As Integer = 0 To _NumberOfOptions - 1
    '                .Position += 2
    '                _Number = .readushort
    '                _Flags = .readushort
    '
    '                Select Case _Flags
    '                    Case 33 'Diabled + Colored
    '                        _Hue = .readushort
    '                        _ContextMenu.Add(CUInt(3000000 + _Number), _Hue, False)
    '                    Case 32 'Enabled + Colored
    '                        _Hue = .readushort
    '                        _ContextMenu.Add(CUInt(3000000 + _Number), _Hue)
    '                    Case 1 'Disabled
    '                        _ContextMenu.Add(CUInt(3000000 + _Number), False)
    '                    Case 0 'Enabled
    '                        _ContextMenu.Add(CUInt(3000000 + _Number))
    '                End Select
    '            Next
    '
    '        End With
    '    End Sub
    '    '
    '    '    Public ReadOnly Property Serial() As Serial
    '        Get
    '            Return _Serial
    '        End Get
    '    End Property
    '
    '    Public ReadOnly Property Menu() As ContextMenu
    '        Get
    '            Return _ContextMenu
    '        End Get
    '    End Property
    '
    '    End Class
#End Region

#Region "Gumps"
    Public Class CompressedGump
        Inherits Packet

        Private _Serial As Serial
        Private _GumpID As UInteger
        Private _X As UInteger
        Private _Y As UInteger
        Private _CompressedGumpLayoutLength As UInteger
        Private _DecompressedGumpLayoutLength As UInteger
        Private _CompressedGumpData() As Byte
        Private _NumberOfLines As UInteger
        Private _CompressedTextLineLength As UInteger
        Private _DecompressedTextLineLength As UInteger
        Private _CompressedTextData() As Byte

        Private _DecompressedGumpData As String = ""
        Private _DecompressedTextData As String = ""

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.CompressedGump)
            _Data = bytes

            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

            buff.Position = 3

            Dim Decompress As System.IO.Compression.DeflateStream

            With buff
                _Serial = New Serial(.readuint)
                _GumpID = .readuint
                _X = .readuint
                _Y = .readuint
                _CompressedGumpLayoutLength = .readuint
                _DecompressedGumpLayoutLength = .readuint
                .Position += 2
                .networkorder = False
                _CompressedGumpData = .readbytes(_CompressedGumpLayoutLength - 6)
                .networkorder = True

                Decompress = New System.IO.Compression.DeflateStream(New MemoryStream(_CompressedGumpData), System.IO.Compression.CompressionMode.Decompress)
                Dim decom As New StreamReader(Decompress, Encoding.ASCII, False)

                _DecompressedGumpData = decom.ReadToEnd.Replace("}", "}" & vbNewLine)

                _NumberOfLines = .readuint

                If _NumberOfLines = 0 Then Exit Sub

                _CompressedTextLineLength = .readuint
                _DecompressedTextLineLength = .readuint

                .Position += 2
                .networkorder = False
                _CompressedTextData = .readbytes(_CompressedTextLineLength)
                .networkorder = True

                Decompress = New System.IO.Compression.DeflateStream(New System.IO.MemoryStream(_CompressedTextData), System.IO.Compression.CompressionMode.Decompress)
                decom = New StreamReader(Decompress, Encoding.BigEndianUnicode, False)

                Dim bytess(_DecompressedTextLineLength) As Byte
                Decompress.Read(bytess, 0, _DecompressedTextLineLength)
                Dim builder As New System.Text.StringBuilder()
                Dim readcount As Integer

                Dim x As New UOLite2.SupportClasses.BufferHandler(bytess)
                x.Position = 1

                Do
                    readcount = x.readushort
                    builder.AppendLine(x.readustrn(readcount))
                Loop Until x.Position >= bytess.Length - 1

                _DecompressedTextData = builder.ToString

            End With

        End Sub

        Public ReadOnly Property DecompressedGumpData As String
            Get
                Return _DecompressedGumpData
            End Get
        End Property

        Public ReadOnly Property DecompressedTextData As String
            Get
                Return _DecompressedTextData
            End Get
        End Property

        Public ReadOnly Property Serial As Serial
            Get
                Return _Serial
            End Get
        End Property

        Public ReadOnly Property GumpID As UInteger
            Get
                Return _GumpID
            End Get
        End Property

        Public ReadOnly Property X As UInteger
            Get
                Return _X
            End Get
        End Property

        Public ReadOnly Property Y As UInteger
            Get
                Return _Y
            End Get
        End Property

    End Class

#End Region

End Namespace


Namespace SupportClasses
#Region "BufferHandler"
    'Only show for debugging, hide for releases.
#If DEBUG Then
    'Buffer Serialization and Deserialization
    Public Class BufferHandler
#Else
    'Buffer Serialization and Deserialization
    ''' Hide this class from the user, there is no reason from him/her to see it.
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
    Public Class UOLite2.SupportClasses.BufferHandler
#End If
        Inherits Stream
        Public curpos As Long
        Private m_buffer As Byte()
        Public networkorder As Boolean

#Region "constructors"
        Public Sub New(ByVal frombuffer As Byte(), ByVal bNetworkOrder As Boolean)
            m_buffer = frombuffer
            networkorder = bNetworkOrder
            curpos = 0
        End Sub
        Public Sub New(ByVal size As UInt32)
            m_buffer = New Byte(size - 1) {}
            networkorder = False
            curpos = 0
        End Sub
        Public Sub New(ByVal size As Integer)
            m_buffer = New Byte(size - 1) {}
            networkorder = False
            curpos = 0
        End Sub
        Public Sub New(ByVal frombuffer As Byte())
            m_buffer = frombuffer
            curpos = 0
            networkorder = False
        End Sub
        Public Sub New(ByVal size As UInt32, ByVal bNetworkOrder As Boolean)
            m_buffer = New Byte(size - 1) {}
            curpos = 0
            networkorder = bNetworkOrder
        End Sub
#End Region

        Public Property buffer() As Byte()
            Get
                Return m_buffer
            End Get
            Set(ByVal value As Byte())
                m_buffer = value
            End Set
        End Property

        Default Public Property Item(ByVal index As Integer) As Byte
            Get
                If (m_buffer IsNot Nothing) AndAlso (m_buffer.Length > index) Then
                    Return m_buffer(index)
                Else
                    Return 0
                End If
            End Get
            Set(ByVal value As Byte)
                If m_buffer IsNot Nothing Then
                    If m_buffer.Length > index Then
                        m_buffer(index) = value
                    End If
                End If
            End Set
        End Property

#Region "Stream members"

        Public Overloads Overrides ReadOnly Property CanRead() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overloads Overrides ReadOnly Property CanSeek() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overloads Overrides ReadOnly Property CanWrite() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overloads Overrides Sub Flush()
            Throw New NotImplementedException()
        End Sub

        Public Overloads Overrides ReadOnly Property Length() As Long
            Get
                Return (m_buffer.Length - curpos)
            End Get
        End Property

        Public Overloads Overrides Property Position() As Long
            Get
                Return curpos
            End Get
            Set(ByVal value As Long)
                curpos = value
            End Set
        End Property

        Public Overloads Overrides Function Read(ByVal destbuffer As Byte(), ByVal offset As Integer, ByVal count As Integer) As Integer
            Dim i As Integer = 0
            For i = 0 To count - 1
                If curpos < m_buffer.Length Then
                    If networkorder Then
                        destbuffer(offset + count - 1 - i) = m_buffer(curpos)
                    Else
                        destbuffer(offset + i) = m_buffer(curpos)
                    End If
                    curpos += 1
                Else
                    Exit For
                End If
            Next
            Return i
        End Function

        Public Overloads Overrides Function Seek(ByVal offset As Long, ByVal origin As SeekOrigin) As Long
            Select Case origin
                Case SeekOrigin.Begin
                    If offset < m_buffer.Length Then
                        curpos = offset
                    End If
                    Exit Select
                Case SeekOrigin.Current
                    If (curpos + offset) < m_buffer.Length Then
                        curpos += offset
                    End If
                    Exit Select
                Case SeekOrigin.[End]
                    If offset < m_buffer.Length Then
                        curpos = m_buffer.Length - 1 - offset
                    End If
                    Exit Select
                Case Else
                    Exit Select
            End Select
            Return curpos
        End Function

        Public Overloads Overrides Sub SetLength(ByVal value As Long)
            Throw New NotImplementedException()
        End Sub

        Public Overloads Overrides Sub Write(ByVal destbuffer As Byte(), ByVal offset As Integer, ByVal count As Integer)
            Dim i As Integer = 0

            If (m_buffer Is Nothing) OrElse (count > Length) Then
                Throw New Exception("Not enough space on buffer!")
            End If

            For i = 0 To count - 1
                If curpos < m_buffer.Length Then
                    If networkorder Then
                        m_buffer(curpos) = destbuffer(offset + count - 1 - i)
                    Else
                        m_buffer(curpos) = destbuffer(offset + i)
                    End If
                    curpos += 1
                Else
                    Exit For
                End If
            Next
            Exit Sub
        End Sub

#End Region

#Region "reading"

        Public Function readbytes(ByVal count As Integer) As Byte()
            Dim targetbuffer As Byte() = New Byte(count - 1) {}
            Read(targetbuffer, 0, count)
            Return targetbuffer
        End Function

#Region "Integer/UInt32"
        Public Function readuint() As UInt32
            Return BitConverter.ToUInt32(readbytes(4), 0)
        End Function

        Public Function readint() As Integer
            Return BitConverter.ToInt32(readbytes(4), 0)
        End Function
#End Region

#Region "Short/UShort"
        Public Function readushort() As UShort
            Return BitConverter.ToUInt16(readbytes(2), 0)
        End Function

        Public Function readshort() As Short
            Return BitConverter.ToInt16(readbytes(2), 0)
        End Function
#End Region

#Region "Byte/Character"

        Public Function readchar() As Byte
            If Length > 0 Then
                curpos = curpos + 1
                Return m_buffer(curpos - 1)
            Else
                Return 0
            End If
        End Function

        Public Shadows Function readbyte() As Byte
            If Length > 0 Then
                curpos = curpos + 1
                Return m_buffer(curpos - 1)
            Else
                Return 0
            End If
        End Function
#End Region

#Region "ASCII Strings"
        Public Function readstr() As String
            Dim prevpos As Long = curpos
            Dim count As Integer = 1

            While (readbyte() > 0) And (Length > 0)
                count += 1
            End While

            curpos = prevpos

            Return readstrn(count - 1)
        End Function

        Public Function readstrn(ByVal size As Integer) As String
            Dim characterarray As Char() = New Char(size - 1) {}
            For i As Integer = 0 To size - 1
                characterarray(i) = Chr(readbyte())
            Next
            Return New String(characterarray, 0, size)
        End Function
#End Region

#Region "Unicode Strings"

        Public Function readustr() As String

            Dim prevpos As Long = curpos
            Dim count As Integer = 1

            While (readushort() > 0) And (Length > 0)
                count += 1
            End While

            curpos = prevpos

            Return readustrn(count - 1)
        End Function

        Public Function readustrn(ByVal size As Integer) As String

            Dim characterarray As Char() = New Char(size - 1) {}

            For i As Integer = 0 To size - 1
                characterarray(i) = ChrW(readushort())
            Next

            Return New String(characterarray, 0, size)
        End Function
#End Region

#End Region

#Region "writing"

        Public Sub writeuint(ByVal towrite As UInt32)
            Write(BitConverter.GetBytes(towrite), 0, 4)
        End Sub

        Public Sub writeint(ByVal towrite As Integer)
            Write(BitConverter.GetBytes(towrite), 0, 4)
        End Sub

        Public Sub writeushort(ByVal towrite As UShort)
            Write(BitConverter.GetBytes(towrite), 0, 2)
        End Sub

        Public Sub writeshort(ByVal towrite As Short)
            Write(BitConverter.GetBytes(towrite), 0, 2)
        End Sub

        Public Overrides Sub writebyte(ByVal towrite As Byte)
            If Length > 0 Then
                m_buffer(curpos) = towrite
                curpos += 1
            End If
        End Sub

        Public Sub writechar(ByVal towrite As Byte)
            If Length > 0 Then
                m_buffer(curpos) = CByte(towrite)
                curpos += 1
            End If
        End Sub

        Public Sub writestr(ByVal towrite As String)
            Dim strbytes As Byte() = ASCIIEncoding.ASCII.GetBytes(towrite)
            For i As Integer = 0 To strbytes.Length - 1
                writebyte(strbytes(i))
            Next
            If strbytes(strbytes.Length - 1) <> 0 Then
                'ensure '\0'-termination
                writebyte(0)
            End If
        End Sub

        Public Sub writestrn(ByVal towrite As String, ByVal length As Integer)
            Dim strbytes As Byte() = ASCIIEncoding.ASCII.GetBytes(towrite)
            For i As Integer = 0 To length - 1
                If i < strbytes.Length Then
                    writebyte(strbytes(i))
                Else
                    writebyte(0)
                End If
            Next
        End Sub

        Public Sub writeustr(ByVal towrite As String)
            Dim strbytes As Char() = towrite.ToCharArray()

            For i As Integer = 0 To strbytes.Length - 1
                writeushort(BitConverter.GetBytes(strbytes(i))(0))
            Next

            If strbytes.Length = 0 Then
                Dim zerobyte() As Char = {ChrW(0)}
                strbytes = zerobyte
            Else
                If BitConverter.GetBytes(strbytes(strbytes.Length - 1))(0) <> 0 Then
                    'ensure '\0'-termination
                    writeushort(0)
                End If
            End If

        End Sub

        Public Sub writeustrn(ByVal towrite As String, ByVal length As Integer)
            Dim strbytes As Char() = towrite.ToCharArray()
            For i As Integer = 0 To length - 1
                If i < strbytes.Length Then
                    writeushort(BitConverter.GetBytes(strbytes(i))(0))
                Else
                    writeushort(0)
                End If
            Next
        End Sub

#End Region

    End Class

#End Region
End Namespace

Namespace Enums

    ''' <summary>
    ''' Reason enumeration for "Get Item Failed" packet. (0x27)
    ''' </summary>
    Public Enum GetItemFailedReason
        ''' <summary>Displays "You cannot pick that up."</summary>
        CannotPickup

        ''' <summary>Displays "That is too far away."</summary>
        TooFar

        ''' <summary>Displays "That is out of sight."</summary>
        OutOfSight

        ''' <summary>Displays "That item does not belong to you. You will have to steal it."</summary>
        DoesntBelongToYou

        ''' <summary>Displays "You are already holding an item."</summary>
        AlreadyHoldingItem

        ''' <summary>Tells the client to delete the item from its cache.</summary>
        Cmd_DestroyTheItem

        ''' <summary>Displays no message, just doesn't let you pick it up.</summary>
        NoMessage
    End Enum

    Public Enum BF_Sub_Commands As UShort
        FastWalk = &H1
        AddWalkKey = &H2
        CloseGump = &H4
        ScreenSize = &H5
        Party = &H6
        QuestArrow = &H7
        MapChange = &H8
        DisarmRequest = &H9
        StunRequest = &HA
        ClientLanguage = &HB
        CloseStatus = &HC
        Animate = &HE
        UnknownEmpty = &HF
        DisplayEquipmentInfo = &H10
        ContextMenuRequest = &H13
        DisplayContextMenu = &H14
        ContextMenuResponse = &H15
        DisplayHelpTopics = &H17
        EnableMapDiffs = &H18
        MiscellaneousStatus = &H19
        StatLockChange = &H1A
        NewSpellBookContent = &H1B
        CastSpellLastSpell = &H1C
        DesignHouse = &H1D
        QueryDesignDetails = &H1D
        HouseCustomization = &H20
        ClearWeaponAbility = &H21
        DamagePacket = &H22
        KeepAlive = &H24
        EnableDisableSESpellIcons = &H25
        SetSpeedModeForMovement = &H26
        ChangeRaceRequestAndResponse = &H2A
        SetMobileAnimation = &H2B
        UseTargetedItem = &H2C
        CastTargetedSpell = &H2D
        UseTargetedSkill = &H2E

    End Enum

    Public Enum BF_06_Sub_Commands As UShort
        AddMember_DisplayMenberList = &H1
        RemoveMember = &H2
        PartyPrivateMessage = &H3
        PartyChat = &H4
        PartyLoot = &H6
        PartyInvitation = &H7
        AcceptRequest = &H8
        DeclineRequest = &H9
    End Enum

    Public Enum BF_20_Sub_Commands As UShort
        BeginHouseCustomization = &H4
        EndHouseCustomization = &H5
    End Enum

    Public Enum BF_19_Sub_Commands As UShort
        BondedStatus = &H0
        StatLockInfo = &H2
        UpdateMobileStatusInformation = &H5
    End Enum

    ''' <summary>
    ''' Packet type enumeration.
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum PacketType As Byte
        CharacterCreation = &H0
        Logout = &H1
        RequestMovement = &H2
        Speech = &H3
        RequestGodMode = &H4
        Attack = &H5
        DoubleClick = &H6
        TakeObject = &H7
        DropObject = &H8
        SingleClick = &H9
        Edit = &HA
        EditArea = &HB
        TileData = &HC
        NPCData = &HD
        EditTemplateData = &HE
        Paperdoll_Old = &HF
        HueData = &H10
        MobileStats = &H11
        GodCommand = &H12
        EquipItemRequest = &H13
        ChangeElevation = &H14
        Follow = &H15
        RequestScriptNames = &H16
        HealthBarStatusUpdate = &H17
        ScriptAttach = &H18
        NPCConversationData = &H19
        ShowItem = &H1A
        LoginConfirm = &H1B
        Text = &H1C
        Destroy = &H1D
        Animate = &H1E
        Explode = &H1F
        Teleport = &H20
        BlockMovement = &H21
        AcceptMovement_ResyncRequest = &H22
        DragItem = &H23
        OpenContainer = &H24
        ObjecttoObject = &H25
        OldClient = &H26
        GetItemFailed = &H27
        DropItemFailed = &H28
        DropItemOK = &H29
        Blood = &H2A
        GodMode = &H2B
        Death = &H2C
        Health = &H2D
        EquipItem = &H2E
        Swing = &H2F
        AttackOK = &H30
        AttackEnd = &H31
        HackMover = &H32
        Group = &H33
        ClientQuery = &H34
        ResourceType = &H35
        ResourceTileData = &H36
        MoveObject = &H37
        FollowMove = &H38
        Groups = &H39
        Skills = &H3A
        AcceptOffer = &H3B
        ContainerContents = &H3C
        Ship = &H3D
        Versions = &H3E
        UpdateStatics = &H3F
        UpdateTerrain = &H40
        UpdateTiledata = &H41
        UpdateArt = &H42
        UpdateAnim = &H43
        UpdateHues = &H44
        VerOK = &H45
        NewArt = &H46
        NewTerrain = &H47
        NewAnim = &H48
        NewHues = &H49
        DestroyArt = &H4A
        CheckVer = &H4B
        ScriptNames = &H4C
        ScriptFile = &H4D
        LightChange = &H4E
        Sunlight = &H4F
        BoardHeader = &H50
        BoardMessage = &H51
        PostMessage = &H52
        LoginReject = &H53
        Sound = &H54
        LoginComplete = &H55
        MapCommand = &H56
        UpdateRegions = &H57
        NewRegion = &H58
        NewContextFX = &H59
        UpdateContextFX = &H5A
        GameTime = &H5B
        RestartVer = &H5C
        PreLogin = &H5D
        ServerList_Olsolete = &H5E
        AddServer = &H5F
        ServerRemove = &H60
        DestroyStatic = &H61
        MoveStatic = &H62
        AreaLoad = &H63
        AreaLoadRequest = &H64
        WeatherChange = &H65
        BookContents = &H66
        SimpleEdit = &H67
        ScriptLSAttach = &H68
        Friends = &H69
        FriendNotify = &H6A
        KeyUse = &H6B
        Target = &H6C
        Music = &H6D
        Animation = &H6E
        Trade = &H6F
        Effect = &H70
        BulletinBoard = &H71
        Combat = &H72
        Ping = &H73
        ShopData = &H74
        RenameMOB = &H75
        ServerChange = &H76
        NakedMOB = &H77
        EquippedMOB = &H78
        ResourceQuery = &H79
        ResourceData = &H7A
        Sequence = &H7B
        ObjectPicker = &H7C
        PickedObject = &H7D
        GodViewQuery = &H7E
        GodViewData = &H7F
        AccountLoginRequest = &H80
        AccountLoginOK = &H81
        AccountLoginFailed = &H82
        AccountDeleteCharacter = &H83
        ChangeCharacterPassword = &H84
        DeleteCharacterFailed = &H85
        AllCharacters = &H86
        SendResources = &H87
        OpenPaperdoll = &H88
        CorpseEquipment = &H89
        TriggerEdit = &H8A
        DisplaySign = &H8B
        ServerRedirect = &H8C
        Unused3 = &H8D
        MoveCharacter = &H8E
        Unused4 = &H8F
        OpenCourseGump = &H90
        PostLogin = &H91
        UpdateMulti = &H92
        BookHeader = &H93
        UpdateSkill = &H94
        HuePicker = &H95
        GameCentralMonitor = &H96
        MovePlayer = &H97
        MOBName = &H98
        TargetMulti = &H99
        TextEntry = &H9A
        RequestAssistance = &H9B
        AssistRequest = &H9C
        GMSingle = &H9D
        ShopSell = &H9E
        ShopOffer = &H9F
        ServerSelect = &HA0
        HPHealth = &HA1
        ManaHealth = &HA2
        FatHealth = &HA3
        HardwareInfo = &HA4
        WebBrowser = &HA5
        Message = &HA6
        RequestTip = &HA7
        ServerList = &HA8
        CharacterList = &HA9
        CurrentTarget = &HAA
        StringQuery = &HAB
        StringResponse = &HAC
        SpeechUnicode = &HAD
        TextUnicode = &HAE
        DeathAnimation = &HAF
        GenericGump = &HB0
        GenericGumpTrigger = &HB1
        ChatMessage = &HB2
        ChatText = &HB3
        TargetObjectList = &HB4
        OpenChat = &HB5
        HelpRequest = &HB6
        HelpText = &HB7
        CharacterProfile = &HB8
        Features = &HB9
        Pointer = &HBA
        AccountID = &HBB
        GameSeason = &HBC
        ClientVersion = &HBD
        AssistVersion = &HBE
        GenericCommand = &HBF
        HuedFX = &HC0
        LocalizedText = &HC1
        UnicodeTextEntry = &HC2
        GlobalQueue = &HC3
        Semivisible = &HC4
        InvalidMap = &HC5
        InvalidMapEnable = &HC6
        ParticleEffect = &HC7
        ChangeUpdateRange = &HC8
        TripTime = &HC9
        UTripTime = &HCA
        GlobalQueueCount = &HCB
        LocalizedTextPlusString = &HCC
        UnknownGodPacket = &HCD
        IGRClient = &HCE
        IGRLogin = &HCF
        IGRConfiguration = &HD0
        IGRLogout = &HD1
        UpdateMobile = &HD2
        ShowMobile = &HD3
        BookInfo = &HD4
        UnknownClientPacket = &HD5
        MegaCliloc = &HD6
        AOSCommand = &HD7
        CustomHouse = &HD8
        Metrics = &HD9
        Mahjong = &HDA
        CharacterTransferLog = &HDB
        CompressedGump = &HDD
    End Enum

End Namespace