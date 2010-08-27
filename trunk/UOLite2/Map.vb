Namespace SupportClasses
    Public Class Map
        Private _Height As UShort
        Private _Width As UShort
        Private _FileIndex As Byte
        Private _MapIndex As Byte
        Private _Tiles(0, 0, 0) As SupportStructures.Tile

        Public Sub New(ByRef FileIndex As Byte, ByRef MapIndex As Byte, ByRef Height As UShort, ByRef Width As UShort)
            _Height = Height
            _Width = Width
            _FileIndex = FileIndex
            _MapIndex = MapIndex

            ReDim _Tiles(Height, Width, 255)

            Dim Patch(8, 8, 255) As SupportStructures.Tile



        End Sub


    End Class

End Namespace

Namespace SupportStructures
    Public Structure Tile
        Public Hue As UShort
        Public ID As UShort
        Public X As UShort
        Public Y As UShort
        Public Z As UShort
    End Structure
End Namespace