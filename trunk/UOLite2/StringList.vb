'Keeps a list of the CliLoc strings, used by cliloc #'s in various packets.
'Things like item names, and most system messages are kept here.
'Although these are not currently implemented.

Imports System, System.IO, System.Text, System.Collections

'Im not going to lie here, I stole this right out of the UltimaSDK.

Partial Class LiteClient


#If DEBUG Then
    ''' <summary>
    ''' A list of the clients strings from the cliloc files.
    ''' </summary>
    Public Class StringList
#Else
    ''' Hide this class from the user, there is no reason from him/her to see it.
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
    Public Class StringList
#End If
        Private m_Table As Hashtable
        Private m_Entries As StringEntry()
        Private m_Language As String

        Public ReadOnly Property Entries() As StringEntry()
            Get
                Return m_Entries
            End Get
        End Property
        Public ReadOnly Property Table() As Hashtable
            Get
                Return m_Table
            End Get
        End Property
        Public ReadOnly Property Language() As String
            Get
                Return m_Language
            End Get
        End Property

        Private Shared m_Buffer As Byte() = New Byte(1023) {}

        ''' <summary>
        ''' Creates a new instance of the string list and populates it with the specified language.
        ''' <example>Dim StrLst As New StringList("enu")
        ''' MsgBox(StrLst.</example>
        ''' </summary>
        ''' <param name="language">Languages: enu,chs,cht,deu,esp,fra,jpn,kor</param>
        Public Sub New(ByVal language As String)
            m_Language = language
            m_Table = New Hashtable()

            Dim path As String = ContentPath & "\cliloc." & language

            Dim list As New ArrayList()

            Using bin As New BinaryReader(New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                bin.ReadInt32()
                bin.ReadInt16()

                While bin.BaseStream.Length <> bin.BaseStream.Position
                    Dim number As Integer = bin.ReadInt32()
                    bin.ReadByte()
                    Dim length As Integer = bin.ReadInt16()

                    If length > m_Buffer.Length Then
                        m_Buffer = New Byte(((length + 1023) And Not 1023) - 1) {}
                    End If

                    bin.Read(m_Buffer, 0, length)
                    Dim text As String = Encoding.UTF8.GetString(m_Buffer, 0, length)

                    list.Add(New StringEntry(number, text))
                    m_Table(number) = text
                End While
            End Using

            m_Entries = DirectCast(list.ToArray(GetType(StringEntry)), StringEntry())
        End Sub
    End Class

#If DEBUG Then
    Public Class StringEntry
#Else
    ''' Hide this class from the user, there is no reason from him/her to see it.
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
    Public Class StringEntry
#End If
        Private m_Number As Integer
        Private m_Text As String

        Public ReadOnly Property Number() As Integer
            Get
                Return m_Number
            End Get
        End Property

        Public ReadOnly Property Text() As String
            Get
                Return m_Text
            End Get
        End Property

        Public Sub New(ByVal number As Integer, ByVal text As String)
            m_Number = number
            m_Text = text
        End Sub
    End Class

End Class