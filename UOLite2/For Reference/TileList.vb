Namespace Ultima
    Public Class HuedTileList
        Private m_Tiles As HuedTile()
        Private m_Count As Integer

        Public Sub New()
            m_Tiles = New HuedTile(7) {}
            m_Count = 0
        End Sub

        Public ReadOnly Property Count() As Integer
            Get
                Return m_Count
            End Get
        End Property

        Public Sub Add(ByVal id As Short, ByVal hue As Short, ByVal z As SByte)
            If (m_Count + 1) > m_Tiles.Length Then
                Dim old As HuedTile() = m_Tiles
                m_Tiles = New HuedTile(old.Length * 2 - 1) {}

                For i As Integer = 0 To old.Length - 1
                    m_Tiles(i) = old(i)
                Next
            End If

            m_Tiles(System.Math.Max(System.Threading.Interlocked.Increment(m_Count), m_Count - 1)).[Set](id, hue, z)
        End Sub

        Public Function ToArray() As HuedTile()
            Dim tiles As HuedTile() = New HuedTile(m_Count - 1) {}

            For i As Integer = 0 To m_Count - 1
                tiles(i) = m_Tiles(i)
            Next

            m_Count = 0

            Return tiles
        End Function
    End Class

    Public Class TileList
        Private m_Tiles As Tile()
        Private m_Count As Integer

        Public Sub New()
            m_Tiles = New Tile(7) {}
            m_Count = 0
        End Sub

        Public ReadOnly Property Count() As Integer
            Get
                Return m_Count
            End Get
        End Property

        Public Sub Add(ByVal id As Short, ByVal z As SByte)
            If (m_Count + 1) > m_Tiles.Length Then
                Dim old As Tile() = m_Tiles
                m_Tiles = New Tile(old.Length * 2 - 1) {}

                For i As Integer = 0 To old.Length - 1
                    m_Tiles(i) = old(i)
                Next
            End If

            m_Tiles(System.Math.Max(System.Threading.Interlocked.Increment(m_Count), m_Count - 1)).[Set](id, z)
        End Sub

        Public Function ToArray() As Tile()
            Dim tiles As Tile() = New Tile(m_Count - 1) {}

            For i As Integer = 0 To m_Count - 1
                tiles(i) = m_Tiles(i)
            Next

            m_Count = 0

            Return tiles
        End Function
    End Class
End Namespace