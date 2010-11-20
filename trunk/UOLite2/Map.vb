Imports System.IO

#If DEBUG Then

#Const DebugTileDataLoad = False

#End If


Partial Class LiteClient

    Private _TileData As SupportClasses.TileData
    Private _CurrentMap As SupportClasses.Map
    'Private _Map As SupportClasses.MapPatch

End Class

Namespace SupportClasses
    Public Class Map
        Friend _MapFile As String, _StaticsFile As String, _StaIDXFile As String
        'Private _Tiles As New Dictionary(Of UInt64, SupportStructures.Tile)(16384) '4 64x64 sections of (4096) cells total.

        Private _TopLeft As MapPatch
        Private _TopRight As MapPatch
        Private _BottomLeft As MapPatch
        Private _BottomRight As MapPatch

        Private _Map As Map
        Protected Friend _MapHeight As UShort
        Protected Friend _MapWidth As UShort
        Private WithEvents _Player As Mobile

        Public Sub New(ByRef MapPath As String, ByRef StaticsPath As String, ByRef StaIDXPath As String, ByRef Width As UShort, ByRef Height As UShort)
            If Height Mod 64 <> 0 Then Throw New ApplicationException("Map Height must be a multiple of 64!")
            If Width Mod 64 <> 0 Then Throw New ApplicationException("Map Width must be a multiple of 64!")
            _MapHeight = Height
            _MapWidth = Width
            _MapFile = MapPath
            _StaticsFile = StaticsPath
            _StaIDXFile = StaIDXPath
        End Sub

        Public Sub LoadPatch(ByRef StartX As UShort, ByRef StartY As UShort, ByRef Width As Byte, ByRef Height As Byte)
            Dim TBT As SupportStructures.Tile
            Dim Z As SByte
            Dim PosKey As UInt64
            Dim XKey As UInt64
            Dim YKey As UInt64

            Using bin As New BinaryReader(New FileStream(_Map._MapFile, FileMode.Open, FileAccess.Read, FileShare.Read))

                For X As UShort = 0 To Width - 9 Step 8
                    For Y As UShort = 0 To Height - 9 Step 8
                        Try

                            'Read the header, and ignore it.
                            bin.ReadUInt32()

                            For BY As Byte = 0 To 7

                                For BX As Byte = 0 To 7

                                    TBT = New SupportStructures.Tile
                                    TBT._TileID = bin.ReadUInt16
                                    Z = bin.ReadSByte

                                    'Get the Tile's key and add it to the dictionary.
                                    ' _Tiles.Add(GetKey(BX + X, BY + Y, Z), TBT)

                                    '_Tiles(BX + X, BY + Y) = New TileStack
                                    '_Tiles(BX + X, BY + Y).Add(TBT)

                                    '_Tiles(BX, BY)._LandTileID = bin.ReadUInt16
                                    '_Tiles(BX, BY)._Z = bin.ReadSByte
                                Next
                            Next

                        Catch ex As Exception
                            Throw New ApplicationException("Invalid Wrong Map Size")
                        End Try
                    Next
                Next

            End Using

            'collect the garbage that this created.
            'GC.Collect()
        End Sub

        Private Function GetBlock(ByRef XOffset As UShort, ByRef YOffset As UShort)


        End Function

        Private Function GetPatch(ByRef XOffset As UShort, ByRef YOffset As UShort) As MapPatch
            If XOffset Mod 64 <> 0 Or YOffset Mod 64 <> 0 Then Throw New ApplicationException("Offsets must be factors of 64!")

            Dim Patch As New MapPatch

            Dim InitialOffset As UInt32 = 0

            'Find the 0,0 position of the 0,0 block.
            'InitialOffset = (XOffset / 8) 

        End Function

        Private Sub _Player_onMove(ByRef Client As LiteClient, ByRef Mobile As Mobile) Handles _Player.onMove
            'TODO: Check movement bounds and load and unload map patches as needed.

            'Don't forget to collect the old map patches.
            GC.Collect()
        End Sub

    End Class

    Public Class TileData
        Private _LandTiles As New Dictionary(Of UShort, SupportStructures.LandTileData)
        Private _StaticTiles As New Dictionary(Of UShort, SupportStructures.StaticTileData)

        Public Sub New(ByRef TileDataMulPath As String)
            'Load up the Land Tile Data
            Dim Land As SupportStructures.LandTileData
            Dim Stat As SupportStructures.StaticTileData
            Dim Flags As UInt32
            Dim Index As UInt32 = 0
            Dim TileName As String
            Dim TextureID As UInt32 = 0

