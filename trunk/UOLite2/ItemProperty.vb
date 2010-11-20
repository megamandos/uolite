Namespace SupportClasses

    Public Class ItemProperty
        Friend _CliLoc As CliLoc
        Friend _Text As String
        Friend _Index As UInteger

        Public Sub New(ByRef CliLoc As UInt32, ByRef Text As String)
            _CliLoc = LiteClient.StrLst.GetCliLoc(CliLoc)
            _Text = Text
        End Sub

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

        Public Overrides Function ToString() As String
            Dim RetString As New Text.StringBuilder
            Dim Vals() As String = Text.Split(vbTab)
            Dim FormatString() As String = Cliloc.Text.Split("~")
            Dim CurrentValue As UInt32 = 0

            For i As UInteger = 0 To FormatString.Length - 1
                If i Mod 2 = 0 Then
                    RetString.Append(FormatString(i))
                Else
                    RetString.Append(Vals(CurrentValue))
                    CurrentValue += 1
                End If
            Next

            Return RetString.ToString
        End Function

    End Class

    Public Class PropertyListClass
        Private _Props As New Hashtable
        Private _item As Item
        Private _Indexes As New Hashtable

        Friend Sub New(ByRef Item As Item)
            _item = Item
        End Sub

        Friend Sub New(ByRef Item As Item, ByRef Properties As HashSet(Of SupportClasses.ItemProperty))
            _item = Item
            Dim Jack As New Hashtable

            For Each p As ItemProperty In Properties
                Jack.Add(p.Cliloc.Number, p)
                _Indexes.Add(p._Index, p.Cliloc.Number)
            Next

            _Props = Jack
        End Sub

        Default Public ReadOnly Property byIndex(ByVal Index As UInteger) As SupportClasses.ItemProperty
            Get
                If Index < _Indexes.Count Then
                    Return DirectCast(_Props(_Indexes(Index)), SupportClasses.ItemProperty)
                Else
                    Throw New IndexOutOfRangeException
                End If
            End Get
        End Property

        Public ReadOnly Property byName(ByVal PropertyName As String) As SupportClasses.ItemProperty
            Get
                For Each p As SupportClasses.ItemProperty In _Props
                    If p.Cliloc.Text = PropertyName Then Return p
                Next

                Return New SupportClasses.ItemProperty(New SupportClasses.CliLoc(0, ""), "")
            End Get
        End Property

        Public ReadOnly Property byCliLocNumber(ByVal CliLocNumber As UInteger) As SupportClasses.ItemProperty
            Get

                If _Props.ContainsKey(CliLocNumber) Then
                    Return DirectCast(_Props(CliLocNumber), SupportClasses.ItemProperty)
                Else
                    Return New SupportClasses.ItemProperty(New SupportClasses.CliLoc(0, ""), "")
                End If

            End Get
        End Property

        Public ReadOnly Property Count As UInteger
            Get
                Return _Props.Count
            End Get
        End Property

        Friend Sub Clear()
            _Props.Clear()
        End Sub

        Public Overrides Function ToString() As String

            If Count > 0 Then
                Dim PropString As New Text.StringBuilder

                For i As UInt32 = 0 To Count - 1
                    PropString.Append(byIndex(i))
                    PropString.Append("$")
                Next

                Return PropString.ToString.TrimEnd({CChar("$")})
            Else
                Return ""
            End If

        End Function

        Friend Sub Import(ByRef Properties As HashSet(Of SupportClasses.ItemProperty))
            For Each p As SupportClasses.ItemProperty In Properties
                _Props.Add(p.Cliloc.Number, p)
            Next
        End Sub

    End Class

End Namespace

Namespace Packets
    Public Class MegaCliLoc
        Inherits Packet

        Private _Serial As Serial
        Private _PropHash As New HashSet(Of SupportClasses.ItemProperty)

        Friend Sub New(ByRef Bytes() As Byte)
            MyBase.New(Enums.PacketType.MegaCliloc)

            buff = New UOLite2.SupportClasses.BufferHandler(Bytes, True)

            Dim prop As SupportClasses.ItemProperty
            Dim CliLocNumber As UInt32
            Dim Text As New System.Text.StringBuilder
            Dim StrLen As UShort
            Dim byte1 As Byte
            Dim byte2 As Byte
            Dim Index As UInteger = 0

            With buff
                .Position = 5
                _Serial = New Serial(.readuint)

                .Position = 15

                Do
                    CliLocNumber = .readuint

                    If Not CliLocNumber = 0 Then

                        StrLen = .readushort

                        If Not (StrLen = 0) Then
                            For i As Integer = 1 To StrLen / 2
                                byte1 = .readbyte
                                byte2 = .readbyte
                                Text.Append(ChrW(BitConverter.ToUInt16({byte1, byte2}, 0)))
                                Text.Replace(Chr(0), "")
                            Next
                        End If

                        prop = New SupportClasses.ItemProperty(CliLocNumber, Text.ToString) With {._Index = Index}

                        _PropHash.Add(prop)

                        Index += 1

                        Text.Clear()
                    Else
                        Exit Sub
                    End If

                Loop

            End With

        End Sub

        Public ReadOnly Property Properties As HashSet(Of SupportClasses.ItemProperty)
            Get
                Return _PropHash
            End Get
        End Property

        Public ReadOnly Property Serial As Serial
            Get
                Return _Serial
            End Get
        End Property

    End Class
End Namespace
