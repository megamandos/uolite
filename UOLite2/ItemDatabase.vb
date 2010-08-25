'This is used to handle a database of items. As an end user, you should NEVER make a new instance of this.


Partial Class LiteClient


    Protected Friend Shared ReadOnly WorldSerial As Serial = New Serial(Convert.ToUInt32(4294967295))
    Protected Friend Shared ReadOnly ZeroSerial As Serial = New Serial(Convert.ToUInt32(0))

    Protected Friend _ItemInHand As Serial = ZeroSerial

    Private WithEvents _ItemDatabase As New ItemDatabase(Me)

    Public ReadOnly Property Items As ItemDatabase
        Get
            Return _ItemDatabase
        End Get
    End Property

End Class


'Hide this class from the user, there is no reason from him/her to see it.
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
Public Class ItemDatabase
    Implements ICollection(Of Item)

    Friend Event NewItem(ByRef item As Item)

    'This will be every item in the game.
    Private _AllItems As New Hashtable

    'Every serial of every item in the game.
    Private _AllSerials As New HashSet(Of Serial)

    'This will be just the items that go in the top layer of the World
    Private _World As New ContentsList(Me, LiteClient.WorldSerial)

    'To apply to the items as they are added, for later callbacks.
    Private _Client As LiteClient

    Friend Sub New(ByRef Client As LiteClient)
        _Client = Client

        'Make the world item
        Dim WorldItem As New Item(Client, LiteClient.WorldSerial)

        'Add the World contents list as the World Item's contents list.
        WorldItem._Contents = _World

        'Add the item to the All Items list.
        _AllItems.Add(WorldItem.Serial, WorldItem)

        'Add the item's serial to the all serials list.
        _AllSerials.Add(WorldItem.Serial)

    End Sub

#Region "Add Item(s)"

    Public Overloads Sub Add(ByRef Item As Item)
        'Don't add the same thing twice.
        If _AllSerials.Contains(Item.Serial) Then Exit Sub

        'Set the item's client for callbacks.
        Item._Client = _Client

        'Add the item to the All Items list.
        _AllItems.Add(Item.Serial, Item)

        'Add the item's serial to the all serials list.
        _AllSerials.Add(Item.Serial)

        'Add the item to it's container's contents list
        DirectCast(_AllItems(Item.Container), Item).Contents.Add(Item.Serial)

        If Item.Contents Is Nothing Then
            'Make a contents list for the item
            Dim Contents As New ContentsList(Me, Item.Serial)

            'Apply the new empty contents list to the item.
            Item._Contents = Contents
        Else
            'Add the item's contents to the items database.
            Add(Item.Contents)
        End If

        'If the server supports AOS features, send a request for the item's property strings.
        If _Client.Features.AOS Then
            Dim bytes(7) As Byte

            bytes(0) = CByte(&HD6)
            bytes(1) = 0
            bytes(2) = bytes.Length

            _Client.InsertBytes(Item.Serial.GetBytes, bytes, 0, 3, 4)

            _Client.Send(bytes)
        End If

    End Sub

    Public Overloads Sub Add(ByRef Items As HashSet(Of Item))
        For Each i As Item In Items
            Add(i)
        Next
    End Sub

    Public Overloads Sub Add(ByRef Items As ContentsList)
        For Each i As Item In Items.Items
            Add(i)
        Next
    End Sub

    Friend Overloads Sub Add(ByVal Packet As Packets.ContainerContents)
        Dim j As Item

        For Each i As Item In Packet.Items
            j = New Item(_Client, i.Serial)

            j._Type = i._Type
            j._StackID = i._StackID
            j._X = i._X
            j._Y = i._Y
            j._Container = i._Container
            j._Hue = i._Hue
            j._Amount = i._Amount
#If DebugItemList Then
                Console.WriteLine("-Adding Item by ContainerContents: Container: " & j._Container.ToString & " Serial:" & i.Serial.ToString)
#End If

            Add(j)
        Next

    End Sub

    Friend Overloads Sub Add(ByVal Packet As Packets.ObjectToObject)

        Dim j As New Item(_Client, Packet.Serial)

        j._Type = Packet._Itemtype
        j._StackID = Packet._stackID
        j._Amount = Packet._amount
        j._X = Packet._X
        j._Y = Packet._Y
        j._Container = Packet._Container
        j._Hue = Packet._Hue

#If DebugItemList Then
            Console.WriteLine("-Adding Item by ObjectToObject: " & j.Serial.ToString)
