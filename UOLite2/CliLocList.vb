Imports System.IO, System.Text

Partial Class LiteClient


    Public ReadOnly Property CliLocStrings As SupportClasses.CliLocList
        Get
            Return StrLst
        End Get
    End Property

    Public Function GetTypeString(ByVal Type As UShort)
        If StrLst.Entry(1020000 + Type) Is Nothing Then
            Return ""
        End If

        Return StrLst.Entry(1020000 + Type)
    End Function

End Class

Namespace SupportClasses

    Public Class CliLocList
        Private _StringHash As New Hashtable(100000)
        Private _ReadBuffer(4096) As Byte

        Public Sub New(ByRef CliLocFile As String)

            Dim clil As CliLoc
            Dim Text As String
            Dim Number As UInteger

            Using bin As New BinaryReader(New FileStream(CliLocFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                bin.ReadInt32()
                bin.ReadInt16()

                While bin.BaseStream.Length <> bin.BaseStream.Position
                    Number = bin.ReadInt32()
                    bin.ReadByte()
                    Dim length As Integer = bin.ReadInt16()

                    bin.Read(_ReadBuffer, 0, length)
                    Text = Encoding.UTF8.GetString(_ReadBuffer, 0, length)

                    clil = New CliLoc(Number, Text)

                    _StringHash.Add(clil.Number, clil)
                End While
            End Using

        End Sub

        Public ReadOnly Property Entry(ByVal CliLocNumber As UInt32) As String
            Get
                If _StringHash.ContainsKey(CliLocNumber) Then
                    Return DirectCast(_StringHash(CliLocNumber), CliLoc).Text
                Else
                    Return ""
                End If
            End Get
        End Property

        Public Function Search(ByRef SearchString As String) As HashSet(Of CliLoc)
            Dim hs As New HashSet(Of CliLoc)

            For Each c As CliLoc In _StringHash
                If c.Text.Contains(SearchString) Then
                    hs.Add(c)
                End If
            Next

            Return hs
        End Function

        Public ReadOnly Property Count As UInt32
            Get
                Return _StringHash.Count
            End Get
        End Property

        Public Function GetCliLoc(ByRef CliLocNumber As UInt32) As CliLoc
            If _StringHash.ContainsKey(CliLocNumber) Then
                Return DirectCast(_StringHash(CliLocNumber), CliLoc)
            Else
                Return New CliLoc(0, "")
            End If
        End Function

    End Class

    Public Class CliLoc
        Friend _CliLocNumber As UInt32
        Friend _Text As String

        Public Sub New(ByRef CliLocNumber As UInt32, ByRef Text As String)
            _CliLocNumber = CliLocNumber
            _Text = Text
        End Sub

        Public ReadOnly Property Number As UInt32
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
End Namespace