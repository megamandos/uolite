Namespace SupportClasses
    Public Class ItemProperty
        Friend _CliLoc As CliLoc
        Friend _Text As String

        Public Sub New(ByRef CliLoc As CliLoc, ByRef Text As String)
            _CliLoc = CliLoc
            _Text = Text
        End Sub

        Public ReadOnly Property Cliloc As CliLoc
            Get
                Return _CliLoc
            End Get
        End Property

        Public ReadOnly Property Text As String
            Get
                Return _Text
            End Get
        End Property

    End Class
End Namespace