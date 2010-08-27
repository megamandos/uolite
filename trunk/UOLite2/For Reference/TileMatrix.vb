Imports System.Collections
Imports System.IO
Imports Microsoft.Win32.SafeHandles

Namespace Ultima
    Public Class TileMatrix
        Private m_StaticTiles As HuedTile()()()()()
        Private m_LandTiles As Tile()()()

        Private m_InvalidLandBlock As Tile()
        Private m_EmptyStaticBlock As HuedTile()()()

        Private m_Map As FileStream

        Private m_Index As FileStream
        Private m_IndexReader As BinaryReader

        Private m_Statics As FileStream

        Private m_BlockWidth As Integer, m_BlockHeight As Integer
        Private m_Width As Integer, m_Height As Integer

        Private m_Patch As TileMatrixPatch

        Public ReadOnly Property Patch() As TileMatrixPatch
            Get
                Return m_Patch
            End Get
        End Property

        Public ReadOnly Property BlockWidth() As Integer
            Get
                Return m_BlockWidth
            End Get
        End Property

        Public ReadOnly Property BlockHeight() As Integer
            Get
                Return m_BlockHeight
            End Get
        End Property

        Public ReadOnly Property Width() As Integer
            Get
                Return m_Width
            End Get
        End Property

        Public ReadOnly Property Height() As Integer
            Get
                Return m_Height
            End Get
        End Property

        Public Sub New(ByVal fileIndex As Integer, ByVal mapID As Integer, ByVal width As Integer, ByVal height As Integer)
            m_Width = width
            m_Height = height
            m_BlockWidth = width >> 3
            m_BlockHeight = height >> 3

            If fileIndex <> &H7F Then
                Dim mapPath As String = Client.GetFilePath("map{0}.mul", fileIndex)

                If mapPath IsNot Nothing Then
                    m_Map = New FileStream(mapPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                End If

                Dim indexPath As String = Client.GetFilePath("staidx{0}.mul", fileIndex)

                If indexPath IsNot Nothing Then
                    m_Index = New FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                    m_IndexReader = New BinaryReader(m_Index)
                End If

                Dim staticsPath As String = Client.GetFilePath("statics{0}.mul", fileIndex)

                If staticsPath IsNot Nothing Then
                    m_Statics = New FileStream(staticsPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                End If
            End If

            m_EmptyStaticBlock = New HuedTile(7)()() {}

            For i As Integer = 0 To 7
                m_EmptyStaticBlock(i) = New HuedTile(7)() {}

                For j As Integer = 0 To 7
                    m_EmptyStaticBlock(i)(j) = New HuedTile(-1) {}
                Next
            Next

            m_InvalidLandBlock = New Tile(195) {}

            m_LandTiles = New Tile(m_BlockWidth - 1)()() {}
            m_StaticTiles = New HuedTile(m_BlockWidth - 1)()()()() {}


            'for ( int i = 0; i < m_BlockWidth; ++i )
            '			{
            '				m_LandTiles[i] = new Tile[m_BlockHeight][];
            '				m_StaticTiles[i] = new Tile[m_BlockHeight][][][];
            '			}

            m_Patch = New TileMatrixPatch(Me, mapID)
        End Sub

        Public ReadOnly Property EmptyStaticBlock() As HuedTile()()()
            Get
                Return m_EmptyStaticBlock
            End Get
        End Property

        Public Sub SetStaticBlock(ByVal x As Integer, ByVal y As Integer, ByVal value As HuedTile()()())
            If x < 0 OrElse y < 0 OrElse x >= m_BlockWidth OrElse y >= m_BlockHeight Then
                Return
            End If

            If m_StaticTiles(x) Is Nothing Then
                m_StaticTiles(x) = New HuedTile(m_BlockHeight - 1)()()() {}
            End If

            m_StaticTiles(x)(y) = value
        End Sub

        Public Function GetStaticBlock(ByVal x As Integer, ByVal y As Integer) As HuedTile()()()
            If x < 0 OrElse y < 0 OrElse x >= m_BlockWidth OrElse y >= m_BlockHeight OrElse m_Statics Is Nothing OrElse m_Index Is Nothing Then
                Return m_EmptyStaticBlock
            End If

            If m_StaticTiles(x) Is Nothing Then
                m_StaticTiles(x) = New HuedTile(m_BlockHeight - 1)()()() {}
            End If

            Dim tiles As HuedTile()()() = m_StaticTiles(x)(y)

            If tiles Is Nothing Then
                tiles = InlineAssignHelper(m_StaticTiles(x)(y), ReadStaticBlock(x, y))
            End If

            Return tiles
        End Function

        Public Function GetStaticTiles(ByVal x As Integer, ByVal y As Integer) As HuedTile()
            Dim tiles As HuedTile()()() = GetStaticBlock(x >> 3, y >> 3)

            Return tiles(x And &H7)(y And &H7)
        End Function

        Public Sub SetLandBlock(ByVal x As Integer, ByVal y As Integer, ByVal value As Tile())
            If x < 0 OrElse y < 0 OrElse x >= m_BlockWidth OrElse y >= m_BlockHeight Then
                Return
            End If

            If m_LandTiles(x) Is Nothing Then
                m_LandTiles(x) = New Tile(m_BlockHeight - 1)() {}
            End If

            m_LandTiles(x)(y) = value
        End Sub

        Public Function GetLandBlock(ByVal x As Integer, ByVal y As Integer) As Tile()
            If x < 0 OrElse y < 0 OrElse x >= m_BlockWidth OrElse y >= m_BlockHeight OrElse m_Map Is Nothing Then
                Return m_InvalidLandBlock
            End If

            If m_LandTiles(x) Is Nothing Then
                m_LandTiles(x) = New Tile(m_BlockHeight - 1)() {}
            End If

            Dim tiles As Tile() = m_LandTiles(x)(y)

            If tiles Is Nothing Then
                tiles = InlineAssignHelper(m_LandTiles(x)(y), ReadLandBlock(x, y))
            End If

            Return tiles
        End Function

        Public Function GetLandTile(ByVal x As Integer, ByVal y As Integer) As Tile
            Dim tiles As Tile() = GetLandBlock(x >> 3, y >> 3)

            Return tiles(((y And &H7) << 3) + (x And &H7))
        End Function

        Private Shared m_Lists As HuedTileList()()

        Private Function ReadStaticBlock(ByVal x As Integer, ByVal y As Integer) As HuedTile()()()
            m_IndexReader.BaseStream.Seek(((x * m_BlockHeight) + y) * 12, SeekOrigin.Begin)

            Dim lookup As Integer = m_IndexReader.ReadInt32()
            Dim length As Integer = m_IndexReader.ReadInt32()

            If lookup < 0 OrElse length <= 0 Then
                Return m_EmptyStaticBlock
            Else
                Dim count As Integer = length \ 7

                m_Statics.Seek(lookup, SeekOrigin.Begin)

                Dim staTiles As StaticTile() = New StaticTile(count - 1) {}

                NativeMethods._lread(m_Statics.SafeFileHandle, pTiles, length)

                If m_Lists Is Nothing Then
                    m_Lists = New HuedTileList(7)() {}

                    For i As Integer = 0 To 7
                        m_Lists(i) = New HuedTileList(7) {}

                        For j As Integer = 0 To 7
                            m_Lists(i)(j) = New HuedTileList()
                        Next
                    Next
                End If

                Dim lists As HuedTileList()() = m_Lists

                Dim pCur As Pointer(Of StaticTile) = pTiles, pEnd As Pointer(Of StaticTile) = pTiles + count

                While pCur < pEnd
                    lists(pCur.m_X And &H7)(pCur.m_Y And &H7).Add(CShort((pCur.m_ID And &H3FFF) + &H4000), pCur.m_Hue, pCur.m_Z)
                    pCur += 1
                End While

                Dim tiles As HuedTile()()() = New HuedTile(7)()() {}

                For i As Integer = 0 To 7
                    tiles(i) = New HuedTile(7)() {}

                    For j As Integer = 0 To 7
                        tiles(i)(j) = lists(i)(j).ToArray()
                    Next
                Next

                Return tiles

            End If
        End Function

        Private Function ReadLandBlock(ByVal x As Integer, ByVal y As Integer) As Tile()
            m_Map.Seek(((x * m_BlockHeight) + y) * 196 + 4, SeekOrigin.Begin)

            Dim tiles As Tile() = New Tile(63) {}

            NativeMethods._lread(m_Map.SafeFileHandle, pTiles, 192)


            Return tiles
        End Function

        Public Sub Dispose()
            If m_Map IsNot Nothing Then
                m_Map.Close()
            End If

            If m_Statics IsNot Nothing Then
                m_Statics.Close()
            End If

            If m_IndexReader IsNot Nothing Then
                m_IndexReader.Close()
            End If
        End Sub
        Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
            target = value
            Return value
        End Function
    End Class

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack:=1)> _
    Public Structure StaticTile
        Public m_ID As Short
        Public m_X As Byte
        Public m_Y As Byte
        Public m_Z As SByte
        Public m_Hue As Short
    End Structure

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack:=1)> _
    Public Structure HuedTile
        Friend m_ID As Short
        Friend m_Hue As Short
        Friend m_Z As SByte

        Public ReadOnly Property ID() As Integer
            Get
                Return m_ID
            End Get
        End Property

        Public ReadOnly Property Hue() As Integer
            Get
                Return m_Hue
            End Get
        End Property

        Public Property Z() As Integer
            Get
                Return m_Z
            End Get
            Set(ByVal value As Integer)
                m_Z = CSByte(value)
            End Set
        End Property

        Public Sub New(ByVal id As Short, ByVal hue As Short, ByVal z As SByte)
            m_ID = id
            m_Hue = hue
            m_Z = z
        End Sub

        Public Sub [Set](ByVal id As Short, ByVal hue As Short, ByVal z As SByte)
            m_ID = id
            m_Hue = hue
            m_Z = z
        End Sub
    End Structure

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack:=1)> _
    Public Structure Tile
        Implements IComparable
        Friend m_ID As Short
        Friend m_Z As SByte

        Public ReadOnly Property ID() As Integer
            Get
                Return m_ID
            End Get
        End Property

        Public Property Z() As Integer
            Get
                Return m_Z
            End Get
            Set(ByVal value As Integer)
                m_Z = CSByte(value)
            End Set
        End Property

        Public ReadOnly Property Ignored() As Boolean
            Get
                Return (m_ID = 2 OrElse m_ID = &H1DB OrElse (m_ID >= &H1AE AndAlso m_ID <= &H1B5))
            End Get
        End Property

        Public Sub New(ByVal id As Short, ByVal z As SByte)
            m_ID = id
            m_Z = z
        End Sub

        Public Sub [Set](ByVal id As Short, ByVal z As SByte)
            m_ID = id
            m_Z = z
        End Sub

        Public Function CompareTo(ByVal x As Object) As Integer Implements IComparable.CompareTo
            If x Is Nothing Then
                Return 1
            End If

            If Not (TypeOf x Is Tile) Then
                Throw New ArgumentNullException()
            End If

            Dim a As Tile = CType(x, Tile)

            If m_Z > a.m_Z Then
                Return 1
            ElseIf a.m_Z > m_Z Then
                Return -1
            End If

            Dim ourData As ItemData = TileData.ItemTable(m_ID And &H3FFF)
            Dim theirData As ItemData = TileData.ItemTable(a.m_ID And &H3FFF)

            If ourData.Height > theirData.Height Then
                Return 1
            ElseIf theirData.Height > ourData.Height Then
                Return -1
            End If

            If ourData.Background AndAlso Not theirData.Background Then
                Return -1
            ElseIf theirData.Background AndAlso Not ourData.Background Then
                Return 1
            End If

            Return 0
        End Function
    End Structure
End Namespace