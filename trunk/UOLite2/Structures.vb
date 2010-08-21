'Common end-user accessable structures.

Public Structure CharListEntry
    Public Name As String
    Public Password As String
    Public Slot As Byte
End Structure

Public Class ItemProperty
    Friend _CliLocNumber As CliLoc
    Friend _Text As String = ""

    Public Sub New(ByRef CliLocNumber As UInteger, Optional ByRef Text As String = "")
        _CliLocNumber = New CliLoc(CliLocNumber)
        Text = Text
    End Sub

    Public ReadOnly Property Cliloc As CliLoc
        Get
            Return _CliLocNumber
        End Get
    End Property

    Public ReadOnly Property Text As String
        Get
            Return _Text
        End Get
    End Property

End Class

Public Class CliLoc
    Friend _CliLocNumber As UInt32

    Public Sub New(ByRef CliLocNumber As UInt32)
        _CliLocNumber = CliLocNumber
    End Sub

    Public ReadOnly Property Text As String
        Get
            'TODO: Make something work here.
            Return ""
        End Get
    End Property

End Class
