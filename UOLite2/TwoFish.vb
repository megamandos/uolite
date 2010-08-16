Imports System.Diagnostics
Imports System.Security.Cryptography

'
' * This implementation of Twofish has been published here:
' * http://www.codetools.com/csharp/twofish_csharp.asp
' 


Namespace Scripts.Engines.Encryption
    ''' <summary>
    ''' Summary description for TwofishEncryption.
    ''' </summary>
    Friend Class TwofishEncryption
        Inherits TwofishBase
        Implements ICryptoTransform
        Public Sub New(ByVal keyLen As Integer, ByRef key__1 As Byte(), ByRef iv__2 As Byte(), ByVal cMode As CipherMode, ByVal direction As EncryptionDirection)
            ' convert our key into an array of ints
            For i As Integer = 0 To key__1.Length \ 4 - 1
                Key(i) = CUInt(key__1(i * 4 + 3) << 24) Or CUInt(key__1(i * 4 + 2) << 16) Or CUInt(key__1(i * 4 + 1) << 8) Or CUInt(key__1(i * 4 + 0))
            Next

            cipherMode = cMode

            ' we only need to convert our IV if we are using CBC
            If cipherMode = CipherMode.CBC Then
                For i As Integer = 0 To 3
                    IV(i) = CUInt(iv__2(i * 4 + 3) << 24) Or CUInt(iv__2(i * 4 + 2) << 16) Or CUInt(iv__2(i * 4 + 1) << 8) Or CUInt(iv__2(i * 4 + 0))
                Next
            End If

            encryptionDirection = direction
            reKey(keyLen, Key)
        End Sub

        ' need to have this method due to IDisposable - just can't think of a reason to use it for in this class
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub


        ''' <summary>
        ''' Transform a block depending on whether we are encrypting or decrypting
        ''' </summary>
        ''' <param name="inputBuffer"></param>
        ''' <param name="inputOffset"></param>
        ''' <param name="inputCount"></param>
        ''' <param name="outputBuffer"></param>
        ''' <param name="outputOffset"></param>
        ''' <returns></returns>
        Public Function TransformBlock(ByVal inputBuffer As Byte(), ByVal inputOffset As Integer, ByVal inputCount As Integer, ByVal outputBuffer As Byte(), ByVal outputOffset As Integer) As Integer Implements ICryptoTransform.TransformBlock
            Dim x As UInteger() = New UInteger(3) {}

            ' load it up
            For i As Integer = 0 To 3

                x(i) = CUInt(inputBuffer(i * 4 + 3 + inputOffset) << 24) Or CUInt(inputBuffer(i * 4 + 2 + inputOffset) << 16) Or CUInt(inputBuffer(i * 4 + 1 + inputOffset) << 8) Or CUInt(inputBuffer(i * 4 + 0 + inputOffset))
            Next

            If encryptionDirection = EncryptionDirection.Encrypting Then
                blockEncrypt(x)
            Else
                blockDecrypt(x)
            End If


            ' load it up
            For i As Integer = 0 To 3
                outputBuffer(i * 4 + 0 + outputOffset) = b0(x(i))
                outputBuffer(i * 4 + 1 + outputOffset) = b1(x(i))
                outputBuffer(i * 4 + 2 + outputOffset) = b2(x(i))
                outputBuffer(i * 4 + 3 + outputOffset) = b3(x(i))
            Next


            Return inputCount
        End Function

        Public Function TransformFinalBlock(ByVal inputBuffer As Byte(), ByVal inputOffset As Integer, ByVal inputCount As Integer) As Byte() Implements ICryptoTransform.TransformFinalBlock
            Dim outputBuffer As Byte()
            ' = new byte[0];
            If inputCount > 0 Then
                outputBuffer = New Byte(15) {}
                ' blocksize
                Dim x As UInteger() = New UInteger(3) {}

                ' load it up
                For i As Integer = 0 To 3
                    ' should be okay as we have already said to pad with zeros

                    x(i) = CUInt(inputBuffer(i * 4 + 3 + inputOffset) << 24) Or CUInt(inputBuffer(i * 4 + 2 + inputOffset) << 16) Or CUInt(inputBuffer(i * 4 + 1 + inputOffset) << 8) Or CUInt(inputBuffer(i * 4 + 0 + inputOffset))
                Next

                If encryptionDirection = EncryptionDirection.Encrypting Then
                    blockEncrypt(x)
                Else
                    blockDecrypt(x)
                End If

                ' load it up
                For i As Integer = 0 To 3
                    outputBuffer(i * 4 + 0) = b0(x(i))
                    outputBuffer(i * 4 + 1) = b1(x(i))
                    outputBuffer(i * 4 + 2) = b2(x(i))
                    outputBuffer(i * 4 + 3) = b3(x(i))
                Next
            Else
                ' the .NET framework doesn't like it if you return null - this calms it down
                outputBuffer = New Byte(-1) {}
            End If

            Return outputBuffer
        End Function

        ' not worked out this property yet - placing break points here just don't get caught.
        Private m_canReuseTransform As Boolean = True
        Public ReadOnly Property CanReuseTransform() As Boolean Implements ICryptoTransform.CanReuseTransform
            Get
                Return m_canReuseTransform
            End Get
        End Property

        ' I normally set this to false when block encrypting so that I can work on one block at a time
        ' but for compression and stream type ciphers this can be set to true so that you get all the data
        Private m_canTransformMultipleBlocks As Boolean = False
        Public ReadOnly Property CanTransformMultipleBlocks() As Boolean Implements ICryptoTransform.CanTransformMultipleBlocks
            Get
                Return m_canTransformMultipleBlocks
            End Get
        End Property

        Public ReadOnly Property InputBlockSize() As Integer Implements ICryptoTransform.InputBlockSize
            Get
                Return inputBlockSize
            End Get
        End Property

        Public ReadOnly Property OutputBlockSize() As Integer Implements ICryptoTransform.OutputBlockSize
            Get
                Return outputBlockSize
            End Get
        End Property

        Private encryptionDirection As EncryptionDirection
    End Class
End Namespace