#End If

        Add(j)
    End Sub

    Friend Overloads Sub Add(ByVal Packet As Packets.ShowItem)

        Dim j As New Item(_Client, Packet.Serial)

        j._Container = LiteClient.WorldSerial 'Set the container to the worldserial, because thats where this is.
        j._Type = Packet.ItemType
        j._Amount = Packet.Amount
        j._StackID = Packet.StackID
        j._X = Packet.X
        j._Y = Packet.Y
        j._Direction = Packet.Direction
        j._Z = Packet.Z
        j._Hue = Packet.Hue

#Const DEBUGShowItem = False

#If DEBUGShowItem = True Then
            Console.WriteLine("-Adding Item by Show Item.")
            Console.WriteLine(" Packet: " & BitConverter.ToString(Packet.Data))
            Console.WriteLine(" Serial: " & j.Serial.ToString)
            Console.WriteLine(" Serial#: " & j.Serial.Value)
#End If

#If DebugItemList Then
            Console.WriteLine("-Adding Item by ShowItem: " & j.Serial.ToString)
#End If

        Add(j)
    End Sub

#End Region

#Region "Remove Items"

    Public Overloads Function RemoveItem(ByRef Item As Item)
        Return RemoveItem(Item.Serial)
    End Function

    Private Overloads Sub RemoveItem(ByRef Serials As HashSet(Of Serial))
        For Each s As Serial In Serials
            RemoveItem(s)
        Next
    End Sub

    Public Overloads Function RemoveItem(ByRef Serial As Serial)
        If _AllSerials.Contains(Serial) Then
            'Remove the Item's contents
            RemoveItem(DirectCast(_AllItems.Item(Serial), Item).Contents.Serials)

            'Remove the item from it's container's contents list.
            'Item.Container.Contents.Remove(Serial)
            DirectCast(_AllItems(DirectCast(_AllItems.Item(Serial), Item).Container), Item).Contents.Remove(Serial)

            'Remove the item from the All Items list.
            _AllItems.Remove(Serial)

            'Remove the item's serial from the Serial's database.
            _AllSerials.Remove(Serial)

            Return True
        End If

        Return False
    End Function

    Public Sub Clear() Implements System.Collections.Generic.ICollection(Of Item).Clear
        _AllItems.Clear()
        _AllSerials.Clear()

        'Make the world item
        Dim WorldItem As New Item

        'Make the world item's serial the world serial.
        WorldItem._Serial = LiteClient.WorldSerial

        'Add the World contents list as the World Item's contents list.
        WorldItem._Contents = _World

        Add(WorldItem)
    End Sub

#End Region

