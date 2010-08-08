Partial Class LiteClient

    ''' <summary>
    ''' An object oriented representation of an in-game context menu.
    ''' </summary>
    Public Class ContextMenu
        Implements ICollection(Of ContextMenuOption)
        Private _OptionHash As New System.Collections.Generic.List(Of ContextMenuOption)
        Public Enabled As Boolean = False

#Region "Context Menu Option Class"

#If DEBUG Then
        Public Class ContextMenuOption
#Else
        ''' Hide this class from the user, there is no reason from him/her to see it.
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
        Public Class ContextMenuOption
#End If
            Private _CliLocNumber As UInteger = 3006249
            Private _ParentContextMenu As ContextMenu
            Private _Hue As UShort = 0
            Private _Enabled As Boolean = True

            Friend Sub New(ByVal CliLocNumber As UInteger, ByVal ContextMenu As ContextMenu)
                _ParentContextMenu = ContextMenu
                _CliLocNumber = CliLocNumber
            End Sub

            ''' <summary>The index of the option in the menu.</summary>
            Public ReadOnly Property Index() As UShort
                Get
                    Return _ParentContextMenu._OptionHash.IndexOf(Me)
                End Get
            End Property

            ''' <summary>A cliloc number from 3006505 to 3006077.</summary>
            Public Property CliLocNumber() As UInteger
                Get
                    Return _CliLocNumber
                End Get
                Set(ByVal value As UInteger)
                    _CliLocNumber = value
                End Set
            End Property

            ''' <summary>Returns a the CliLoc string that corresponds to the CliLoc number.</summary>
            Public ReadOnly Property CliLocString() As String
                Get
                    Return StrLst.Table(_CliLocNumber)
                End Get
            End Property

            ''' <summary>
            ''' The hue of the text. Default is zero.
            ''' </summary>
            Public Property Hue() As UShort
                Get
                    Return _Hue
                End Get
                Set(ByVal value As UShort)
                    _Hue = value
                End Set
            End Property

            ''' <summary>
            ''' Whether or not the option will be displayed as "greyed out".
            ''' </summary>
            Public Property Enabled() As Boolean
                Get
                    Return _Enabled
                End Get
                Set(ByVal value As Boolean)
                    _Enabled = value
                End Set
            End Property

            ''' <summary>Removes the option from the menu.</summary>
            Public Sub Remove()
                _ParentContextMenu.Remove(Me)
            End Sub

        End Class
#End Region

