Imports System.IO

#If DEBUG Then

#Const DebugTileDataLoad = False

#End If


Partial Class LiteClient

    Private _TileData As SupportClasses.TileData
    Private Maps As New Hashtable
    Private MapDefinitions As New Hashtable

    Private _CurrentMap As Enums.Facets

    Public ReadOnly Property CurrentMap As SupportClasses.Map
        Get
            Return Maps(_CurrentMap)
        End Get
    End Property

    Private Sub RegisterMaps()
        RegisterMap(0, 0, 0, 7168, 4096, 4, "Felucca")
        RegisterMap(1, 1, 1, 7168, 4096, 0, "Trammel")
        RegisterMap(2, 2, 2, 2304, 1600, 1, "Ilshenar")
        RegisterMap(3, 3, 3, 2560, 2048, 1, "Malas")
        RegisterMap(4, 4, 4, 1448, 1448, 1, "Tokuno")
        RegisterMap(5, 5, 5, 1280, 4096, 1, "TerMur")
    End Sub

    ''' <summary>
    ''' Registers a map with the client, telling it that a map exists. This must be done for all maps the client will be using.
    ''' </summary>
    ''' <param name="Index">An unreserved unique index for this map.</param>
    ''' <param name="MapID">An identification number used in client communications. For any visible maps this must be between 0-3.</param>
    ''' <param name="FileIndex">A file identification number, this must be 0,2,3, or 4 for visible maps.</param>
    ''' <param name="Width">The width of the map.</param>
    ''' <param name="Height">The height of the map.</param>
    ''' <param name="Name">The name of the map, currently there is no use for this.</param>
    Private Sub RegisterMap(ByRef Index As UShort, ByRef MapID As UShort, ByRef FileIndex As UShort, ByRef Width As UShort, ByRef Height As UShort, ByRef Season As UShort, ByRef Name As String)
        Dim NewMapDef As New SupportStructures.MapDefinition With {.Index = Index,
                                                                .MapID = MapID,
                                                                .FileIndex = FileIndex,
                                                                .Width = Width,
                                                                .Height = Height,
                                                                .Season = Season,
                                                                .Name = Name}
        MapDefinitions.Add(Index, NewMapDef)

        Dim NewMap As New SupportClasses.Map(NewMapDef)
        Maps.Add(Index, NewMap)
    End Sub

End Class

Namespace SupportClasses
    Public Class OLDMap
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

    Public Class Map
        Private _VisibleArea As VisibleMap

        Public ReadOnly Property VisibleArea As VisibleMap
            Get
                Return _VisibleArea
            End Get
        End Property

        Private _Facet As Enums.Facets

        Public ReadOnly Property Facet
            Get
                Return _Facet
            End Get
        End Property

        Private _Definition As SupportStructures.MapDefinition

        Friend Sub New(ByRef Definition As SupportStructures.MapDefinition, ByRef TileData As TileData)
            _VisibleArea = New VisibleMap(Definition, TileData)
            _Definition = Definition
        End Sub

        Public ReadOnly Property Height As UShort
            Get
                Return _Definition.Height
            End Get
        End Property

        Public ReadOnly Property Width As UShort
            Get
                Return _Definition.Width
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                Return _Definition.Name
            End Get
        End Property

        Public ReadOnly Property Index As UShort
            Get
                Return _Definition.Index
            End Get
        End Property

        Public ReadOnly Property MapID As UShort
            Get
                Return _Definition.MapID
            End Get
        End Property

        Public ReadOnly Property FileIndex As UShort
            Get
                Return _Definition.FileIndex
            End Get
        End Property

    End Class

    ''' <summary>
    ''' This is a 128x128 section of the map, as in the real client. This means that 
    ''' when the player is within 20 tiles of the edge of the visible section, the AutoXY will trigger and move 
    ''' the visible section by 64 tiles in w/e direction the player is facing.
    ''' </summary>
    Public Class VisibleMap
        Friend _TileData As TileData
        Private CellCollection As New Hashtable

        Private Sub AddCell(ByRef Tile As SupportStructures.Tile, ByRef X As UShort, ByRef Y As UShort)
            Dim LocalID As UInt32 = ((X - CurrentX) * 65535) + ((Y - CurrentY) * 256) + (Convert.ToByte(Tile._Z))
            Dim NewCell As New MapCell(Tile, X, Y, _TileData)
            CellCollection.Add(LocalID, NewCell)
        End Sub

        Private _Range As Byte = 20

        ''' <summary>
        ''' Sets the map load triggers, as this distance the next section of the map is loaded when moving. Sets from 10 to 20 tiles.
        ''' </summary>
        Public Property Range As Byte
            Get
                Return _Range
            End Get
            Set(ByVal value As Byte)
                'If set aboove 20, will set to 20. If set below 10, will set to 10.
                'Otherwise would accept it.
                Select Case value
                    Case Is > 20
                        _Range = 20
                    Case Is <= 20
                        _Range = value
                    Case Is >= 10
                        _Range = value
                    Case Is < 10
                        _Range = 10
                End Select
            End Set
        End Property

        Private _Definition As SupportStructures.MapDefinition
        Private _DataFolderPath As String = System.Reflection.Assembly.GetExecutingAssembly.Location & "\content"

        Public Sub New(ByRef Definition As SupportStructures.MapDefinition, ByRef TileData As TileData)
            _Definition = Definition
            _TileData = TileData
        End Sub

        ''' <summary>
        ''' This is where the top left x position of this visible block goes on the real map.
        ''' </summary>
        Public ReadOnly Property CurrentX As UShort
            Get
                Return _CurrentX
            End Get
        End Property
        Private _CurrentX As UShort = 0

        ''' <summary>
        ''' This is where the top left y position of this visible block does on the real map.
        ''' </summary>
        Public ReadOnly Property CurrentY As UShort
            Get
                Return _CurrentY
            End Get
        End Property
        Private _CurrentY As UShort = 0

        ''' <summary>
        ''' Use this to move the visible section of the map to a new location. Be careful when using this, 
        ''' don't move it to an area that the player is not on! You should use <see cref="AutoXY">AutoXY</see> instead. 
        ''' Setting a new X,Y will cause UOLite to load the new sections of the map, this generaly consists of loading 
        ''' two 64x64 sections of the map (8192 tiles) and unloading two more (8192 tiles). If you try to set this 
        ''' to a section of the map that doesnt exist or doesnt fall on 64x64 boundries, it will silently fail. If you 
        ''' set this to the right or bottom edge of the map it will reset to the left or top edge of the map accordingly.
        ''' </summary>
        ''' <param name="X">The x offset of this on the real map.</param>
        ''' <param name="Y">The y offset of this on the real map.</param>
        Public Sub SetXY(ByRef X As UShort, ByRef Y As UShort)


        End Sub

        ''' <summary>
        ''' This will check the trigger lines and move the visible section of the map automaticaly. This is 
        ''' the recomended way of moving the visible section of the map.
        ''' </summary>
        ''' <param name="PlayerX">The X position of the player.</param>
        ''' <param name="PlayerY">The Y position of the player.</param>
        Public Sub AutoXY(ByRef PlayerX As UShort, ByRef PlayerY As UShort)
            'TODO: add checks to load new sections when crossing the 64x64 tile trigger boundries.
        End Sub

    End Class

    Public Class MapCell
        Private _Tile As SupportStructures.Tile
        Private _TileData As TileData
        Private _X As UShort
        Private _Y As UShort
        Private _Z As SByte

        Friend Sub New(ByRef Tile As SupportStructures.Tile, ByRef X As UShort, ByRef Y As UShort, ByRef TileData As TileData)
            _Tile = Tile
            _TileData = TileData
            _Z = Tile._Z
            _X = X
            _Y = Y
        End Sub

        Public ReadOnly Property Flags As SupportStructures.TileFlagStruct
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._Flags
                Else
                    Return _TileData.LandTiles(_Tile._TileID)._Flags
                End If
            End Get
        End Property

        Public ReadOnly Property Weight As Byte
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._Weight
                Else
                    Return 255
                End If
            End Get
        End Property

        Public ReadOnly Property QualityLayerLightID As Byte
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._Quality
                Else
                    Return 0
                End If
            End Get
        End Property

        Public ReadOnly Property QuantityClass As Byte
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._Quantity
                Else
                    Return 1
                End If
            End Get
        End Property

        Public ReadOnly Property ArtID As UShort
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._AnimID
                Else
                    Return 0
                End If
            End Get
        End Property

        Public ReadOnly Property Hue As UShort
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._Hue
                Else
                    Return 0
                End If
            End Get
        End Property

        Public ReadOnly Property HeightCapacity As Byte
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._Height
                Else
                    Return 1
                End If
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                If _Tile._TileType = Enums.TileType.StaticTile Then
                    Return _TileData.StaticTiles(_Tile._TileID)._Name
                Else
                    Return _TileData.LandTiles(_Tile._TileID)._Name
                End If
            End Get
        End Property

    End Class

End Namespace

Namespace SupportStructures
    Public Structure MapDefinition
        Public Index As UShort
        Public MapID As UShort
        Public FileIndex As UShort
        Public Width As UShort
        Public Height As UShort
        Public Name As String
        Public Season As UShort
    End Structure

    Public Structure Tile
        Friend _TileID As UShort
        Friend _TileType As Enums.TileType
        Friend _Z As SByte
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