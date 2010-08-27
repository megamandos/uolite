Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging
Imports Microsoft.Win32.SafeHandles

Namespace Ultima
    Public Class Map
        Private m_Tiles As TileMatrix
        Private m_FileIndex As Integer, m_MapID As Integer
        Private m_Width As Integer, m_Height As Integer

        Private Shared m_Colors As Short()

        Public Shared Property Colors() As Short()
            Get
                Return m_Colors
            End Get
            Set(ByVal value As Short())
                m_Colors = value
            End Set
        End Property

        Public Shared ReadOnly Felucca As New Map(0, 0, 6144, 4096)
        Public Shared ReadOnly Trammel As New Map(0, 1, 6144, 4096)
        Public Shared ReadOnly Ilshenar As New Map(2, 2, 2304, 1600)
        Public Shared ReadOnly Malas As New Map(3, 3, 2560, 2048)
        Public Shared ReadOnly Tokuno As New Map(4, 4, 1448, 1448)
        
        Private Sub New(ByVal fileIndex As Integer, ByVal mapID As Integer, ByVal width As Integer, ByVal height As Integer)
            m_FileIndex = fileIndex
            m_MapID = mapID
            m_Width = width
            m_Height = height
        End Sub

        Public ReadOnly Property LoadedMatrix() As Boolean
            Get
                Return (m_Tiles IsNot Nothing)
            End Get
        End Property

        Public ReadOnly Property Tiles() As TileMatrix
            Get
                If m_Tiles Is Nothing Then
                    m_Tiles = New TileMatrix(m_FileIndex, m_MapID, m_Width, m_Height)
                End If

                Return m_Tiles
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

        Public Function GetImage(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer, ByVal height As Integer) As Bitmap
            Return GetImage(x, y, width, height, True)
        End Function

        Public Function GetImage(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer, ByVal height As Integer, ByVal statics As Boolean) As Bitmap
            Dim bmp As New Bitmap(width << 3, height << 3, PixelFormat.Format16bppRgb555)

            GetImage(x, y, width, height, bmp, statics)

            Return bmp
        End Function

        Private m_Cache As Short()()()
        Private m_Cache_NoStatics As Short()()()
        Private m_Black As Short()

        Private Function GetRenderedBlock(ByVal x As Integer, ByVal y As Integer, ByVal statics As Boolean) As Short()
            Dim matrix As TileMatrix = Me.Tiles

            Dim bw As Integer = matrix.BlockWidth
            Dim bh As Integer = matrix.BlockHeight

            If x < 0 OrElse y < 0 OrElse x >= bw OrElse y >= bh Then
                If m_Black Is Nothing Then
                    m_Black = New Short(63) {}
                End If

                Return m_Black
            End If

            Dim cache As Short()()() = (If(statics, m_Cache, m_Cache_NoStatics))

            If cache Is Nothing Then
                If statics Then
                    m_Cache = InlineAssignHelper(cache, New Short(m_Tiles.BlockHeight - 1)()() {})
                Else
                    m_Cache_NoStatics = New Short(m_Tiles.BlockHeight - 1)()() {}
                End If
            End If

            If cache(y) Is Nothing Then
                cache(y) = New Short(m_Tiles.BlockWidth - 1)() {}
            End If

            Dim data As Short() = cache(y)(x)

            If data Is Nothing Then
                cache(y)(x) = InlineAssignHelper(data, RenderBlock(x, y, statics))
            End If

            Return data
        End Function

        Private Function RenderBlock(ByVal x As Integer, ByVal y As Integer, ByVal drawStatics As Boolean) As Short()
            Dim data As Short() = New Short(63) {}

            Dim tiles As Tile() = m_Tiles.GetLandBlock(x, y)

            Dim pTiles As Pointer(Of Tile) = ptTiles

            Dim pvData As Pointer(Of Short) = pData

            If drawStatics Then
                Dim statics As HuedTile()()() = If(drawStatics, m_Tiles.GetStaticBlock(x, y), Nothing)

                Dim k As Integer = 0, v As Integer = 0
                While k < 8
                    For p As Integer = 0 To 7
                        Dim highTop As Integer = -255
                        Dim highZ As Integer = -255
                        Dim highID As Integer = 0
                        Dim highHue As Integer = 0
                        Dim z As Integer, top As Integer

                        Dim curStatics As HuedTile() = statics(p)(k)

                        If curStatics.Length > 0 Then
                            Dim pStatics As Pointer(Of HuedTile) = phtStatics
                            Dim pStaticsEnd As Pointer(Of HuedTile) = pStatics + curStatics.Length

                            While pStatics < pStaticsEnd
                                z = pStatics.m_Z
                                top = z + pHeight(pStatics.m_ID And &H3FFF)

                                If top > highTop OrElse (z > highZ AndAlso top >= highTop) Then
                                    highTop = top
                                    highZ = z
                                    highID = pStatics.m_ID
                                    highHue = pStatics.m_Hue
                                End If

                                pStatics += 1
                            End While

                        End If

                        top = pTiles.m_Z

                        If top > highTop Then
                            highID = pTiles.m_ID
                            highHue = 0
                        End If

                        If highHue = 0 Then
                            System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target = pColors(highID)
                        Else
                            System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target = Hues.GetHue(highHue - 1).Colors((pColors(highID) >> 10) And &H1F)
                        End If

                        pTiles += 1
                    Next
                    k += 1
                    v += 8
                End While
            Else
                Dim pEnd As Pointer(Of Tile) = pTiles + 64

                While pTiles < pEnd
                    System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target = pColors((System.Math.Max(System.Threading.Interlocked.Increment(pTiles), pTiles - 1)).m_ID)
                End While
            End If





            Return data
        End Function

        Public Sub GetImage(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer, ByVal height As Integer, ByVal bmp As Bitmap)
            GetImage(x, y, width, height, bmp, True)
        End Sub

        Public Sub GetImage(ByVal x As Integer, ByVal y As Integer, ByVal width As Integer, ByVal height As Integer, ByVal bmp As Bitmap, ByVal statics As Boolean)
            If m_Colors Is Nothing Then
                LoadColors()
            End If

            Dim bd As BitmapData = bmp.LockBits(New Rectangle(0, 0, width << 3, height << 3), ImageLockMode.[WriteOnly], PixelFormat.Format16bppRgb555)

            Dim stride As Integer = bd.Stride
            Dim blockStride As Integer = stride << 3

            Dim pStart As Pointer(Of Byte) = CType(bd.Scan0, Pointer(Of Byte))

            Dim oy As Integer = 0, by As Integer = y
            While oy < height
                Dim pRow0 As Pointer(Of Integer) = CType(pStart + (0 * stride), Pointer(Of Integer))
                Dim pRow1 As Pointer(Of Integer) = CType(pStart + (1 * stride), Pointer(Of Integer))
                Dim pRow2 As Pointer(Of Integer) = CType(pStart + (2 * stride), Pointer(Of Integer))
                Dim pRow3 As Pointer(Of Integer) = CType(pStart + (3 * stride), Pointer(Of Integer))
                Dim pRow4 As Pointer(Of Integer) = CType(pStart + (4 * stride), Pointer(Of Integer))
                Dim pRow5 As Pointer(Of Integer) = CType(pStart + (5 * stride), Pointer(Of Integer))
                Dim pRow6 As Pointer(Of Integer) = CType(pStart + (6 * stride), Pointer(Of Integer))
                Dim pRow7 As Pointer(Of Integer) = CType(pStart + (7 * stride), Pointer(Of Integer))

                Dim ox As Integer = 0, bx As Integer = x
                While ox < width
                    Dim data As Short() = GetRenderedBlock(bx, by, statics)

                    Dim pvData As Pointer(Of Integer) = CType(pData, Pointer(Of Integer))

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow0), pRow0 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow0), pRow0 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow0), pRow0 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow0), pRow0 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow1), pRow1 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow1), pRow1 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow1), pRow1 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow1), pRow1 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow2), pRow2 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow2), pRow2 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow2), pRow2 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow2), pRow2 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow3), pRow3 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow3), pRow3 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow3), pRow3 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow3), pRow3 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow4), pRow4 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow4), pRow4 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow4), pRow4 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow4), pRow4 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow5), pRow5 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow5), pRow5 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow5), pRow5 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow5), pRow5 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow6), pRow6 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow6), pRow6 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow6), pRow6 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow6), pRow6 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    System.Math.Max(System.Threading.Interlocked.Increment(pRow7), pRow7 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow7), pRow7 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow7), pRow7 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target
                    System.Math.Max(System.Threading.Interlocked.Increment(pRow7), pRow7 - 1).Target = System.Math.Max(System.Threading.Interlocked.Increment(pvData), pvData - 1).Target

                    ox += 1
                    bx += 1
                End While
                oy += 1
                by += 1
                pStart += blockStride
            End While

            bmp.UnlockBits(bd)
        End Sub

        'public unsafe void GetImage( int x, int y, int width, int height, Bitmap bmp )
        '		{
        '			if ( m_Colors == null )
        '				LoadColors();
        '
        '			TileMatrix matrix = Tiles;
        '
        '			BitmapData bd = bmp.LockBits( new Rectangle( 0, 0, width<<3, height<<3 ), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb555 );
        '
        '			int scanDelta = bd.Stride >> 1;
        '
        '			short *pvDest = (short *)bd.Scan0;
        '
        '			fixed ( short *pColors = m_Colors )
        '			{
        '				fixed ( int *pHeight = TileData.HeightTable )
        '				{
        '					for ( int i = 0; i < width; ++i )
        '					{
        '						pvDest = ((short *)bd.Scan0) + (i << 3);
        '
        '						for ( int j = 0; j < height; ++j )
        '						{
        '							Tile[] tiles = matrix.GetLandBlock( x + i, y + j );
        '							HuedTile[][][] statics = matrix.GetStaticBlock( x + i, y + j );
        '
        '							for ( int k = 0, v = 0; k < 8; ++k, v += 8 )
        '							{
        '								for ( int p = 0; p < 8; ++p )
        '								{
        '									int highTop = -255;
        '									int highZ = -255;
        '									int highID = 0;
        '									int highHue = 0;
        '									int z, top;
        '
        '									HuedTile[] curStatics = statics[p][k];
        '
        '									if ( curStatics.Length > 0 )
        '									{
        '										fixed ( HuedTile *phtStatics = curStatics )
        '										{
        '											HuedTile *pStatics = phtStatics;
        '											HuedTile *pStaticsEnd = pStatics + curStatics.Length;
        '
        '											while ( pStatics < pStaticsEnd )
        '											{
        '												z = pStatics->m_Z;
        '												top = z + pHeight[pStatics->m_ID & 0x3FFF];
        '
        '												if ( top > highTop || (z > highZ && top >= highTop) )
        '												{
        '													highTop = top;
        '													highZ = z;
        '													highID = pStatics->m_ID;
        '													highHue = pStatics->m_Hue;
        '												}
        '
        '												++pStatics;
        '											}
        '										}
        '									}
        '
        '									top = tiles[v + p].Z;
        '
        '									if ( top > highTop )
        '									{
        '										highID = tiles[v + p].ID;
        '										highHue = 0;
        '									}
        '
        '									if ( highHue == 0 )
        '										pvDest[p] = pColors[highID];
        '									else
        '										pvDest[p] = Hues.GetHue( highHue - 1 ).Colors[(pColors[highID] >> 10) & 0x1F];
        '								}
        '
        '								pvDest += scanDelta;
        '							}
        '						}
        '					}
        '				}
        '			}
        '
        '			bmp.UnlockBits(bd);
        '		}


        Private Shared Sub LoadColors()
            m_Colors = New Short(32767) {}

            Dim path As String = Client.GetFilePath("radarcol.mul")

            If path Is Nothing Then
                Return
            End If

            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                NativeMethods._lread(fs.SafeFileHandle, pColors, &H10000)
            End Using

        End Sub

        Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
            target = value
            Return value
        End Function
    End Class
End Namespace