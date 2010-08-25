' Currently, gumps are only "partialy" implemented.
' They can be received, but if you want to actualy send a response
' you will have to build the response packet yourself until the next beta is released
' please DO NOT contact me asking how to build this packet,
' everything you need to know is already included in the packet guides
' included with this dll. As you can imagine I am very busy and don't have time to
' walk people through building custom packets byte by byte.

Partial Class LiteClient

    Public Event onNewGump(ByRef Client As LiteClient, ByRef Gump As SupportClasses.Gump)

End Class

Namespace SupportClasses

    Public Class Gump
        Private _Pages As New HashSet(Of Page)
        Private _GumpID As UInteger
        Private _Serial As UInteger
        Private _X As UInteger
        Private _Y As UInteger
        Protected Friend Data() As String
        Protected Friend Text() As String
        Protected Friend _Client As LiteClient

        Friend Sub New(ByRef GumpPacket As Packets.CompressedGump, ByRef Client As LiteClient)
            _Client = Client
            _GumpID = GumpPacket.GumpID
            _Serial = GumpPacket.Serial
            _X = GumpPacket.X
            _Y = GumpPacket.Y

            Data = LCase(GumpPacket.DecompressedGumpData).Split(vbNewLine)
            Text = GumpPacket.DecompressedTextData.Split(vbNewLine)
            Dim CurrentPage As New Page(0, Me)
            Dim tim() As String

            For Each s As String In Data
                tim = s.Split(" ")

                If tim.Count <= 1 Then Continue For

                'TODO: finish implementing the gump system.
                Select Case tim(1)
                    Case "page"
                        CurrentPage = New Page(tim(2), Me)
                        _Pages.Add(CurrentPage)

                    Case "button"
                        CurrentPage.Buttons.Add(New Button(CurrentPage, tim(2), tim(3), tim(4), tim(5), tim(6), tim(7), tim(8)))

                    Case "gumppic"
                        Select Case tim.Length
                            Case 6
                                CurrentPage.GumpPics.Add(New GumpPic(CurrentPage, tim(2), tim(3), tim(4)))
                            Case 7
                                CurrentPage.GumpPics.Add(New GumpPic(CurrentPage, tim(2), tim(3), tim(4), tim(5)))
                        End Select

                End Select

            Next

        End Sub

        'Page [id]
        Public NotInheritable Class Page
            Protected Friend _Gump As Gump
            Private _Buttons As New HashSet(Of Button)
            Private _PageNumber As UShort
            Private _GumpPics As New HashSet(Of GumpPic)

            Friend Sub New(ByRef PageNumber As UShort, ByRef Gump As Gump)
                _PageNumber = PageNumber
            End Sub

            Public ReadOnly Property Buttons As HashSet(Of Button)
                Get
                    Return _Buttons
                End Get
            End Property

            Public ReadOnly Property GumpPics As HashSet(Of GumpPic)
                Get
                    Return _GumpPics
                End Get
            End Property

            Public ReadOnly Property PageNumber As UShort
                Get
                    Return _PageNumber
                End Get
            End Property

        End Class

        Public MustInherit Class GumpObject
            Friend _Page As Page
            Friend _X As UShort
            Friend _Y As UShort

            ''' <summary>
            ''' The page number in the gump that this object belongs on.
            ''' </summary>
            Public ReadOnly Property Page As Page
                Get
                    Return _Page
                End Get
            End Property

            ''' <summary>
            ''' The X axis position, relative to the gump's 0,0
            ''' </summary>
            Public ReadOnly Property X As UShort
                Get
                    Return _X
                End Get
            End Property

            ''' <summary>
            ''' The Y axis position, relative to the gump's 0,0
            ''' </summary>
            Public ReadOnly Property Y As UShort
                Get
                    Return _Y
                End Get
            End Property

        End Class

        'Button [x] [y] [released-id] [pressed-id] [quit] [page-id] [return-value]
        Public NotInheritable Class Button
            Inherits GumpObject

            Private _ReleasedID As UInteger
            Private _PressedID As UInteger
            Private _Quit As Boolean
            Private _PageID As UShort
            Private _ReturnValue As UInteger

            Friend Sub New(ByRef Page As Page,
                           ByRef X As UShort,
                           ByRef Y As UShort,
                           ByRef ReleasedID As UInteger,
                           ByRef PressedID As UInteger,
                           ByRef Quit As UInteger,
                           ByRef PageID As UInteger,
                           ByRef ReturnValue As UInteger)
                _Page = Page
                _X = X
                _Y = Y
                _ReleasedID = ReleasedID
                _PressedID = PressedID
                _Quit = Quit
                _PageID = PageID
                _ReturnValue = ReturnValue
            End Sub

            Public ReadOnly Property ReleasedID As UInteger
                Get
                    Return _ReleasedID
                End Get
            End Property

            Public ReadOnly Property PressedID As UInteger
                Get
                    Return _PressedID
                End Get
            End Property

            Public ReadOnly Property Quit As Boolean
                Get
                    Return _Quit
                End Get
            End Property

            Public ReadOnly Property PageID As UShort
                Get
                    Return _PageID
                End Get
            End Property

            Public ReadOnly Property ReturnValue As UInteger
                Get
                    Return _ReturnValue
                End Get
            End Property

        End Class

        'GumpPic [x] [y] [id] <[color]>
        Public NotInheritable Class GumpPic
            Inherits GumpObject
            Private _ArtID As UShort
            Private _Hue As UShort

            Public Sub New(ByRef Page As Page, ByRef X As UInteger, ByRef Y As UInteger, ByRef ArtID As UShort, Optional ByRef Hue As UShort = 0)
                _Page = Page
                _X = X
                _Y = Y
                _ArtID = ArtID
                _Hue = Hue
            End Sub

            Public ReadOnly Property ArtID As UShort
                Get
                    Return _ArtID
                End Get
            End Property

            Public ReadOnly Property Hue As UShort
                Get
                    Return _Hue
                End Get
            End Property

        End Class

        'CroppedText [x] [y] [width] [height] [color] [text-id]
        Public NotInheritable Class CroppedText
            Inherits GumpObject
            Private _Width As UShort
            Private _Height As UShort
            Private _Hue As UShort
            Private _TextID As UInteger

            Friend Sub New(ByRef Page As Page, ByRef X As UInteger, ByRef Y As UInteger, ByRef Width As UShort, ByRef Height As UShort, ByRef Hue As UShort, ByRef TextID As UInteger)
                _Page = Page
                _X = X
                _Y = Y
                _Width = Width
                _Height = Height
                _Hue = Hue
                _TextID = TextID
            End Sub

            Public ReadOnly Property Width As UShort
                Get
                    Return _Width
                End Get
            End Property

            Public ReadOnly Property Height As UShort
                Get
                    Return _Height
                End Get
            End Property

            Public ReadOnly Property Hue As UShort
                Get
                    Return _Hue
                End Get
            End Property

            Public ReadOnly Property TextID As UInteger
                Get
                    Return _TextID
                End Get
            End Property

            Public ReadOnly Property Text As String
                Get
                    Return _Page._Gump.Text(_TextID)
                End Get
            End Property

        End Class



    End Class

End Namespace