'The circular buffer class was custom made by me, though is not a unique idea of mine.
'If you want to know how a circular buffer class actually operates, check the wikipedia entry.
'It is a generic class and can be used in many ways that a FIFO buffer is needed.

Namespace SupportClasses


    'see http://en.wikipedia.org/wiki/Circular_buffer
    ''' <summary>A highly efficient FIFO buffer class.</summary>
    ''' <typeparam name="T">The Type of objects you want to make a circular buff of.</typeparam>
    Public Class CircularBuffer(Of T)
        Private Shared Bytes() As T
        Private ReadPosition As UInteger = 0
        Private WritePosition As UInteger = 0
        Private _Size As Integer = 0

        Public Sub New(ByRef Size As UInteger)
            Dim b(Size) As T
            Bytes = b
        End Sub

#Region "Read"

        ''' <summary>Returns the object at the tail of the buffer and advances the read position by 1.</summary>
        Public Function ReadSingle() As T
            Dim j As T = Bytes(ReadPosition)

            SyncLock Me
                ReadPosition += 1
                If ReadPosition = Bytes.Length Then ReadPosition = 0
                _Size -= 1
            End SyncLock

            Return j
        End Function

        ''' <summary>Returns an array containing the number of objects requested, and advances the tail of the buffer.</summary>
        ''' <param name="Number">Number of objects to get from the tail of the buffer.</param>
        Public Function Read(ByRef Number As UInteger) As T()
            Dim ReturnBytes(Number) As T

            SyncLock Me
                For i As Integer = 0 To Number - 1
                    ReturnBytes(i) = Bytes(ReadPosition)
                    ReadPosition += 1
                    If ReadPosition = Bytes.Length Then ReadPosition = 0
                Next
            End SyncLock

            _Size -= Number

            Return ReturnBytes
        End Function

        ''' <summary>Advances the tail of the buffer, returns nothing, effectively skipping the specified number of objects.</summary>
        ''' <param name="NumberToSkip">The amount by which to advance.</param>
        Public Sub AdvanceTail(Optional ByRef NumberToSkip As UInteger = 1)
            If ReadPosition + NumberToSkip <= WritePosition Then
                ReadPosition += NumberToSkip
                _Size -= NumberToSkip
            Else
                Throw New ApplicationException("Tried to advance the tail past the head!")
            End If

        End Sub

#End Region

#Region "Write"

        ''' <summary>
        ''' Writes a single object to the buffer, then advances the head position by 1.
        ''' </summary>
        ''' <param name="ObjectToWrite">The object to insert into the buffer.</param>
        ''' <remarks></remarks>
        Public Sub WriteSingle(ByRef ObjectToWrite As T)
            SyncLock Me
                Bytes(WritePosition) = ObjectToWrite
                WritePosition += 1
                If WritePosition = Bytes.Length Then WritePosition = 0

                _Size += 1
            End SyncLock
        End Sub

        ''' <summary>
        ''' Writes the array of objects to the buffer and advances the head position by that number of objects.
        ''' </summary>
        ''' <param name="ObjectsToWrite">An array of the objects to write to the buffer.</param>
        ''' <remarks></remarks>
        Public Sub Write(ByRef ObjectsToWrite() As T)
            SyncLock Me
                For Each b As T In ObjectsToWrite
                    Bytes(WritePosition) = b
                    WritePosition += 1
                    If WritePosition = Bytes.Length Then WritePosition = 0
                Next
                _Size += ObjectsToWrite.Length
            End SyncLock
        End Sub

        ''' <summary>
        ''' Empties the buffer.
        ''' </summary>
        Public Sub Clear()
            _Size = 0
            ReadPosition = WritePosition
        End Sub

#End Region

#Region "Peek"

        ''' <summary>
        ''' Returns the specified number of objects as the desired position, but does NOT advance the tail position.
        ''' </summary>
        ''' <param name="Size">The number of objects to retreive</param>
        ''' <param name="Offset">The position to start reading from.</param>
        Public Function PeekMultiple(ByRef Size As UInteger, Optional ByRef Offset As UInteger = 0) As T()
            Dim retbytes(Size - 1) As T
            SyncLock Me
                For i As UInteger = 0 To Size - 1
                    retbytes(i) = Bytes(ReadPosition + i + Offset)
                Next
            End SyncLock
            Return retbytes
        End Function

        ''' <summary>
        ''' Returns the object as the given postion, but does NOT advance the tail position.
        ''' </summary>
        ''' <param name="Offset"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function Peek(Optional ByRef Offset As UInteger = 0) As T
            Dim k As T
            SyncLock Me
                k = Bytes(ReadPosition + Offset)
            End SyncLock
            Return k
        End Function

#End Region

#Region "Properties"

        ''' <summary>
        ''' The total number of objects current held in the buffer. (not the actual buffer size)
        ''' </summary>
        Public ReadOnly Property Size As Integer
            Get
                Return _Size
            End Get
        End Property

        ''' <summary>
        ''' Return the true size of the circular buffer, not just how much it actualy contains.
        ''' </summary>
        Public ReadOnly Property RealSize As UInteger
            Get
                Return Bytes.Length
            End Get
        End Property

        ''' <summary>
        ''' The offset of the tail from the true beginning of the buffer.
        ''' </summary>
        Public ReadOnly Property TailPosition As UInteger
            Get
                Return ReadPosition
            End Get
        End Property

        ''' <summary>
        ''' The offset of the head from the true beginning of the buffer.
        ''' </summary>
        Public ReadOnly Property HeadPosition As UInteger
            Get
                Return WritePosition
            End Get
        End Property

        ''' <summary>
        ''' Copies the entire buffer to an array and returns the array, does NOT move the head or tail position.
        ''' </summary>
        Public ReadOnly Property ToArray As T()
            Get
                Dim ReturnBytes(Size) As T
                Dim CurrPos As UInteger = ReadPosition

                SyncLock Me
                    For i As UInteger = 0 To Size - 1
                        ReturnBytes(i) = Bytes(CurrPos)
                        CurrPos += 1
                        If CurrPos = Bytes.Length Then CurrPos = 0
                    Next
                End SyncLock

                Return ReturnBytes
            End Get
        End Property

#End Region

    End Class

End Namespace

