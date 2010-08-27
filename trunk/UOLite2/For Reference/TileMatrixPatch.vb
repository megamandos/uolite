Imports System.IO
Imports System.Collections
Imports Microsoft.Win32.SafeHandles

Namespace Ultima
    Public Class TileMatrixPatch
        Private m_LandBlocks As Integer, m_StaticBlocks As Integer

        Public ReadOnly Property LandBlocks() As Integer
            Get
                Return m_LandBlocks
            End Get
        End Property

        Public ReadOnly Property StaticBlocks() As Integer
            Get
                Return m_StaticBlocks
            End Get
        End Property

        Public Sub New(ByVal matrix As TileMatrix, ByVal index As Integer)
            Dim mapDataPath As String = Client.GetFilePath("mapdif{0}.mul", index)
            Dim mapIndexPath As String = Client.GetFilePath("mapdifl{0}.mul", index)

            If mapDataPath IsNot Nothing AndAlso mapIndexPath IsNot Nothing Then
                m_LandBlocks = PatchLand(matrix, mapDataPath, mapIndexPath)
            End If

            Dim staDataPath As String = Client.GetFilePath("stadif{0}.mul", index)
            Dim staIndexPath As String = Client.GetFilePath("stadifl{0}.mul", index)
            Dim staLookupPath As String = Client.GetFilePath("stadifi{0}.mul", index)

            If staDataPath IsNot Nothing AndAlso staIndexPath IsNot Nothing AndAlso staLookupPath IsNot Nothing Then
                m_StaticBlocks = PatchStatics(matrix, staDataPath, staIndexPath, staLookupPath)
            End If
        End Sub

        Private Function PatchLand(ByVal matrix As TileMatrix, ByVal dataPath As String, ByVal indexPath As String) As Integer
            Using fsData As New FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using fsIndex As New FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Dim indexReader As New BinaryReader(fsIndex)

                    Dim count As Integer = CInt(indexReader.BaseStream.Length \ 4)

                    For i As Integer = 0 To count - 1
                        Dim blockID As Integer = indexReader.ReadInt32()
                        Dim x As Integer = blockID / matrix.BlockHeight
                        Dim y As Integer = blockID Mod matrix.BlockHeight

                        fsData.Seek(4, SeekOrigin.Current)

                        Dim tiles As Tile() = New Tile(63) {}

                        NativeMethods._lread(fsData.SafeFileHandle, pTiles, 192)


                        matrix.SetLandBlock(x, y, tiles)
                    Next

                    Return count
                End Using
            End Using
        End Function

        Private Function PatchStatics(ByVal matrix As TileMatrix, ByVal dataPath As String, ByVal indexPath As String, ByVal lookupPath As String) As Integer
            Using fsData As New FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Using fsIndex As New FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    Using fsLookup As New FileStream(lookupPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                        Dim indexReader As New BinaryReader(fsIndex)
                        Dim lookupReader As New BinaryReader(fsLookup)

                        Dim count As Integer = CInt(indexReader.BaseStream.Length \ 4)

                        Dim lists As HuedTileList()() = New HuedTileList(7)() {}

                        For x As Integer = 0 To 7
                            lists(x) = New HuedTileList(7) {}

                            For y As Integer = 0 To 7
                                lists(x)(y) = New HuedTileList()
                            Next
                        Next

                        For i As Integer = 0 To count - 1
                            Dim blockID As Integer = indexReader.ReadInt32()
                            Dim blockX As Integer = blockID / matrix.BlockHeight
                            Dim blockY As Integer = blockID Mod matrix.BlockHeight

                            Dim offset As Integer = lookupReader.ReadInt32()
                            Dim length As Integer = lookupReader.ReadInt32()
                            lookupReader.ReadInt32()
                            ' Extra
                            If offset < 0 OrElse length <= 0 Then
                                matrix.SetStaticBlock(blockX, blockY, matrix.EmptyStaticBlock)
                                Continue For
                            End If

                            fsData.Seek(offset, SeekOrigin.Begin)

                            Dim tileCount As Integer = length \ 7

                            Dim staTiles As StaticTile() = New StaticTile(tileCount - 1) {}

                            NativeMethods._lread(fsData.SafeFileHandle, pTiles, length)

                            Dim pCur As Pointer(Of StaticTile) = pTiles, pEnd As Pointer(Of StaticTile) = pTiles + tileCount

                            While pCur < pEnd
                                lists(pCur.m_X And &H7)(pCur.m_Y And &H7).Add(CShort((pCur.m_ID And &H3FFF) + &H4000), pCur.m_Hue, pCur.m_Z)
                                pCur += 1
                            End While

                            Dim tiles As HuedTile()()() = New HuedTile(7)()() {}

                            For x As Integer = 0 To 7
                                tiles(x) = New HuedTile(7)() {}

                                For y As Integer = 0 To 7
                                    tiles(x)(y) = lists(x)(y).ToArray()
                                Next
                            Next

                            matrix.SetStaticBlock(blockX, blockY, tiles)

                        Next

                        Return count
                    End Using
                End Using
            End Using
        End Function
    End Class
End Namespace