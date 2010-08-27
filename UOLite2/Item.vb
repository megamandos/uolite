'This is the class that represents an in-game item.


Public Class Item

#Region "Constructor"
    Friend Sub New(ByVal client As LiteClient, ByVal Serial As Serial)
        Me._Serial = Serial
        _Client = client
    End Sub

    ''' <summary>
    ''' When using this new, never ever ever try to access the contents!
    ''' </summary>
    ''' <remarks></remarks>
    Friend Sub New()
    End Sub
#End Region

#Region "Variables"

    Friend _Client As LiteClient
    Friend _Serial As New Serial(0)
    Friend _Type As UShort
    Friend _Layer As UOLite2.Enums.Layers
    Friend _StackID As Byte = 0
    Friend _Amount As UShort = 1
    Friend _X As UShort = 0
    Friend _Y As UShort = 0
    Friend _Z As Byte = 0
    Friend _GridIndex As Byte = 0
    Friend _Contents As ItemDatabase.ContentsList = Nothing
    Friend _Container As Serial = LiteClient.WorldSerial
    Friend _Hue As UShort = 0
    Friend _Direction As UOLite2.Enums.Direction = UOLite2.Enums.Direction.North
    Friend _IsMobile As Boolean = False
    Friend _Properties As New SupportClasses.PropertyListClass(Me)
    Friend _ToolTipRevisionHash As UInt32 = 0

#End Region

#Region "Properties"

    Public ReadOnly Property Properties As SupportClasses.PropertyListClass
        Get
            Return _Properties
        End Get
    End Property


    ''' <summary>The serial of the item.</summary>
    Public ReadOnly Property Serial() As Serial
        Get
            Return _Serial
        End Get
    End Property

    ''' <summary>The artwork number of that item. This is what determines what it looks like in game.</summary>
    Public ReadOnly Property Type() As UShort
        Get
            Return _Type
        End Get
    End Property

    ''' <summary>The number to add the the artwork number to get the artwork number of the item if it is a stack. 
    ''' Usualy this is 0x01.</summary>
    Public ReadOnly Property StackID() As Byte
        Get
            Return _StackID
        End Get
    End Property

    ''' <summary>The number of objects in a stack.</summary>
    Public ReadOnly Property Amount() As UShort
        Get
            Return _Amount
        End Get
    End Property

    ''' <summary>The location of the item on the X axis. If the item is inside of a container, 
    ''' this represents the number of pixels within the container from the left side at which 
    ''' the item will be placed.</summary>
    Public ReadOnly Property X() As UShort
        Get
            Return _X
        End Get
    End Property

    ''' <summary>The location of the item on the Y axis. If the item is inside of a container, 
    ''' this represents the number of pixels from the top of the container that the item will 
    ''' be placed</summary>
    Public ReadOnly Property Y() As UShort
        Get
            Return _Y
        End Get
    End Property

    ''' <summary>The location of the item on the Z axis.  If the item is inside of a container this
    ''' specifies the "height" of it, like if its on top of other objects.</summary>
    Public ReadOnly Property Z() As UShort
        Get
            Return _Z
        End Get
    End Property

    ''' <summary>The serial of the container of the item.</summary>
    Public ReadOnly Property Container() As Serial
        Get
            Return _Container
        End Get
    End Property

    ''' <summary>The item's hue.</summary>
    Public ReadOnly Property Hue() As UShort
        Get
            Return _Hue
        End Get
    End Property

    ''' <summary>
    ''' Returns a string containing the ASCII name of the item artwork name. Returns "Blank" if no typename can be found.
    ''' </summary>
    Public ReadOnly Property TypeName() As String
        Get
            Return _Client.GetTypeString(_Type)
        End Get
    End Property

    Public ReadOnly Property Contents() As ItemDatabase.ContentsList
        Get
            Return _Contents
        End Get
    End Property

    Public Overridable ReadOnly Property Layer() As UOLite2.Enums.Layers
        Get
            Return _Layer
        End Get
    End Property

    ''' <summary>
    ''' A user defined string for making notes on items and mobiles.
    ''' </summary>
    Public Property Tag As String = ""

#End Region

#Region "Functions/Subs"
    Public Sub DoubleClick()
        _Client.ActionBuffer.Add(SupportClasses.ActionBufferClass.ActionType.DoubleClick, Serial)
    End Sub

    Public Sub SingleClick()
        'Make the packet
        Dim sc As New Packets.Singleclick

        'Assign the serial
        sc.Serial = Me.Serial

        'Send the packet to the server.
        _Client.Send(sc)
    End Sub

    ''' <summary>
    ''' Picks up the object.
    ''' </summary>
    ''' <param name="Amount">The amount that you want to take, if it is a stack. (0 for the whole stack)</param>
    Public Sub Take(ByVal Amount As UShort)
        _Client.ActionBuffer.Add(SupportClasses.ActionBufferClass.ActionType.PickupItem, Serial, Amount)
    End Sub

    Public Sub Move(ByRef TargetContainer As Serial)
        Take(Amount)

        While _Client.PlayerBusy
            'Wait for the player to no longer be busy.
        End While

        _Client.DropItem(TargetContainer)
    End Sub

    ''' <summary>
    ''' Drops the item as the feet of the player.
    ''' </summary>
    Public Sub Drop()
        Take(Amount)

        While _Client.PlayerBusy
            'Wait for the player to no longer be busy.
        End While

        _Client.DropItem(_Client.Player.X, _Client.Player.Y, _Client.Player.Z)
    End Sub

#End Region

End Class

Partial Class LiteClient
    ''' <summary>
    ''' Drops the item on the ground at the specified point.
    ''' </summary>
    Public Overloads Sub DropItem(ByRef X As UShort, ByRef Y As UShort, ByRef Z As Byte)
        Dim k As Packets.DropObject = New Packets.DropObject(_ItemInHand, X, Y, Z, WorldSerial)
        Send(k)
        _ItemInHand = ZeroSerial
    End Sub

    ''' <summary>
    ''' Drops the item into the specified container.
    ''' </summary>
    Public Overloads Sub DropItem(ByRef Container As Serial)
        Dim k As Packets.DropObject = New Packets.DropObject(_ItemInHand, &HFFFF, &HFFFF, 0, Container)
        Send(k)
        _ItemInHand = ZeroSerial
    End Sub

    ''' <summary>
    ''' Drops the item in the players hand into the player's backpack.
    ''' </summary>
    Public Overloads Sub DropItem()
        DropItem(Player.Layers.BackPack.Serial)
    End Sub

    Public Sub DoubleClick(ByRef Serial As Serial)
        ActionBuffer.Add(SupportClasses.ActionBufferClass.ActionType.DoubleClick, Serial)
    End Sub

    Public Sub SingleClick(ByRef Serial As Serial)
        'Make the packet
        Dim sc As New Packets.Singleclick

        'Assign the serial
        sc.Serial = Serial

        'Send the packet to the server.
        Send(sc)
    End Sub

End Class