#If DebugTileDataLoad Then
            Debug.WriteLine("Starting Read of TILEDATA.MUL!")
#End If

            Using bin As New BinaryReader(New FileStream(TileDataMulPath & "\tiledata.mul", FileMode.Open, FileAccess.Read, FileShare.Read))
                'bin.ReadInt32()
                'bin.ReadInt16()

                While bin.BaseStream.Position <> 428032
                    'Read the header, and ignore it.
                    bin.ReadUInt32()

                    For i As Integer = 0 To 31
                        'loop 32 times to read the blocks
                        Flags = bin.ReadUInt32
                        TextureID = bin.ReadUInt16
                        TileName = Text.Encoding.ASCII.GetString(bin.ReadBytes(20)).Replace(Chr(0), "")
                        Land = New SupportStructures.LandTileData(Flags, TextureID, TileName)
                        _LandTiles.Add(Index, Land)
                        Index += 1
                    Next

                End While

                Index = 0

                While bin.BaseStream.Length <> bin.BaseStream.Position
                    'Read the header, and ignore it.
                    bin.ReadUInt32()

                    For i As Integer = 0 To 31
                        'loop 32 times to read the blocks
                        Stat = New SupportStructures.StaticTileData

                        Stat._Flags = New SupportStructures.TileFlagStruct(bin.ReadUInt32)
                        Stat._Weight = bin.ReadByte
                        Stat._Quality = bin.ReadByte
                        Stat._Unknown = bin.ReadUInt16
                        Stat._Unknown1 = bin.ReadByte
                        Stat._Quantity = bin.ReadByte
                        Stat._AnimID = bin.ReadUInt16
                        Stat._Unknown2 = bin.ReadByte
                        Stat._Hue = bin.ReadByte
                        Stat._Unknown3 = bin.ReadUInt16
                        Stat._Height = bin.ReadByte
                        Stat._Name = Text.Encoding.ASCII.GetString(bin.ReadBytes(20)).Replace(Chr(0), "")

                        _StaticTiles.Add(Index, Stat)
                        Index += 1
                    Next

                End While

            End Using


#If DebugTileDataLoad Then
            Debug.WriteLine("Land Tiles loaded: " & _LandTiles.Count)
            Debug.WriteLine("Static Tiles Loaded: " & _StaticTiles.Count)
            Debug.WriteLine("TILEDATA.MUL Load Complete!")
