Partial Class LiteClient

    Public Event onPartyInvitation(ByRef Client As LiteClient, ByRef PartyLeader As Serial)

    Private _InParty As Boolean = False
    Public Party As UOParty = Nothing

    Public Class UOParty
        Private _Members As New ArrayList
        Private _Leader As New Serial(0)
        Private _CanLootMe As Boolean = False
        Private _Invitation As New PartyInvitation

        Friend Sub New(ByRef Invitation As PartyInvitation)
            _Invitation = Invitation
        End Sub

        Public Property CanLootMe As Boolean
            Get
                Return _CanLootMe
            End Get
            Set(ByVal value As Boolean)
                'TODO: Add some packet building code here.

            End Set
        End Property

        Public ReadOnly Property Members As Serial()
            Get
                Return _Members.ToArray
            End Get
        End Property

        Public Overloads Sub SendMessage(ByRef Text As String, ByRef Member As Serial)
            'TODO: Add some packet building code.

        End Sub

        Public Overloads Sub SendMessage(ByRef Text As String)
            'TODO: Add some packet building code.

        End Sub

    End Class

    Public Class PartyInvitation
        Friend _Leader As Serial
        Friend _Active As Boolean = False

        Friend Sub New()

        End Sub

        Public Sub Accept()
            'TODO: Place some packet building code here.

            '...


            _Active = False

        End Sub

        Public Sub Deny()
            'TODO: Place some packet building code here.

            '...


            _Active = False

        End Sub

        Public ReadOnly Property Active As Boolean
            Get
                Return _Active
            End Get
        End Property

        Public ReadOnly Property Leader As Serial
            Get
                Return _Leader
            End Get
        End Property

    End Class

End Class
