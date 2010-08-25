Partial Class LiteClient

    Protected Friend _Targeting As Boolean = False
    Protected Friend _TargetUID As UInteger
    Protected Friend _TargetType As Byte
    Protected Friend _TargetFlag As Byte

    Public Event onTargetRequest(ByRef Client As LiteClient)

    ''' <summary>
    ''' Whether or not the server is requesting a target from the client, setting it to false will tell the server that you cancelled your target.
    ''' </summary>
    Public Property Targeting As Boolean
        Get
            Return _Targeting
        End Get
        Set(ByVal value As Boolean)
            If _Targeting And Not value Then
                Dim tpacket As New Packets.Target

                tpacket.TargetType = Enums.TargetActionType.Cancel
                tpacket.Serial = _TargetUID
                tpacket.Flag = _TargetFlag
                tpacket.Target = LiteClient.ZeroSerial

                Send(tpacket)
                _Targeting = value
            End If
        End Set
    End Property

    ''' <summary>
    ''' Gets or Sets the LastTarget.
    ''' </summary>
    Public Property LastTarget As Serial

    ''' <summary>
    ''' The type of target requested, either Ground or Object.
    ''' </summary>
    Public ReadOnly Property TargetType As Enums.TargetRequestType
        Get
            Return _TargetType
        End Get
    End Property

    ''' <summary>
    ''' The result of your target action on your target. Such as the target will be harmed or healed.
    ''' </summary>
    Public ReadOnly Property TargetFlag As Enums.TargetActionType
        Get
            Return _TargetFlag
        End Get
    End Property

    Private Sub HandleTargetPacket(ByRef Packet As Packets.Target)
        _Targeting = True
        _TargetUID = Packet.Serial
        _TargetType = Packet.TargetType
        _TargetFlag = Packet.Flag
        RaiseEvent onTargetRequest(Me)
    End Sub

    ''' <summary>
    ''' Responds to a target request from the server.
    ''' </summary>
    ''' <param name="Serial">The serial of the object to be targeted.</param>
    Public Overloads Sub Target(ByVal Serial As Serial)
        If Not Targeting Then Exit Sub
        Dim tpacket As New Packets.Target

        tpacket.TargetType = _TargetType
        tpacket.Serial = _TargetUID
        tpacket.Flag = _TargetFlag
        tpacket.Target = Serial

        Send(tpacket)

        _LastTarget = Serial
    End Sub

    ''' <summary>
    ''' Responds to a target request from the server.
    ''' </summary>
    ''' <param name="X">The X position to target.</param>
    ''' <param name="Y">The Y position to target.</param>
    ''' <param name="Z">The Z position to target.</param>
    ''' <param name="Graphic">The graphic of the tile, if the tile is a static tile, its 0 if it is a ground/map tile.</param>
    Public Overloads Sub Target(ByVal X As UShort, ByVal Y As UShort, ByVal Z As UShort, Optional ByVal Graphic As UShort = 0)
        If Not Targeting Then Exit Sub
        Dim tpacket As New Packets.Target

        tpacket.TargetType = _TargetType
        tpacket.Serial = _TargetUID
        tpacket.Flag = _TargetFlag
        tpacket.X = X
        tpacket.Y = Y
        tpacket.Z = Z

        Send(tpacket)
    End Sub


End Class

Partial Class Item

    ''' <summary>
    ''' Target's the object, if the server has requested a target.
    ''' </summary>
    Public Sub Target()
        _Client.Target(_Serial)
    End Sub

End Class

Namespace Packets
    Public Class Target
        Inherits Packet

        Friend _TargetType As Byte = 0 'Object target
        Friend _Serial As UInt32
        Friend _Flag As Byte
        Friend _Target As Serial
        Friend _X As UShort = 0
        Friend _Y As UShort = 0
        Friend _Z As UShort = 0
        Friend _Artwork As UShort

        Friend Sub New()
            MyBase.New(UOLite2.Enums.PacketType.Target)
            Dim bytes(19) As Byte
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)
            buff.Position = 0
            'Write the packet type information
            buff.writebyte(CByte(108))
        End Sub

        Friend Sub New(ByVal bytes() As Byte)
            MyBase.New(UOLite2.Enums.PacketType.Target)
            _Data = bytes
            _size = bytes.Length
            buff = New UOLite2.SupportClasses.BufferHandler(bytes, True)

#If DebugTargeting Then
                Console.WriteLine("Recieved Target Packet: " & BitConverter.ToString(bytes))
#End If

            With buff
                .Position = 1
                '1-1
                _TargetType = .readbyte
                '2-5
                _Serial = New Serial(.readuint)
                '6-6
                _Flag = .readbyte
                '7-10
                _Target = New Serial(.readuint)
                '11-12
                _X = .readushort
                '13-14
                _Y = .readushort
                '15-16
                _Z = .readushort
                '17-18
                _Artwork = .readushort
            End With
        End Sub

        Public Property TargetType() As Byte
            Get
                Return _TargetType
            End Get
            Set(ByVal Value As Byte)
                _TargetType = Value
                buff.Position = 1
                buff.writebyte(Value)
            End Set
        End Property

        Public Property Serial() As UInt32
            Get
                Return _Serial
            End Get
            Set(ByVal Value As UInt32)
                _Serial = Value
                buff.Position = 2
                buff.writeuint(Value)
            End Set
        End Property

        Public Property Flag() As Byte
            Get
                Return _Flag
            End Get
            Set(ByVal Value As Byte)
                _Flag = Value
                buff.Position = 6
                buff.writebyte(Value)
            End Set
        End Property

        Public Property Target() As Serial
            Get
                Return _Target
            End Get
            Set(ByVal Value As Serial)
                _Target = Value
                buff.Position = 7
                buff.writeuint(Value.Value)
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

        Public Property Z() As UShort
            Get
                Return _Z
            End Get
            Set(ByVal Value As UShort)
                _Z = Value
                buff.Position = 15
                buff.writeushort(Value)
            End Set
        End Property

        Public Property Artwork() As UShort
            Get
                Return _Artwork
            End Get
            Set(ByVal Value As UShort)
                _Artwork = Value
                buff.Position = 17
                buff.writeushort(Value)
            End Set
        End Property

    End Class
End Namespace

Namespace Enums

    Public Enum TargetRequestType
        TargetObject
        TargetGround
    End Enum

    Public Enum TargetActionType
        Neutral
        Harmful
        Helpful
        Cancel
    End Enum

End Namespace