#End If

        End Sub

        Public ReadOnly Property LandTiles(ByVal Index As UInt32) As SupportStructures.LandTileData
            Get
                If _LandTiles.ContainsKey(Index) Then
                    Return _LandTiles(Index)
                Else
                    Throw New IndexOutOfRangeException
                End If
            End Get
        End Property

        Public ReadOnly Property LandTileCount As UInt32
            Get
                Return _LandTiles.Count
            End Get
        End Property

        Public ReadOnly Property StaticTiles(ByVal Index As UInt32) As SupportStructures.StaticTileData
            Get
                If _StaticTiles.ContainsKey(Index) Then
                    Return _StaticTiles(Index)
                Else
                    Throw New IndexOutOfRangeException
                End If
            End Get
        End Property

        Public ReadOnly Property StaticTileCount As UInt32
            Get
                Return _StaticTiles.Count
            End Get
        End Property

    End Class

    ''' <summary>This is a single 64x64 grid as a chunk of loaded map and statics.</summary>
    Public Class MapPatch
        Friend _XOffset As UShort
        Friend _YOffset As UShort
        Private Const Width = 64
        Private Const Height = 64

        Friend _Tiles As New Dictionary(Of UInt32, SupportStructures.Tile)

        'OK so a position is calculated internally as a 32-bit unsigned integer to use as a key in its lookup.
        'The integer breakdown is as follows:
        'Bytes:    1|              2|                              3|                               4|
        '            <Reserved>      <X Position on 64x64 Grid>      <Y Position on 64x64 Grid>       <Z Position on 64x64 Grid>

        Private Function GetKey(ByRef X As UShort, ByRef Y As UShort, ByRef Z As Byte) As UInt32
            Return CUInt(X) * 65536 + CUInt(Y) * 256 + CUInt(Z)
        End Function

        Friend Sub Add(ByRef Tile As SupportStructures.Tile, ByRef X As UShort, ByRef Y As UShort, ByRef Z As Byte)

        End Sub

        Public Function HasTile(ByRef X As UShort, ByRef Y As UShort, ByRef Z As Byte) As Boolean
            If _Tiles.ContainsKey(GetKey(X, Y, Z)) Then
                Return True
            Else
                Return False
            End If
        End Function

    End Class

    'This is an 8x8 block on the map. 8x8 is how the .map files store the data.
    Public Class Block
        Public Property XOffset As UShort
        Public Property YOffset As UShort

        Private TileArray(63) As SupportStructures.Tile

        Public ReadOnly Property Tile(ByVal XOffset As UShort, ByVal YOffset As UShort)
            Get

            End Get
        End Property

        Public Sub New(ByVal XOffset As UShort, ByVal YOffset As UShort, ByVal Map As Map)

            Dim StartingPosition As ULong = ((XOffset \ 8) * (Map._MapHeight / 8) * 196) + (YOffset \ 8)

            Using bin As New BinaryReader(New FileStream(Map._MapFile, FileMode.Open, FileAccess.Read, FileShare.Read))

                'Move the read head to the starting position.
                bin.BaseStream.Position = StartingPosition

                'Read and discard the block header.
                bin.ReadUInt32()

                For Y As Byte = 0 To 7
                    For X As Byte = 0 To 7
                        'Load a land tile and add it to the array.
                        TileArray(Y * 8 + X) = New SupportStructures.Tile With {._TileID = bin.ReadUInt16,
                                                                                ._Z = bin.ReadSByte,
                                                                                ._TileType = Enums.TileType.LandTile}


                    Next
                Next

            End Using

        End Sub

    End Class

End Namespace

Namespace SupportStructures
    Public Structure Tile
        Friend _TileID As UShort
        Friend _TileType As Enums.TileType
        Friend _Z As SByte
        'Friend _StaticTileID As UShort
        'Friend _Z As SByte
    End Structure

    Public Structure StaticData
        Private _StaticTileID As UShort
        Private _X As Byte
        Private _Y As Byte
        Private _Z As SByte
        Private _Unknown As UShort
    End Structure

    Public Structure StaticTileData
        Friend _Flags As TileFlagStruct 'See Enums.TileFlags
        Friend _Weight As Byte 'Weight of the item, 255 means not movable.
        Friend _Quality As Byte 'If Wearable, this is a layer. If Light Source, this is Light ID
        Friend _Unknown As UShort
        Friend _Unknown1 As Byte
        Friend _Quantity As Byte 'If this is a weapon, its the weapon class, if this is armor, its the armor class.
        Friend _AnimID As UShort 'The body ID of the animation. add 50,000 and 60,000 respectively to get the two gump indecies associated with this tile.
        Friend _Unknown2 As Byte
        Friend _Hue As Byte 'Perhaps color light?
        Friend _Unknown3 As UShort
        Friend _Height 'If container, this is how much the container can hold.
        Friend _Name As String 'ASCII[20]
    End Structure

    Public Structure LandTileData
        Friend _Flags As TileFlagStruct 'See Enums.TileFlags
        Friend _Name As String 'ASCII[20]
        Friend _TextureID As UShort 'If 0, the land tile has no texture.

        Public Sub New(ByRef Flags As UInt32, ByRef ID As UInt16, ByRef Name As String)
            _Flags = New SupportStructures.TileFlagStruct(Flags)
            _TextureID = ID
            _Name = Name
        End Sub

        Public ReadOnly Property Flags As SupportStructures.TileFlagStruct
            Get
                Return _Flags
            End Get
        End Property

        Public ReadOnly Property ID As UShort
            Get
                Return _TextureID
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                Return _Name
            End Get
        End Property

    End Structure

    Public Structure PalleteEntry
        Private _Red As Byte
        Private _Green As Byte
        Private _Blue As Byte
        Private _Color As System.Drawing.Color

        Friend Sub New(ByRef Red As Byte, ByRef Green As Byte, ByRef Blue As Byte)
            _Red = Red
            _Green = Green
            _Blue = Blue
            _Color = System.Drawing.Color.FromArgb(255, Red, Green, Blue)
        End Sub

        Public ReadOnly Property Color As System.Drawing.Color
            Get
                Return _Color
            End Get
        End Property
    End Structure

    Public Structure TileFlagStruct
        Private _Flags As UInt32

        Friend Sub New(ByRef Flags As UInt32)
            _Flags = Flags
        End Sub

        Public ReadOnly Property Background As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Background)
            End Get
        End Property

        Public ReadOnly Property Weapon As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Weapon)
            End Get
        End Property

        Public ReadOnly Property Transparent As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Transparent)
            End Get
        End Property

        Public ReadOnly Property Translucent As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Translucent)
            End Get
        End Property

        Public ReadOnly Property Wall As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Wall)
            End Get
        End Property

        Public ReadOnly Property Damaging As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Damaging)
            End Get
        End Property

        Public ReadOnly Property Impassable As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Impassable)
            End Get
        End Property

        Public ReadOnly Property Wet As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Wet)
            End Get
        End Property

        Public ReadOnly Property Surface As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Surface)
            End Get
        End Property

        Public ReadOnly Property Bridge As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Bridge)
            End Get
        End Property

        Public ReadOnly Property Stackable As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Stackable)
            End Get
        End Property

        Public ReadOnly Property Window As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Window)
            End Get
        End Property

        Public ReadOnly Property NoShoot As Boolean
            Get
                Return (_Flags And Enums.TileFlags.NoShoot)
            End Get
        End Property

        Public ReadOnly Property PrefixA As Boolean
            Get
                Return (_Flags And Enums.TileFlags.PrefixA)
            End Get
        End Property

        Public ReadOnly Property PrefixAn As Boolean
            Get
                Return (_Flags And Enums.TileFlags.PrefixAn)
            End Get
        End Property

        Public ReadOnly Property Internal As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Internal)
            End Get
        End Property

        Public ReadOnly Property Foliage As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Foliage)
            End Get
        End Property

        Public ReadOnly Property PartiallyHued As Boolean
            Get
                Return (_Flags And Enums.TileFlags.PartiallyHued)
            End Get
        End Property

        Public ReadOnly Property Map As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Map)
            End Get
        End Property

        Public ReadOnly Property Container As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Container)
            End Get
        End Property

        Public ReadOnly Property Wearable As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Wearable)
            End Get
        End Property

        Public ReadOnly Property LightSource As Boolean
            Get
                Return (_Flags And Enums.TileFlags.LightSource)
            End Get
        End Property

        Public ReadOnly Property Animated As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Animated)
            End Get
        End Property

        Public ReadOnly Property NoDiagonal As Boolean
            Get
                Return (_Flags And Enums.TileFlags.NoDiagonal)
            End Get
        End Property

        Public ReadOnly Property Armor As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Armor)
            End Get
        End Property

        Public ReadOnly Property Roof As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Roof)
            End Get
        End Property

        Public ReadOnly Property Door As Boolean
            Get
                Return (_Flags And Enums.TileFlags.Door)
            End Get
        End Property

        Public ReadOnly Property StairBack As Boolean
            Get
                Return (_Flags And Enums.TileFlags.StairBack)
            End Get
        End Property

        Public ReadOnly Property StairRight As Boolean
            Get
                Return (_Flags And Enums.TileFlags.StairRight)
            End Get
        End Property



    End Structure

End Namespace

Namespace Enums
    Public Enum TileType As Byte
        LandTile
        StaticTile
    End Enum

    <Flags()> _
    Public Enum TileFlags As UInt32
        Background = &H1
        Weapon = &H2
        Transparent = &H4
        Translucent = &H8
        Wall = &H10
        Damaging = &H20
        Impassable = &H40
        Wet = &H80
        Surface = &H200
        Bridge = &H400
        Stackable = &H800
        Window = &H1000
        NoShoot = &H2000
        PrefixA = &H4000
        PrefixAn = &H8000
        Internal = &H10000
        Foliage = &H20000
        PartiallyHued = &H40000
        Map = &H100000
        Container = &H200000
        Wearable = &H400000
        LightSource = &H800000
        Animated = &H1000000
        NoDiagonal = &H2000000
        Armor = &H8000000
        Roof = &H10000000
        Door = &H20000000
        StairBack = &H40000000
        StairRight = 2147483648
    End Enum
End Namespace