#Region "The 8 ways to add an option..."

        ''' <summary>
        ''' Adds a new option to the end of the context menu, using the supplied CliLoc number.
        ''' </summary>
        ''' <param name="CliLocNumber">A cliloc number from 3006505 to 3006077</param>
        Public Sub Add(ByVal CliLocNumber As UInteger)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber

            _OptionHash.Add(opt)
        End Sub

        ''' <summary>
        ''' Adds a new option at the specified index with the supplied CliLocNumber.
        ''' </summary>
        ''' <param name="CliLocNumber">A cliloc number from 3006505 to 3006077</param>
        ''' <param name="Index">The index to insert the new option at.</param>
        Public Sub Add(ByVal CliLocNumber As UInteger, ByVal Index As UInteger)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber

            _OptionHash.Insert(Index, opt)
        End Sub

        Public Sub Add(ByVal CliLocNumber As UInteger, ByVal Hue As UShort)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber
            opt.Hue = Hue

            _OptionHash.Add(opt)
        End Sub

        Public Sub Add(ByVal CliLocNumber As UInteger, ByVal Hue As UShort, ByVal Index As UInteger)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber
            opt.Hue = Hue

            _OptionHash.Insert(Index, opt)
        End Sub

        Public Sub Add(ByVal CliLocNumber As UInteger, ByVal Hue As UShort, ByVal Enabled As Boolean)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber
            opt.Hue = Hue
            opt.Enabled = Enabled

            _OptionHash.Add(opt)
        End Sub

        Public Sub Add(ByVal CliLocNumber As UInteger, ByVal Hue As UShort, ByVal Enabled As Boolean, ByVal Index As UInteger)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber
            opt.Hue = Hue
            opt.Enabled = Enabled

            _OptionHash.Insert(Index, opt)
        End Sub

        Public Sub Add(ByVal CliLocNumber As UInteger, ByVal Enabled As Boolean)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber
            opt.Enabled = Enabled

            _OptionHash.Add(opt)
        End Sub

        Public Sub Add(ByVal CliLocNumber As UInteger, ByVal Enabled As Boolean, ByVal Index As UInteger)
            Dim opt As New ContextMenuOption(Me.Count, Me)
            opt.CliLocNumber = CliLocNumber
            opt.Enabled = Enabled

            _OptionHash.Insert(Index, opt)
        End Sub

#End Region

        ''' <summary>
        ''' Removes all of the options.
        ''' </summary>
        Public Sub Clear() Implements System.Collections.Generic.ICollection(Of ContextMenuOption).Clear
            _OptionHash.Clear()
        End Sub

        ''' <summary>
        ''' Returns the number of the options in the menu.
        ''' </summary>
        Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of ContextMenuOption).Count
            Get
                Return _OptionHash.Count
            End Get
        End Property

        ''' <summary>
        ''' Removes the option at the specified index.
        ''' </summary>
        ''' <param name="Index">The index of the option to remove.</param>
        Public Sub Remove(ByVal Index As UInteger)
            _OptionHash.RemoveAt(Index)
        End Sub

        Friend Function Packet(ByVal Serial As Serial) As Packets.DisplayContextMenu
            Return New Packets.DisplayContextMenu(Me, Serial)
        End Function

#Region "Private Functions/Subs"

        Private Function Remove(ByVal item As ContextMenuOption) As Boolean Implements System.Collections.Generic.ICollection(Of ContextMenuOption).Remove
            If _OptionHash.Contains(item) Then
                _OptionHash.Remove(item)
                Return True
            Else
                Return False
            End If
        End Function

        Private Sub Add(ByVal item As ContextMenuOption) Implements System.Collections.Generic.ICollection(Of ContextMenuOption).Add
        End Sub

        Private Function Contains(ByVal item As ContextMenuOption) As Boolean Implements System.Collections.Generic.ICollection(Of ContextMenuOption).Contains
            Return _OptionHash.Contains(item)
        End Function

        Private Sub CopyTo(ByVal array() As ContextMenuOption, ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of ContextMenuOption).CopyTo

        End Sub

        Private ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of ContextMenuOption).IsReadOnly
            Get
                Return True
            End Get
        End Property

        Private Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of ContextMenuOption) Implements System.Collections.Generic.IEnumerable(Of ContextMenuOption).GetEnumerator
            Dim ie As IEnumerator = _OptionHash.GetEnumerator
            Return ie
        End Function

        Private Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return _OptionHash.GetEnumerator
        End Function

#End Region

    End Class

    Partial Class Item
        Private _CTMenu As New ContextMenu

        Public ReadOnly Property Menu() As ContextMenu
            Get
                Return _CTMenu
            End Get
        End Property

        Friend Sub HandleContextMenuRequest(ByVal packet As Packets.ContextMenuRequest)
            If Me.Menu.Enabled Then
                '_Client.DropPacket()

#Const DebugContextMenuPacket = False
#If DebugContextMenuPacket Then
                Console.WriteLine("Menu Packet: " & BitConverter.ToString(Menu.Packet(Me.Serial).Data))
#End If

                'Respond by sending the packet for this item's context menu to the client
                _Client.Send(Menu.Packet(Me.Serial))
            End If
        End Sub

        Friend Sub HandleContextMenuResponse(ByVal packet As Packets.ContextMenuResponse)
            If Menu.Enabled Then
                '_Client.DropPacket()

                Dim k As New ContextMenuResponder
                k.parent = Me
                k.index = packet.Index
                k.Serial = _Serial

                'Handle the response on a seperate thread. Otherwise the client waits for this to return.
                Dim CMRThread As Threading.Thread = New Threading.Thread(AddressOf k.StartEvent)
                CMRThread.Start()

            End If
        End Sub

        Private Class ContextMenuResponder
            Public Serial As Serial
            Public index As UShort
            Public parent As Item

            Public Sub StartEvent()
                parent.CallCMREvent(Serial, index)
            End Sub
        End Class

        Private Sub CallCMREvent(ByVal Serial As Serial, ByVal Index As UShort)
            RaiseEvent ContextMenuResponse(Serial, Index)
        End Sub

        Public Event ContextMenuResponse(ByVal Serial As Serial, ByVal Index As UShort)

    End Class

End Class