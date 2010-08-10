'For scavenging items, much like razor scavenger, but a LOT more powerful.

Partial Class LiteClient

    Private WithEvents _Scavenger As New ScavengerObject(Me)

    Public Event onScavengerPickup(ByRef client As LiteClient, ByRef Serial As Serial, ByRef Type As UShort)

    Public ReadOnly Property Scavenger As ScavengerObject
        Get
            Return _Scavenger
        End Get
    End Property

    Public Class ScavengerObject
        Private _Client As LiteClient
        Private _ItemTypes As New HashSet(Of UShort)
        Private GrabDistance As Byte = 2
        Private _AltCont As AltCont

        Friend Sub New(ByRef Client As LiteClient)
            _Client = Client
            _AltCont = New AltCont(_Client)
        End Sub

        Public Property Enabled As Boolean = False

        Public ReadOnly Property AlternateContainer As AltCont
            Get
                Return _AltCont
            End Get
        End Property

        Public Class AltCont
            Private _Container As Serial
            Private _Enabled As Boolean = False
            Private _Client As LiteClient
            Private _IsMobile As Boolean = False
            Private _Backpack As Serial

            Friend ReadOnly Property MobileBackpack As Serial
                Get
                    Return _Backpack
                End Get
            End Property

            Public ReadOnly Property IsMobile As Boolean
                Get
                    Return _IsMobile
                End Get
            End Property

            Friend Sub New(ByRef Client As LiteClient)
                _Client = Client
            End Sub

            Public Property Enabled As Boolean
                Get
                    Return _Enabled
                End Get
                Set(ByVal value As Boolean)
                    _Enabled = value
                End Set
            End Property

            Public ReadOnly Property Container As Serial
                Get
                    If Enabled = False Then
                        Return ZeroSerial
                    Else
                        Return _Container
                    End If
                End Get
            End Property

            Public Overloads Function SetContainer(ByRef RazorSerial As String) As Boolean
                Return SetContainer(_Client.GetSerialFromString(RazorSerial))
            End Function

            Public Overloads Function SetContainer(ByRef Serial As Serial) As Boolean
                If _Client.Mobiles.Exists(Serial) = True Then
                    Try
                        _Container = Serial
                    Catch ex As Exception
                        _Enabled = False
                    End Try

                    _IsMobile = True
                    _Backpack = _Client.Mobiles.Mobile(Serial).Layers.BackPack.Serial
                    _Enabled = True

                ElseIf _Client.Items.Contains(Serial) = True Then
                    _Container = Serial
                    _Enabled = True

                Else
                    _Enabled = False
                End If

                Return _Enabled
            End Function

            Public Property DismountToAccess As Boolean = False

        End Class

        Public Sub AddType(ByRef Type As UShort)
            If Not _ItemTypes.Contains(Type) Then
                _ItemTypes.Add(Type)
            End If
        End Sub

        Public Sub RemoveType(ByRef Type As UShort)
            If _ItemTypes.Contains(Type) Then
                _ItemTypes.Remove(Type)
            End If
        End Sub

        Public Function TypeList() As UShort()
            Return _ItemTypes.ToArray
        End Function

        Public Sub Toggle(Optional ByRef Silent As Boolean = True, Optional ByRef OnMessage As String = "Scavenger On", Optional ByRef OffMessage As String = "Scavenger Off")
            If _Enabled Then
                _Enabled = False
                If Not Silent Then _Client.Speak(OffMessage)
            Else
                _Enabled = True
                If Not Silent Then _Client.Speak(OnMessage)
            End If
        End Sub

        Friend Sub CheckForPickup(ByRef Serial As Serial)
            If _Enabled Then
                If _Client.Get2DDistance(_Client.Items.Item(Serial).X, _Client.Items.Item(Serial).Y, _Client.Player.X, _Client.Player.Y) <= GrabDistance Then

                    For Each t As UShort In TypeList()
                        If _Client.Items.Item(Serial).Type = t Then

                            If AlternateContainer.Enabled = True Then
                                If AlternateContainer.DismountToAccess And _Client.Player.IsMounted Then
                                    _Client.Player.DoubleClick()

                                    If AlternateContainer.IsMobile Then
                                        _Client.Items.Item(Serial).Move(AlternateContainer.MobileBackpack)

                                        _Client.DoubleClick(AlternateContainer.Container)
                                    Else
                                        _Client.Items.Item(Serial).Move(AlternateContainer.Container)
                                    End If

                                ElseIf AlternateContainer.DismountToAccess Then
                                    _Client.Player.DoubleClick()
                                    If AlternateContainer.IsMobile Then
                                        _Client.Items.Item(Serial).Move(AlternateContainer.MobileBackpack)
                                    Else
                                        _Client.Items.Item(Serial).Move(AlternateContainer.Container)
                                    End If
                                Else
                                    _Client.Items.Item(Serial).Move(AlternateContainer.Container)
                                End If
                            Else
                                _Client.Items.Item(Serial).Move(_Client.Player.Layers.BackPack.Serial)
                            End If

                            'We have the item, no need to continue.
                            Exit Sub
                        End If
                    Next

                End If
            End If
        End Sub

        Friend Sub CheckSurroundings()
            If _Enabled Then

                For Each i As Item In _Client.Items.Item(WorldSerial).Contents.Items
                    CheckForPickup(i.Serial)
                Next

            End If
        End Sub

    End Class

End Class