#Region "Properties/Enumeration"

    Public ReadOnly Property Count As Integer Implements System.Collections.Generic.ICollection(Of Item).Count
        Get
            Return _AllItems.Count
        End Get
    End Property

    Public ReadOnly Property Item(ByVal Serial As Serial) As Item
        Get
            Return _AllItems(Serial)
        End Get
    End Property

    Public ReadOnly Property Serials As HashSet(Of Serial)
        Get
            Return _AllSerials
        End Get
    End Property

    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Item) Implements System.Collections.Generic.IEnumerable(Of Item).GetEnumerator
        Return _AllItems.OfType(Of Item).GetEnumerator
    End Function

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return _AllItems.OfType(Of Item).GetEnumerator
    End Function

    Public Overloads Function byType(ByRef Type As UShort) As HashSet(Of Item)
        Dim ItemSet As New HashSet(Of Item)

        For Each S As Serial In _AllSerials
            If DirectCast(_AllItems(S), Item).Type = Type Then
                ItemSet.Add(DirectCast(_AllItems(S), Item))
            End If
        Next

        Return ItemSet
    End Function

    Public Overloads Function byType(ByRef Types As HashSet(Of UShort)) As HashSet(Of Item)
        Dim ItemSet As New HashSet(Of Item)

        For Each S As Serial In _AllSerials
            For Each T As UShort In Types
                If DirectCast(_AllItems(S), Item).Type = T Then
                    ItemSet.Add(DirectCast(_AllItems(S), Item))
                End If
            Next
        Next

        Return ItemSet
    End Function

    Public Overloads Function byType(ByRef Types() As UShort) As HashSet(Of Item)
        Dim ItemSet As New HashSet(Of Item)

        For Each S As Serial In _AllSerials
            For Each T As UShort In Types
                If DirectCast(_AllItems(S), Item).Type = T Then
                    ItemSet.Add(DirectCast(_AllItems(S), Item))
                End If
            Next
        Next

        Return ItemSet
    End Function

    Public Function byRazorSerial(ByRef Serial As String) As Serial
        If _AllSerials.Contains(_Client.GetSerialFromString(Serial)) Then
            Return _Client.GetSerialFromString(Serial)
        Else
            Return LiteClient.ZeroSerial
        End If
    End Function

    Public Overloads Function Contains(ByRef Item As Item) As Boolean
        If _AllSerials.Contains(Item.Serial) Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Overloads Function Contains(ByRef Serial As Serial) As Boolean
        If _AllSerials.Contains(Serial) Then
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Returns an array of all of the items the client is currently tracking.
    ''' </summary>
    Public ReadOnly Property Items As UOLite2.Item()
        Get
            Dim retarray(_AllItems.Count - 1) As UOLite2.Item
            _AllItems.Values.CopyTo(retarray, 0)
            Return retarray
        End Get
    End Property

#End Region

    Public Class ContentsList
        Private _Owner As Serial
        Private _Serials As New HashSet(Of Serial)
        Private _Database As ItemDatabase

        ''' <param name="Database">The Database that contains the items in this list.</param>
        ''' <param name="Owner">The item this list belongs to.</param>
        Friend Sub New(ByRef Database As ItemDatabase, ByRef Owner As Serial)
            _Database = Database
            _Owner = Owner
        End Sub

        Friend Sub Add(ByRef Serial As Serial)
            _Serials.Add(Serial)
        End Sub

        Friend Sub Add(ByRef Item As Item)
            _Serials.Add(Item.Serial)
        End Sub

        Friend Sub Remove(ByRef Serial As Serial)
            _Serials.Remove(Serial)
        End Sub

        Friend Sub Remove(ByRef Item As Item)
            _Serials.Remove(Item.Serial)
        End Sub

        Public ReadOnly Property Count As Integer
            Get
                Return _Serials.Count
            End Get
        End Property

        ''' <summary>
        ''' Returns a hashset of the items contains in this list.
        ''' </summary>
        Public ReadOnly Property Items As HashSet(Of Item)
            Get
                Dim RetHash As New HashSet(Of Item)

                For Each s As Serial In _Serials
                    RetHash.Add(_Database._AllItems(s))
                Next

                Return RetHash
            End Get
        End Property

        ''' <summary>Returns a hashset of the serials of the items contained in this list.</summary>
        Public ReadOnly Property Serials As HashSet(Of Serial)
            Get
                Return _Serials
            End Get
        End Property

        Public Function byType(Optional ByRef Recursive As Boolean = False) As HashSet(Of Serial)
            Dim RetSers As New HashSet(Of Serial)

            For Each s As Serial In _Serials
                RetSers.Add(s)

                If Recursive Then
                    For Each ser As Serial In DirectCast(_Database._AllItems(s), Item).Contents.byType(True)
                        RetSers.Add(ser)
                    Next
                End If

            Next

            Return RetSers
        End Function

        ''' <summary>
        ''' Returns the total of all stacks of the item type specified.
        ''' </summary>
        ''' <param name="Recursive">Whether or not to search subcontainers.</param>
        Public Function byTypeTotal(Optional ByRef Recursive As Boolean = False) As UInteger
            Dim RetUInt As UInteger = 0

            For Each s As Serial In byType(Recursive)
                RetUInt += DirectCast(_Database._AllItems(s), Item).Amount
            Next

            Return RetUInt
        End Function

    End Class

#Region "NOT USED!!"
    ''' <summary>
    ''' NOT USED!
    ''' </summary>
    Private Function Contains2(ByVal item As Item) As Boolean Implements System.Collections.Generic.ICollection(Of Item).Contains
        Return Nothing
    End Function

    ''' <summary>
    ''' NOT USED!!!
    ''' </summary>
    Private Sub Add1(ByVal item As Item) Implements System.Collections.Generic.ICollection(Of Item).Add
    End Sub

    ''' <summary>
    ''' NOT USED!!!
    ''' </summary>
    Private Sub CopyTo(ByVal array() As Item, ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of Item).CopyTo
    End Sub

    ''' <summary>
    ''' NOT USED!!!
    ''' </summary>
    Private ReadOnly Property IsReadOnly As Boolean Implements System.Collections.Generic.ICollection(Of Item).IsReadOnly
        Get
            Return Nothing
        End Get
    End Property

    ''' <summary>
    ''' NOT USED!!!
    ''' </summary>
    Private Function Remove0(ByVal item As Item) As Boolean Implements System.Collections.Generic.ICollection(Of Item).Remove
        Return Nothing
    End Function
#End Region

End Class
