'Made to organize the math, like calculating relative direction between two mobiles, and 2D/3D distance.

Partial Class LiteClient
    ''' <summary>Returns the direction of Y, relative to X.</summary>
    Public Function GetDirection(ByRef X1 As UShort, ByRef Y1 As UShort, ByRef X2 As UShort, ByRef Y2 As UShort) As UOLite2.Enums.Direction
        If X1 = X2 And Y1 = Y2 Then
            Return UOLite2.Enums.Direction.None
        ElseIf X1 = X2 And Y1 < Y2 Then
            Return UOLite2.Enums.Direction.South
        ElseIf X1 = X2 And Y1 > Y2 Then
            Return UOLite2.Enums.Direction.North
        ElseIf X1 > X2 And Y1 = Y2 Then
            Return UOLite2.Enums.Direction.West
        ElseIf X1 > X2 And Y1 < Y2 Then
            Return UOLite2.Enums.Direction.SouthWest
        ElseIf X1 > X2 And Y1 > Y2 Then
            Return UOLite2.Enums.Direction.NorthWest
        ElseIf X1 < X2 And Y1 = Y2 Then
            Return UOLite2.Enums.Direction.East
        ElseIf X1 < X2 And Y1 < Y2 Then
            Return UOLite2.Enums.Direction.SouthEast
        Else 'If X1 < X2 And Y1 > Y2 Then
            Return UOLite2.Enums.Direction.NorthEast
        End If
    End Function

    Public Function GetDirection(ByRef Mobile1 As Mobile, ByRef Mobile2 As Mobile)
        Return GetDirection(Mobile1.X, Mobile1.Y, Mobile2.X, Mobile2.Y)
    End Function

    Public Function Get2DDistance(ByRef Mobile1 As Mobile, ByRef Mobile2 As Mobile)
        Return Get2DDistance(Mobile1.X, Mobile1.Y, Mobile2.X, Mobile2.Y)
    End Function

    Public Function Get2DDistance(ByRef X1 As UShort, ByRef Y1 As UShort, ByRef X2 As UShort, ByRef Y2 As UShort) As UShort
        'Whichever is greater is the distance.
        Dim xdif As Integer = CInt(X1) - CInt(X2)
        Dim ydif As Integer = CInt(Y1) - CInt(Y2)

        If xdif < 0 Then xdif *= -1
        If ydif < 0 Then ydif *= -1

        'Return the largest difference.
        If ydif > xdif Then
            Return ydif
        Else
            Return xdif
        End If
    End Function

    Public Sub ReverseByteArray(ByRef Bytes() As Byte)
        Dim tempbyte As Byte = 0

        For i As Integer = 0 To Bytes.Length \ 2
            tempbyte = Bytes(i)
            Bytes(i) = Bytes(Bytes.Length - 1 - i)
            Bytes(Bytes.Length - 1 - i) = tempbyte
        Next

    End Sub

    ''' <summary>Returns the given string as a byte array, padded as specified.</summary>
    ''' <param name="Length">The size of the array you want back.</param>
    ''' <param name="Text">The text you want encoded in bytes.</param>
    ''' <param name="Unicode">Whether or not you want unicode or not.</param>
    ''' <param name="NullTerminate">Whether to add the null bytes to the end of the string.</param>
    Friend Function GetBytesFromString(ByRef Length As Integer, ByRef Text As String, Optional ByRef NullTerminate As Boolean = False, Optional ByRef Unicode As Boolean = False) As Byte()
        Dim bytes(Length - 1) As Byte 'make an empty array the size specified.
        Dim encoding As New System.Text.ASCIIEncoding() 'make an encoder.
        Dim strbytes() As Byte = encoding.GetBytes(Text) 'get the encoder to encode the bytes.

        'copy the bytes into the new array.
        For i As Integer = 0 To strbytes.Length - 1
            bytes(i) = strbytes(i)
        Next

        'return the new array of bytes with the ascii string into it.
        Return bytes
    End Function

    ''' <summary>Copies bytes from one array to another.</summary>
    ''' <param name="SourceArray">Where to get the bytes.</param>
    ''' <param name="TargetArray">Where to put the bytes.</param>
    ''' <param name="SourceStartIndex">The position in the source array to start reading.</param>
    ''' <param name="TargetStartIndex">The position in the target array to start writing.</param>
    ''' <param name="Size">The number of bytes to copy.</param>
    Friend Sub InsertBytes(ByRef SourceArray() As Byte, ByRef TargetArray() As Byte, ByRef SourceStartIndex As Integer, ByRef TargetStartIndex As Integer, ByRef Size As Integer)
        For i As Integer = TargetStartIndex To TargetStartIndex + Size - 1
            TargetArray(i) = SourceArray(i - TargetStartIndex + SourceStartIndex)
        Next
    End Sub

    Protected Friend Function GetHexStringToUint(ByRef HexString As String) As UInteger
        Dim RetInt As UInteger = 0

        For i As UInteger = HexString.Length - 1 To 0
            Select Case HexString(i)
                Case "0"
                    RetInt += 0 * (i ^ 16)
                Case "1"
                    RetInt += 1 * (i ^ 16)
                Case "2"
                    RetInt += 2 * (i ^ 16)
                Case "3"
                    RetInt += 3 * (i ^ 16)
                Case "4"
                    RetInt += 4 * (i ^ 16)
                Case "5"
                    RetInt += 5 * (i ^ 16)
                Case "6"
                    RetInt += 6 * (i ^ 16)
                Case "7"
                    RetInt += 7 * (i ^ 16)
                Case "8"
                    RetInt += 8 * (i ^ 16)
                Case "9"
                    RetInt += 9 * (i ^ 16)
                Case "A", "a"
                    RetInt += 10 * (i ^ 16)
                Case "B", "b"
                    RetInt += 11 * (i ^ 16)
                Case "C", "c"
                    RetInt += 12 * (i ^ 16)
                Case "D", "d"
                    RetInt += 13 * (i ^ 16)
                Case "E", "e"
                    RetInt += 14 * (i ^ 16)
                Case "F", "f"
                    RetInt += 15 * (i ^ 16)
            End Select
        Next

        Return RetInt
    End Function

    Protected Friend Function GetSerialFromString(ByRef SerialString As String) As Serial
        Return New Serial(GetHexStringToUint(SerialString))
    End Function

End Class
